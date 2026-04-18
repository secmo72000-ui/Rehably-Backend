using FluentAssertions;
using Rehably.Infrastructure.Services.Communication;
using Xunit;

namespace Rehably.Tests.Services.Communication;

[Collection("Communication Tests")]
public class MockMessageStoreTests
{
    [Fact]
    public void Add_WithSingleMessage_MessageIsStored()
    {
        MockMessageStore.Clear();

        var message = new MockMessage
        {
            Type = "Email",
            Timestamp = DateTime.UtcNow,
            To = "test@example.com",
            Content = "Test message"
        };

        MockMessageStore.Add(message);

        var messages = MockMessageStore.GetAll();
        messages.Should().ContainSingle();
        messages[0].Should().BeEquivalentTo(message);
    }

    [Fact]
    public void Add_WithMultipleMessages_AllMessagesAreStored()
    {
        MockMessageStore.Clear();

        var messages = new List<MockMessage>
        {
            new() { Type = "Email", To = "user1@example.com", Content = "Message 1" },
            new() { Type = "SMS", To = "+1234567890", Content = "Message 2" },
            new() { Type = "WhatsApp", To = "+0987654321", Content = "Message 3" }
        };

        foreach (var message in messages)
        {
            MockMessageStore.Add(message);
        }

        var stored = MockMessageStore.GetAll();
        stored.Should().HaveCount(3);
    }

    [Fact]
    public void Clear_WithExistingMessages_RemovesAllMessages()
    {
        MockMessageStore.Clear();

        MockMessageStore.Add(new MockMessage { Type = "Email", To = "test@example.com", Content = "Test" });

        MockMessageStore.Clear();

        var messages = MockMessageStore.GetAll();
        messages.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WithEmptyMessages_DoesNotThrow()
    {
        MockMessageStore.Clear();

        var act = () => MockMessageStore.Clear();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetAll_AfterAdd_ReturnsMessagesInOrder()
    {
        MockMessageStore.Clear();

        MockMessageStore.Add(new MockMessage { Type = "Email", To = "first@example.com", Content = "First" });
        MockMessageStore.Add(new MockMessage { Type = "SMS", To = "+1234567890", Content = "Second" });
        MockMessageStore.Add(new MockMessage { Type = "WhatsApp", To = "+0987654321", Content = "Third" });

        var messages = MockMessageStore.GetAll();
        messages.Should().HaveCount(3);
        messages[0].To.Should().Be("first@example.com");
        messages[1].To.Should().Be("+1234567890");
        messages[2].To.Should().Be("+0987654321");
    }
}