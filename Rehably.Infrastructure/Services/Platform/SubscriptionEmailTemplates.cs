namespace Rehably.Infrastructure.Services.Platform;

public static class SubscriptionEmailTemplates
{
    public static string SubscriptionCreated(string clinicName, string packageName, DateTime startDate, DateTime endDate, DateTime? trialEndDate)
    {
        var trialText = trialEndDate.HasValue ? $"<p><strong>Trial Period:</strong> Until {trialEndDate.Value:MMMM dd, yyyy}</p>" : "";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Subscription Activated</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #4CAF50; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                    .button { display: inline-block; padding: 12px 30px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Subscription Activated!</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>Your subscription has been successfully activated. Here are your subscription details:</p>

                        <div class="details">
                            <h3>Subscription Details</h3>
                            <p><strong>Package:</strong> {{packageName}}</p>
                            <p><strong>Start Date:</strong> {{startDate:MMMM dd, yyyy}}</p>
                            <p><strong>End Date:</strong> {{endDate:MMMM dd, yyyy}}</p>
                            {{trialText}}
                        </div>

                        <p>You can now enjoy all the features included in your package. If you have any questions or need assistance, please don't hesitate to contact our support team.</p>

                        <a href="#" class="button">Manage Your Subscription</a>
                    </div>
                    <div class="footer">
                        <p>Thank you for choosing our service!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string SubscriptionCancelled(string clinicName, string packageName, DateTime cancelledAt, string? reason)
    {
        var reasonText = !string.IsNullOrEmpty(reason) ? $"<p><strong>Reason:</strong> {reason}</p>" : "";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Subscription Cancelled</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #f44336; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Subscription Cancelled</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>Your subscription has been cancelled as of {{cancelledAt:MMMM dd, yyyy}}.</p>

                        <div class="details">
                            <h3>Cancellation Details</h3>
                            <p><strong>Package:</strong> {{packageName}}</p>
                            <p><strong>Cancelled On:</strong> {{cancelledAt:MMMM dd, yyyy}}</p>
                            {{reasonText}}
                        </div>

                        <p>Your access to the service will continue until the end of your current billing period. After that, your account will be downgraded to our free tier.</p>

                        <p>We're sorry to see you go! If you change your mind, you can always reactivate your subscription from your dashboard.</p>
                    </div>
                    <div class="footer">
                        <p>Thank you for being part of our community!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string SubscriptionRenewed(string clinicName, string packageName, DateTime newStartDate, DateTime newEndDate)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Subscription Renewed</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #2196F3; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #2196F3; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Subscription Renewed!</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>Great news! Your subscription has been successfully renewed.</p>

                        <div class="details">
                            <h3>Renewal Details</h3>
                            <p><strong>Package:</strong> {{packageName}}</p>
                            <p><strong>New Period:</strong> {{newStartDate:MMMM dd, yyyy}} - {{newEndDate:MMMM dd, yyyy}}</p>
                        </div>

                        <p>Your service will continue uninterrupted. All your data and settings have been preserved.</p>

                        <p>Thank you for your continued trust in our service!</p>
                    </div>
                    <div class="footer">
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string SubscriptionUpgraded(string clinicName, string oldPackageName, string newPackageName, DateTime upgradeDate)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Subscription Upgraded</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #FF9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #FF9800; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Subscription Upgraded!</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>Congratulations! Your subscription has been successfully upgraded.</p>

                        <div class="details">
                            <h3>Upgrade Details</h3>
                            <p><strong>Previous Package:</strong> {{oldPackageName}}</p>
                            <p><strong>New Package:</strong> {{newPackageName}}</p>
                            <p><strong>Upgrade Date:</strong> {{upgradeDate:MMMM dd, yyyy}}</p>
                        </div>

                        <p>You now have access to all the features and benefits included in your new package. Start exploring your new capabilities right away!</p>

                        <p>If you have any questions about your new package features, please check our documentation or contact support.</p>
                    </div>
                    <div class="footer">
                        <p>Enjoy your upgraded experience!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string SubscriptionExpiring(string clinicName, string packageName, DateTime endDate, int daysUntilExpiry)
    {
        var urgencyText = daysUntilExpiry <= 3 ? "Your subscription is expiring very soon!" :
                         daysUntilExpiry <= 7 ? "Your subscription is expiring soon!" :
                         "Your subscription is expiring soon.";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Subscription Expiring Soon</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #ff9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #ff9800; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                    .button { display: inline-block; padding: 12px 30px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>{{urgencyText}}</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>This is a friendly reminder that your subscription will expire in <strong>{{daysUntilExpiry}} day(s)</strong>.</p>

                        <div class="details">
                            <h3>Current Subscription</h3>
                            <p><strong>Package:</strong> {{packageName}}</p>
                            <p><strong>Expiration Date:</strong> {{endDate:MMMM dd, yyyy}}</p>
                        </div>

                        <p>To avoid any interruption in service, please renew your subscription before the expiration date.</p>

                        <a href="#" class="button">Renew Now</a>

                        <p>If you have any questions or need assistance with renewal, please don't hesitate to contact our support team.</p>
                    </div>
                    <div class="footer">
                        <p>We value your business!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string PaymentFailed(string clinicName, string packageName, DateTime failedDate, string? errorMessage)
    {
        var errorText = !string.IsNullOrEmpty(errorMessage) ? $"<p><strong>Error:</strong> {errorMessage}</p>" : "";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Payment Failed</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }
                    .details { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #f44336; }
                    .footer { text-align: center; margin-top: 20px; color: #777; font-size: 12px; }
                    .button { display: inline-block; padding: 12px 30px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Payment Failed</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>We were unable to process your subscription payment. This may be due to insufficient funds, an expired card, or other banking issues.</p>

                        <div class="details">
                            <h3>Payment Details</h3>
                            <p><strong>Package:</strong> {{packageName}}</p>
                            <p><strong>Failed On:</strong> {{failedDate:MMMM dd, yyyy}}</p>
                            {{errorText}}
                        </div>

                        <p>Please update your payment information or try a different payment method to ensure uninterrupted service.</p>

                        <a href="#" class="button">Update Payment Method</a>

                        <p>If you believe this is an error or need assistance, please contact our support team immediately.</p>
                    </div>
                    <div class="footer">
                        <p>Please address this issue promptly to avoid service interruption.</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
