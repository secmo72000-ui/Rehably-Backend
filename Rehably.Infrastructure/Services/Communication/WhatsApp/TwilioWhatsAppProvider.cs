using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Rehably.Infrastructure.Services.Communication.WhatsApp;

public class TwilioWhatsAppProvider : IWhatsAppProvider
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioWhatsAppProvider(string accountSid, string authToken, string fromNumber)
    {
        _accountSid = accountSid;
        _authToken = authToken;
        _fromNumber = fromNumber;
        TwilioClient.Init(accountSid, authToken);
    }

    public string Name => "Twilio WhatsApp";

    public async Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await MessageResource.CreateAsync(
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(message.To),
                body: message.Body);

            return new WhatsAppResult
            {
                Success = true,
                MessageId = result.Sid
            };
        }
        catch (Exception ex)
        {
            return new WhatsAppResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
