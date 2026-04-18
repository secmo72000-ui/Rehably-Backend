using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.Communication;
using Rehably.Infrastructure.Services.Communication;
using Rehably.Infrastructure.Services.Communication.WhatsApp;
using Xunit;

namespace Rehably.Tests.Services.Communication;

[Collection("Communication Tests")]
public class MockWhatsAppProviderTests
{
    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        var loggerMock = new Mock<ILogger<MockWhatsAppProvider>>();
        var provider = new MockWhatsAppProvider(loggerMock.Object);

        var message = new WhatsAppMessage
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
        var loggerMock = new Mock<ILogger<MockWhatsAppProvider>>();
        var provider = new MockWhatsAppProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new WhatsAppMessage
        {
            To = "+201234567890",
            Body = "Your verification code is: 123456"
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages.Should().ContainSingle();
        messages[0].Type.Should().Be("WhatsApp");
        messages[0].To.Should().Be("+201234567890");
        messages[0].Content.Should().Contain("Your verification code is: 123456");
    }

    [Fact]
    public async Task SendAsync_WithMedia_IncludesMediaInContent()
    {
        var loggerMock = new Mock<ILogger<MockWhatsAppProvider>>();
        var provider = new MockWhatsAppProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new WhatsAppMessage
        {
            To = "+201234567890",
            Body = "Please find the attached image",
            Media = new List<WhatsAppMedia>
            {
                new() { ContentType = "image/jpeg", Url = "https://example.com/image.jpg" }
            }
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages[0].Content.Should().Contain("Media: 1");
        messages[0].Content.Should().Contain("image/jpeg");
    }

    [Fact]
    public void Name_ReturnsMock()
    {
        var loggerMock = new Mock<ILogger<MockWhatsAppProvider>>();
        var provider = new MockWhatsAppProvider(loggerMock.Object);

        provider.Name.Should().Be("Mock (Development)");
    }
}
