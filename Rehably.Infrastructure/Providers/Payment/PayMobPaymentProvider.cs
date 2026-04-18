using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.Services.Payment;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Providers.Payment;

public class PayMobPaymentProvider : IPaymentProvider
{
    private readonly PaymentProviderConfig _config;
    private readonly ILogger<PayMobPaymentProvider> _logger;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public string Name => "PayMob";
    public string Currency => _config.Currency ?? "EGP";

    private const string BaseUrl = "https://accept.paymob.com/api";
    private const string AuthPath = "/auth/tokens";
    private const string OrderRegistrationPath = "/ecommerce/orders";
    private const string PaymentKeyPath = "/acceptance/payment_keys";

    public PayMobPaymentProvider(
        PaymentProviderConfig config,
        ILogger<PayMobPaymentProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PayMob");
    }

    public async Task<Result<PaymentInitiationResult>> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                return Result<PaymentInitiationResult>.Failure("PayMob API key is not configured");
            }

            if (string.IsNullOrEmpty(_config.IntegrationId))
            {
                return Result<PaymentInitiationResult>.Failure("PayMob Integration ID is not configured");
            }

            var authResult = await GetAuthTokenAsync();
            if (!authResult.Success)
            {
                return Result<PaymentInitiationResult>.Failure(authResult.Error!);
            }

            var orderResult = await RegisterOrderAsync(authResult.Token!, (int)(amount * 100), metadata);
            if (!orderResult.Success)
            {
                return Result<PaymentInitiationResult>.Failure(orderResult.Error!);
            }

            var paymentKeyResult = await GetPaymentKeyAsync(
                authResult.Token!,
                orderResult.Id!.Value,
                (int)(amount * 100),
                currency,
                returnUrl,
                cancelUrl);

            if (!paymentKeyResult.Success)
            {
                return Result<PaymentInitiationResult>.Failure(paymentKeyResult.Error!);
            }

            var iframeUrl = _config.FrameUrl ?? "https://accept.paymob.com/api/acceptance/iframes";
            var paymentUrl = $"{iframeUrl}/{paymentKeyResult.Token}";

            return Result<PaymentInitiationResult>.Success(
                new PaymentInitiationResult(paymentUrl, orderResult.Id.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayMob payment");
            return Result<PaymentInitiationResult>.Failure($"Failed to create payment: {ex.Message}");
        }
    }

    public async Task<Result<PaymentVerificationResult>> VerifyPaymentAsync(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Result<PaymentVerificationResult>.Failure("Payload is required");
            }

            var jsonDoc = JsonDocument.Parse(payload);
            var root = jsonDoc.RootElement;

            var transactionId = root.TryGetProperty("id", out var idProp)
                ? idProp.GetInt32().ToString()
                : root.TryGetProperty("obj", out var objProp) &&
                  objProp.TryGetProperty("id", out var objIdProp)
                    ? objIdProp.GetInt32().ToString()
                    : null;

            var isSuccess = root.TryGetProperty("success", out var successProp)
                ? successProp.GetBoolean()
                : root.TryGetProperty("obj", out var objProp2) &&
                  objProp2.TryGetProperty("success", out var objSuccessProp)
                    ? objSuccessProp.GetBoolean()
                    : false;

            if (isSuccess)
            {
                _logger.LogInformation("PayMob payment verified successfully: {TransactionId}", transactionId);
                return Result<PaymentVerificationResult>.Success(
                    new PaymentVerificationResult(transactionId));
            }

            _logger.LogWarning("PayMob payment verification failed: {TransactionId}", transactionId);
            return Result<PaymentVerificationResult>.Failure("Payment was not successful");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing PayMob webhook payload");
            return Result<PaymentVerificationResult>.Failure("Invalid payload format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayMob payment");
            return Result<PaymentVerificationResult>.Failure($"Failed to verify payment: {ex.Message}");
        }
    }

    public async Task<Result> RefundAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                return Result.Failure("PayMob API key is not configured");
            }

            var authResult = await GetAuthTokenAsync();
            if (!authResult.Success)
            {
                return Result.Failure(authResult.Error!);
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResult.Token}");

            var refundPayload = new
            {
                transaction_id = int.Parse(transactionId),
                amount_cents = amount.HasValue ? (int?)(int)(amount.Value * 100) : null
            };

            var content = new StringContent(
                JsonSerializer.Serialize(refundPayload, s_jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/acceptance/void_refund", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayMob refund failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return Result.Failure($"Refund failed: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, s_jsonOptions);

            if (result.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                _logger.LogInformation("PayMob refund successful: {TransactionId}", transactionId);
                return Result.Success();
            }

            _logger.LogWarning("PayMob refund not processed: {TransactionId}", transactionId);
            return Result.Failure("Refund was not processed by PayMob");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayMob refund for transaction {TransactionId}", transactionId);
            return Result.Failure($"Failed to process refund: {ex.Message}");
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        try
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            var secretToUse = !string.IsNullOrEmpty(secret) ? secret : _config.HmacSecret;
            if (string.IsNullOrEmpty(secretToUse))
            {
                _logger.LogWarning("PayMob HMAC secret is not configured");
                return false;
            }

            var computedHmac = ComputeHmac(payload, secretToUse);
            var isValid = computedHmac.Equals(signature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("PayMob webhook signature validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayMob webhook signature");
            return false;
        }
    }

    private async Task<(string? Token, string? Error, bool Success)> GetAuthTokenAsync()
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();

            var authPayload = new
            {
                api_key = _config.ApiKey
            };

            var content = new StringContent(
                JsonSerializer.Serialize(authPayload, s_jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}{AuthPath}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayMob authentication failed: {StatusCode}", response.StatusCode);
                return (null, $"Authentication failed: {response.StatusCode}", false);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, s_jsonOptions);

            if (result.TryGetProperty("token", out var tokenProp))
            {
                return (tokenProp.GetString(), null, true);
            }

            return (null, "Token not found in response", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayMob auth token");
            return (null, $"Failed to get auth token: {ex.Message}", false);
        }
    }

    private async Task<(int? Id, string? Error, bool Success)> RegisterOrderAsync(
        string authToken,
        int amountCents,
        Dictionary<string, string>? metadata)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            var orderPayload = new
            {
                auth_token = authToken,
                delivery_needed = false,
                amount_cents = amountCents,
                currency = Currency,
                items = new[] { new { name = "Clinic Subscription", amount_cents = amountCents, quantity = 1 } },
                metadata = metadata
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderPayload, s_jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}{OrderRegistrationPath}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayMob order registration failed: {StatusCode}", response.StatusCode);
                return (null, $"Order registration failed: {response.StatusCode}", false);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, s_jsonOptions);

            if (result.TryGetProperty("id", out var idProp))
            {
                return (idProp.GetInt32(), null, true);
            }

            return (null, "Order ID not found in response", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering PayMob order");
            return (null, $"Failed to register order: {ex.Message}", false);
        }
    }

    private async Task<(string? Token, string? Error, bool Success)> GetPaymentKeyAsync(
        string authToken,
        int orderId,
        int amountCents,
        string currency,
        string returnUrl,
        string cancelUrl)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            if (string.IsNullOrEmpty(_config.IntegrationId))
            {
                return (null, "Integration ID is not configured", false);
            }

            var billingData = new
            {
                apartment = "NA",
                email = "clinic@rehably.com",
                floor = "NA",
                first_name = "Rehably",
                street = "NA",
                building = "NA",
                phone_number = "NA",
                shipping_method = "NA",
                postal_code = "NA",
                city = "NA",
                country = "NA",
                last_name = "Clinic",
                state = "NA"
            };

            var paymentKeyPayload = new
            {
                auth_token = authToken,
                amount_cents = amountCents,
                expiration = 3600,
                order_id = orderId,
                billing_data = billingData,
                currency = currency,
                integration_id = _config.IntegrationId,
                lock_order_when_paid = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(paymentKeyPayload, s_jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}{PaymentKeyPath}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayMob payment key request failed: {StatusCode}", response.StatusCode);
                return (null, $"Payment key request failed: {response.StatusCode}", false);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, s_jsonOptions);

            if (result.TryGetProperty("token", out var tokenProp))
            {
                return (tokenProp.GetString(), null, true);
            }

            return (null, "Payment token not found in response", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayMob payment key");
            return (null, $"Failed to get payment key: {ex.Message}", false);
        }
    }

    private static string ComputeHmac(string data, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
