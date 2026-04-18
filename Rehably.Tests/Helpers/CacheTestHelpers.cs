using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Rehably.Tests.Helpers;

public static class CacheTestHelpers
{
    public static void SetupCacheGet<T>(Mock<IMemoryCache> cacheMock, string key, T? value)
    {
        object? cachedValue = value;
        cacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
            .Returns(value != null);
    }

    public static void SetupCacheGetWithEntry<T>(Mock<IMemoryCache> cacheMock, string key, T? value)
    {
        var entryMock = new Mock<ICacheEntry>();
        entryMock.Setup(e => e.Value).Returns(value);
        entryMock.Setup(e => e.PostEvictionCallbacks).Returns(Array.Empty<PostEvictionCallbackRegistration>());

        cacheMock.Setup(c => c.CreateEntry(key))
            .Returns(entryMock.Object);
        cacheMock.Setup(c => c.TryGetValue(key, out It.Ref<object?>.IsAny))
            .Returns(new CacheHit { Value = value });
    }

    public static void VerifyCacheSet(Mock<IMemoryCache> cacheMock, string key, Times? times = null)
    {
        cacheMock.Verify(c => c.CreateEntry(key), times ?? Times.Once());
    }

    public static void VerifyCacheRemove(Mock<IMemoryCache> cacheMock, string key, Times? times = null)
    {
        cacheMock.Verify(c => c.Remove(key), times ?? Times.Once());
    }

    public static void VerifyCacheTryGetValue(Mock<IMemoryCache> cacheMock, string key, Times? times = null)
    {
        cacheMock.Verify(c => c.TryGetValue(key, out It.Ref<object?>.IsAny), times ?? Times.Once());
    }

    public static Mock<ICacheEntry> SetupCacheEntry<T>(Mock<IMemoryCache> cacheMock, string key, T value)
    {
        var entryMock = new Mock<ICacheEntry>();
        entryMock.SetupSet(e => e.Value = value);
        entryMock.Setup(e => e.ExpirationTokens).Returns(Array.Empty<
            IChangeToken>());
        entryMock.Setup(e => e.PostEvictionCallbacks).Returns(Array.Empty<PostEvictionCallbackRegistration>());

        cacheMock.Setup(c => c.CreateEntry(key)).Returns(entryMock.Object);

        return entryMock;
    }

    public struct CacheHit
    {
        public object? Value { get; set; }

        public static implicit operator bool(CacheHit hit) => hit.Value != null;
    }
}
