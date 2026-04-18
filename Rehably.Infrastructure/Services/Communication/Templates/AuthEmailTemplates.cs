namespace Rehably.Infrastructure.Services.Communication.Templates;

public static class AuthEmailTemplates
{
    public static string PasswordResetOtp(string otp, int expiryMinutes, string locale = "en")
    {
        return locale == "ar"
            ? GetArabicPasswordResetOtp(otp, expiryMinutes)
            : GetEnglishPasswordResetOtp(otp, expiryMinutes);
    }

    private static string GetEnglishPasswordResetOtp(string otp, int expiryMinutes) => $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 40px; }}
        .otp-box {{ background: #f8f9fa; border: 2px dashed #667eea; border-radius: 8px; padding: 25px; text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #667eea; font-family: monospace; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Reset Your Password</h1>
        </div>
        <div class=""content"">
            <p>You requested to reset your password. Use the verification code below:</p>
            <div class=""otp-box"">
                <div class=""otp-code"">{otp}</div>
                <p style=""margin-top: 15px; color: #666;"">This code expires in {expiryMinutes} minutes</p>
            </div>
            <p>If you didn't request this, please ignore this email or contact support if you have concerns.</p>
        </div>
        <div class=""footer"">
            <p>&copy; Rehably - Clinic Management Platform</p>
        </div>
    </div>
</body>
</html>";

    private static string GetArabicPasswordResetOtp(string otp, int expiryMinutes) => $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Arial, sans-serif; background: #f4f4f4; direction: rtl; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 40px; text-align: right; }}
        .otp-box {{ background: #f8f9fa; border: 2px dashed #667eea; border-radius: 8px; padding: 25px; text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #667eea; font-family: monospace; direction: ltr; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>إعادة تعيين كلمة المرور</h1>
        </div>
        <div class=""content"">
            <p>لقد طلبت إعادة تعيين كلمة المرور. استخدم رمز التحقق أدناه:</p>
            <div class=""otp-box"">
                <div class=""otp-code"">{otp}</div>
                <p style=""margin-top: 15px; color: #666;"">ينتهي هذا الرمز خلال {expiryMinutes} دقائق</p>
            </div>
            <p>إذا لم تطلب هذا، يرجى تجاهل هذا البريد الإلكتروني أو الاتصال بالدعم إذا كانت لديك مخاوف.</p>
        </div>
        <div class=""footer"">
            <p>&copy; Rehably - منصة إدارة العيادات</p>
        </div>
    </div>
</body>
</html>";

    public static string EmailVerificationOtp(string otp, int expiryMinutes) => $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 40px; }}
        .otp-box {{ background: #f8f9fa; border: 2px dashed #667eea; border-radius: 8px; padding: 25px; text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #667eea; font-family: monospace; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Verify Your Email</h1>
        </div>
        <div class=""content"">
            <h2>Welcome to Rehably!</h2>
            <p>Use the verification code below to complete your registration:</p>
            <div class=""otp-box"">
                <div class=""otp-code"">{otp}</div>
                <p style=""margin-top: 15px; color: #666;"">This code expires in {expiryMinutes} minutes</p>
            </div>
            <p>If you didn't request this, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>&copy; Rehably - Clinic Management Platform</p>
        </div>
    </div>
</body>
</html>";

    public static string PasswordChanged(string email, DateTime changedAt, string locale = "en")
    {
        var dateStr = changedAt.ToString("yyyy-MM-dd HH:mm:ss UTC");
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
        .container {{ max-width: 600px; margin: 40px auto; padding: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Changed Successfully</h2>
        <p>Your password was changed on {dateStr}.</p>
        <p>If you did not make this change, please contact support immediately.</p>
        <p>- The Rehably Team</p>
    </div>
</body>
</html>";
    }
}
