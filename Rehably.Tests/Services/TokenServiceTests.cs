using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Moq;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Rehably.Tests.Services;

public class TokenServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _configurationMock = CreateConfigurationMock();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _roleManagerMock = CreateRoleManagerMock();

        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TokenServiceTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new ApplicationDbContext(dbContextOptions);

        _sut = new TokenService(
            _configurationMock.Object,
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _roleManagerMock.Object,
            _dbContext);
    }

    private static Mock<RoleManager<ApplicationRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            null!,
            null!,
            null!,
            null!);
    }

    [Fact]
    public void GenerateAccessToken_WithMustChangePasswordTrue_IncludesClaimAndSets5MinuteExpiration()
    {
        var token = _sut.GenerateAccessToken("user-123", Guid.NewGuid(), null, new List<string> { "User" }, mustChangePassword: true);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "mustChangePassword" && c.Value == "true");
        var expiration = jwtToken.ValidTo;
        var expectedExpiration = DateTime.UtcNow.AddMinutes(5);
        expiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_WithMustChangePasswordFalse_IncludesClaimAndSets24HourExpiration()
    {
        var token = _sut.GenerateAccessToken("user-123", Guid.NewGuid(), null, new List<string> { "User" }, mustChangePassword: false);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "mustChangePassword" && c.Value == "false");
        var expiration = jwtToken.ValidTo;
        var expectedExpiration = DateTime.UtcNow.AddMinutes(1440);
        expiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_IncludesAllRequiredClaims()
    {
        var tenantId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken("user-123", tenantId: tenantId, clinicId: clinicId, new List<string> { "Admin", "User" });

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
        jwtToken.Claims.Should().Contain(c => c.Type == "TenantId" && c.Value == tenantId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "ClinicId" && c.Value == clinicId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokenEachTime()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        token1.Should().NotBe(token2);
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64EncodedToken()
    {
        var token = _sut.GenerateRefreshToken();

        var buffer = new Span<byte>(new byte[64]);
        var isValid = Convert.TryFromBase64String(token, buffer, out var bytesWritten);
        isValid.Should().BeTrue();
        bytesWritten.Should().BeGreaterThan(0);
    }

    private Mock<IConfiguration> CreateConfigurationMock()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(x => x.GetSection("JwtSettings")).Returns(new JwtConfigurationSectionMock());
        return mock;
    }

    private class JwtConfigurationSectionMock : IConfigurationSection
    {
        public string? this[string key]
        {
            get => key switch
            {
                "Secret" => "this-is-a-test-secret-key-that-is-at-least-32-bytes-long",
                "Issuer" => "TestIssuer",
                "Audience" => "TestAudience",
                "AccessTokenExpirationMinutes" => "1440",
                _ => null
            };
            set { }
        }

        public string Key => "JwtSettings";
        public string Path => "JwtSettings";
        public string? Value { get; set; }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public IChangeToken? GetReloadToken() => new Mock<IChangeToken>().Object;
        public IConfigurationSection GetSection(string key) => this!;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
