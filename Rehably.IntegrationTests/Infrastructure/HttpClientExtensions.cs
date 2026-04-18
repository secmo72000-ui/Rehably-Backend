using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Rehably.IntegrationTests.Infrastructure;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> AssertSuccessAsync<T>(this HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseEnvelope<T>>(content, JsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue($"API response indicated failure: {content}");
        apiResponse.Data.Should().NotBeNull();

        return apiResponse.Data!;
    }

    public static async Task AssertFailureAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected,
            $"Expected {expected} but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    public static async Task AssertStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected,
            $"Expected {expected} but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private record ApiResponseEnvelope<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
    }
}
