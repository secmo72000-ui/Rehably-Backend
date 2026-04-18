using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.Communication;
using Rehably.Infrastructure.Services.Communication;
using Rehably.Infrastructure.Services.Communication.Email;
using Xunit;

namespace Rehably.Tests.Services.Communication;

[Collection("Communication Tests")]
public class MockEmailProviderTests
{
    [Fact]
    public async Task SendAsync_WithValidMessage_ReturnsSuccessResult()
    {
        var loggerMock = new Mock<ILogger<MockEmailProvider>>();
        var provider = new MockEmailProvider(loggerMock.Object);

        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = false
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
        var loggerMock = new Mock<ILogger<MockEmailProvider>>();
        var provider = new MockEmailProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Verification Code",
            Body = "Your code is: 123456",
            IsHtml = false
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages.Should().ContainSingle();
        messages[0].Type.Should().Be("Email");
        messages[0].To.Should().Be("recipient@example.com");
        messages[0].Content.Should().Contain("Verification Code");
        messages[0].Content.Should().Contain("Your code is: 123456");
    }

    [Fact]
    public async Task SendAsync_WithHtmlMessage_IncludesBodyInContent()
    {
        var loggerMock = new Mock<ILogger<MockEmailProvider>>();
        var provider = new MockEmailProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "HTML Email",
            Body = "<h1>Hello</h1><p>This is HTML</p>",
            IsHtml = true
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages[0].Content.Should().Contain("<h1>Hello</h1>");
    }

    [Fact]
    public async Task SendAsync_WithAttachments_IncludesAttachmentsInContent()
    {
        var loggerMock = new Mock<ILogger<MockEmailProvider>>();
        var provider = new MockEmailProvider(loggerMock.Object);
        MockMessageStore.Clear();

        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Email with Attachments",
            Body = "Please find attached files",
            IsHtml = false,
            Attachments = new List<EmailAttachment>
            {
                new() { FileName = "document.pdf", ContentType = "application/pdf" }
            }
        };

        await provider.SendAsync(message);

        var messages = MockMessageStore.GetAll();
        messages[0].Content.Should().Contain("Attachments: 1");
        messages[0].Content.Should().Contain("document.pdf");
    }

    [Fact]
    public void Name_ReturnsMock()
    {
        var loggerMock = new Mock<ILogger<MockEmailProvider>>();
        var provider = new MockEmailProvider(loggerMock.Object);

        provider.Name.Should().Be("Mock (Development)");
    }
}
