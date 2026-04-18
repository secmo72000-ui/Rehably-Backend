using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.Communication;
using Rehably.Infrastructure.Services.Communication;
using Rehably.Infrastructure.Services.Communication.Sms;
using Xunit;

namespace Rehably.Tests.Services.Communication;

[Collection("Communication Tests")]
public class MockSmsProviderTests
{
    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        var loggerMock = new Mock<ILogger<MockSmsProvider>>();
        var provider = new MockSmsProvider(loggerMock.Object);

        var message = new SmsMessage
        {
            To = "+201234567890",
            Body = "Your verification code is: 123456"
        };

        var result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
        result.MessageId.Should().NotBeEmpty();
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SendAsync_WithMessage_AddsToMockStore()
    {
        var loggerMock = new Mock<ILogger<MockSmsProvider>>();
        var provider = new MockSmsProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new SmsMessage
        {
            To = "+201234567890",
            Body = "Your verification code is: 123456"
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages.Should().ContainSingle();
        messages[0].Type.Should().Be("SMS");
        messages[0].To.Should().Be("+201234567890");
        messages[0].Content.Should().Contain("Your verification code is: 123456");
    }

    [Fact]
    public void Name_ReturnsMock()
    {
        var loggerMock = new Mock<ILogger<MockSmsProvider>>();
        var provider = new MockSmsProvider(loggerMock.Object);

        provider.Name.Should().Be("Mock (Development)");
    }
}
