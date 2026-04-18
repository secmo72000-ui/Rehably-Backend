using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace Rehably.Tests.Helpers;

/// <summary>
/// Represents the standard API success/error response envelope.
/// </summary>
public record ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("error")]
    public ApiError? Error { get; init; }
}

/// <summary>
/// Represents the error object within an API response.
/// </summary>
public record ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("details")]
    public List<string>? Details { get; init; }
}

/// <summary>
/// Represents an API response containing a list of items.
/// </summary>
public record ApiListResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public List<T> Data { get; init; } = [];

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Extension methods for deserializing and asserting API responses in integration tests.
/// </summary>
public static class ApiResponseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Reads the response as an ApiResponse, asserts HTTP 2xx and success==true, and returns the data.
    /// </summary>
    public static async Task<T> AssertSuccessAsync<T>(this HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success status code but got {(int)response.StatusCode} {response.StatusCode}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        apiResponse.Should().NotBeNull("Response body should deserialize to ApiResponse<T>");
        apiResponse!.Success.Should().BeTrue("API response success flag should be true");
        apiResponse.Data.Should().NotBeNull("API response data should not be null on success");

        return apiResponse.Data!;
    }

    /// <summary>
    /// Asserts the response has the expected HTTP status code and success==false.
    /// Returns the error object for further assertions.
    /// </summary>
    public static async Task<ApiError?> AssertFailureAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        response.StatusCode.Should().Be(expectedStatusCode,
            $"Expected {(int)expectedStatusCode} {expectedStatusCode}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        apiResponse.Should().NotBeNull("Response body should deserialize to ApiResponse");
        apiResponse!.Success.Should().BeFalse("API response success flag should be false on failure");

        return apiResponse.Error;
    }

    /// <summary>
    /// Deserializes the response body as an ApiResponse without any assertions.
    /// </summary>
    public static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(this HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        apiResponse.Should().NotBeNull("Response body should deserialize to ApiResponse<T>");
        return apiResponse!;
    }

    /// <summary>
    /// Reads the response as an ApiListResponse and asserts success.
    /// </summary>
    public static async Task<List<T>> AssertSuccessListAsync<T>(this HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success status code but got {(int)response.StatusCode} {response.StatusCode}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiListResponse<T>>(JsonOptions);
        apiResponse.Should().NotBeNull("Response body should deserialize to ApiListResponse<T>");
        apiResponse!.Success.Should().BeTrue("API response success flag should be true");

        return apiResponse.Data;
    }
}
