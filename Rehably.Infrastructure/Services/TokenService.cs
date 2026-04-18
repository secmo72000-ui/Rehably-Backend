using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Rehably.Application.Services.Auth;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Rehably.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public TokenService(
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public string GenerateAccessToken(string userId, Guid? tenantId, Guid? clinicId, IList<string> roles, bool mustChangePassword = false)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("mustChangePassword", mustChangePassword.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));
        }

        if (clinicId.HasValue)
        {
            claims.Add(new Claim("ClinicId", clinicId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expirationMinutes = mustChangePassword ? 5 : double.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "1440");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateAccessTokenAsync(string userId, Guid? tenantId, Guid? clinicId, IList<string> roles, IEnumerable<string> permissions, bool mustChangePassword = false)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("mustChangePassword", mustChangePassword.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));
        }

        if (clinicId.HasValue)
        {
            claims.Add(new Claim("ClinicId", clinicId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var expirationMinutes = mustChangePassword ? 5 : double.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "1440");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var token = await _refreshTokenRepository.GetValidTokenAsync(userId, refreshToken);
        return token != null;
    }

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        var refreshDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<HashSet<string>> GetPermissionsForRolesAsync(IList<string> roleNames)
    {
        if (roleNames == null || roleNames.Count == 0)
            return new HashSet<string>();

        var roleNameList = roleNames.ToList();

        var permissions = await _dbContext.RoleClaims
            .Join(
                _dbContext.Roles,
                rc => rc.RoleId,
                r => r.Id,
                (rc, r) => new { r.Name, rc.ClaimType, rc.ClaimValue })
            .Where(x => roleNameList.Contains(x.Name) && x.ClaimType == "Permission" && x.ClaimValue != null)
            .Select(x => x.ClaimValue!)
            .Distinct()
            .ToListAsync();

        return permissions.ToHashSet();
    }

    public async Task InvalidateClinicTokensAsync(Guid clinicId)
    {
        await _refreshTokenRepository.RevokeAllForClinicAsync(clinicId);
        await _unitOfWork.SaveChangesAsync();
    }
}
