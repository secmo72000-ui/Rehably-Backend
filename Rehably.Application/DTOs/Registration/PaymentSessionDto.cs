namespace Rehably.Application.DTOs.Registration;

public record PaymentSessionDto(
    string SessionUrl,
    string Provider,
    string SessionId
);
