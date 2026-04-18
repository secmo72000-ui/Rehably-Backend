using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Rehably.Infrastructure.Services.Communication.Sms;

public class TwilioSmsProvider : ISmsProvider
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioSmsProvider(string accountSid, string authToken, string fromNumber)
    {
        _accountSid = accountSid;
        _authToken = authToken;
        _fromNumber = fromNumber;
        TwilioClient.Init(accountSid, authToken);
    }

    public string Name => "Twilio";

    public async Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await MessageResource.CreateAsync(
                to: new PhoneNumber(message.To),
                from: new PhoneNumber(_fromNumber),
                body: message.Body);

            return new SmsResult
            {
                Success = true,
                MessageId = result.Sid
            };
        }
        catch (Exception ex)
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
