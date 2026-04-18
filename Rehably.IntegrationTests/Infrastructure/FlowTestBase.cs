using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Rehably.IntegrationTests.Infrastructure;

public abstract class FlowTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected RehablyWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected FlowTestBase(RehablyWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<string> LoginAsAdminAsync()
    {
        return await LoginAsync("admin@rehably.com", "TempPassword123!");
    }

    protected async Task<string> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        return result?.Data?.AccessToken
               ?? throw new InvalidOperationException($"Login failed for {email}: {content}");
    }

    protected async Task<LoginResponse> LoginFullAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        return result?.Data
               ?? throw new InvalidOperationException($"Login failed for {email}: {content}");
    }

    protected async Task<T> GetAsync<T>(string url, string token) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);

        return result?.Data ?? throw new InvalidOperationException($"GET {url} returned null data: {content}");
    }

    protected async Task<T> PostAsync<T>(string url, object body, string token) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);

        return result?.Data ?? throw new InvalidOperationException($"POST {url} returned null data: {content}");
    }

    protected async Task<HttpResponseMessage> GetRawAsync(string url, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> PostRawAsync(string url, object body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> PutRawAsync(string url, object body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> DeleteRawAsync(string url, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> PostMultipartRawAsync(string url, MultipartFormDataContent content, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;
        return await Client.SendAsync(request);
    }

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    // Response wrapper types matching the API's ApiResponse<T> shape
    protected record ApiResponse<T>(bool Success, T Data, string? Message, ApiError? Error) where T : class;
    protected record ApiResponse(bool Success, string? Message, ApiError? Error);
    protected record ApiError(string Code, string Message, List<string>? Details);

    protected record LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; }
        public DateTime ExpiresAt { get; init; }
        public bool MustChangePassword { get; init; }
        public bool EmailVerified { get; init; }
    }
}
