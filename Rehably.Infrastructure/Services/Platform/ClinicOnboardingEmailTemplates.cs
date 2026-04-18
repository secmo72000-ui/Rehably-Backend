namespace Rehably.Infrastructure.Services.Platform;

public static class ClinicOnboardingEmailTemplates
{
    public static string OtpVerification(string clinicName, string otp, int expiryMinutes)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Verify Your Email</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { margin: 0; font-size: 28px; font-weight: 600; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .otp-box { background: #f8f9fa; border: 2px dashed #667eea; padding: 25px; text-align: center; border-radius: 8px; margin: 25px 0; }
                    .otp-code { font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #667eea; margin: 15px 0; }
                    .warning { background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                    .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Email Verification</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{clinicName}}</strong>,</p>
                        <p>Thank you for registering with Rehably! To complete your registration and secure your account, please verify your email address using the One-Time Password (OTP) below.</p>

                        <div class="otp-box">
                            <p style="margin: 0; color: #666; font-size: 14px;">Your Verification Code</p>
                            <div class="otp-code">{{otp}}</div>
                        </div>

                        <div class="warning">
                            <p style="margin: 0; color: #856404;"><strong>Important:</strong> This code will expire in <strong>{{expiryMinutes}} minutes</strong>. Please use it promptly.</p>
                        </div>

                        <p>If you didn't request this verification, please ignore this email. Your account remains secure.</p>

                        <p>If you have any questions or need assistance, our support team is here to help.</p>
                    </div>
                    <div class="footer">
                        <p>Welcome to Rehably - Your Physiotherapy Clinic Management Solution</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string RegistrationPending(string clinicName, string ownerName, string registrationDate)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Registration Submitted</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .steps { background: #f8f9fa; padding: 25px; border-radius: 8px; margin: 25px 0; }
                    .step { display: flex; align-items: center; margin: 15px 0; }
                    .step-number { background: #11998e; color: white; width: 30px; height: 30px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: bold; margin-right: 15px; flex-shrink: 0; }
                    .step-text { flex: 1; }
                    .step.completed .step-number { background: #38ef7d; }
                    .step.completed .step-text { color: #11998e; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Registration Submitted Successfully!</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{ownerName}}</strong>,</p>
                        <p>Congratulations! Your registration for <strong>{{clinicName}}</strong> has been submitted successfully on {{registrationDate:MMMM dd, yyyy}}.</p>

                        <p>Your application is now under review. Our team will verify your information and documents within 1-2 business days.</p>

                        <div class="steps">
                            <h3 style="margin-top: 0;">Onboarding Progress</h3>
                            <div class="step completed">
                                <div class="step-number">1</div>
                                <div class="step-text"><strong>Email Verification</strong> - Completed</div>
                            </div>
                            <div class="step completed">
                                <div class="step-number">2</div>
                                <div class="step-text"><strong>Documents Upload</strong> - Completed</div>
                            </div>
                            <div class="step">
                                <div class="step-number">3</div>
                                <div class="step-text"><strong>Review & Approval</strong> - In Progress</div>
                            </div>
                            <div class="step">
                                <div class="step-number">4</div>
                                <div class="step-text"><strong>Payment Setup</strong> - Pending</div>
                            </div>
                            <div class="step">
                                <div class="step-number">5</div>
                                <div class="step-text"><strong>Account Activation</strong> - Pending</div>
                            </div>
                        </div>

                        <p><strong>What happens next?</strong></p>
                        <ul style="line-height: 1.8;">
                            <li>Our team will review your submitted documents</li>
                            <li>You'll receive an email notification once approved</li>
                            <li>After approval, you'll be prompted to complete payment</li>
                            <li>Once payment is confirmed, your account will be activated</li>
                        </ul>

                        <p>You can check your registration status anytime from your dashboard.</p>
                    </div>
                    <div class="footer">
                        <p>Thank you for choosing Rehably!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string ClinicApproved(string clinicName, string ownerName, string approveDate, string? notes, string dashboardUrl)
    {
        var notesHtml = !string.IsNullOrEmpty(notes) ? $"<p><strong>Admin Notes:</strong> {notes}</p>" : "";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Clinic Approved!</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .details { background: #f8f9fa; padding: 25px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #11998e; }
                    .button { display: inline-block; padding: 14px 35px; background: #11998e; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Clinic Application Approved!</h1>
                        <p style="font-size: 18px; margin: 10px 0 0 0;">Welcome to Rehably!</p>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{ownerName}}</strong>,</p>
                        <p>Great news! Your clinic application for <strong>{{clinicName}}</strong> has been approved on {{approveDate:MMMM dd, yyyy}}.</p>

                        <div class="details">
                            <h3 style="margin-top: 0;">Next Steps</h3>
                            <p style="margin-bottom: 15px;">To activate your account and start using Rehably, please complete your payment:</p>
                            <ol style="line-height: 2;">
                                <li>Click the button below to proceed to payment</li>
                                <li>Select your preferred subscription plan</li>
                                <li>Complete payment using your preferred method (Credit Card, PayMob, or Stripe)</li>
                                <li>Your account will be activated immediately after successful payment</li>
                            </ol>
                        </div>

                        {{notesHtml}}

                        <div style="text-align: center; margin: 30px 0;">
                            <a href="{{dashboardUrl}}" class="button">Complete Payment & Activate Account</a>
                        </div>

                        <p><strong>Payment Methods Accepted:</strong></p>
                        <ul style="line-height: 1.8;">
                            <li>Credit/Debit Cards (Visa, Mastercard)</li>
                            <li>PayMob (Egyptian payment gateway)</li>
                            <li>Stripe (International payment gateway)</li>
                        </ul>

                        <p>Need help? Our support team is available to assist you with the payment process.</p>
                    </div>
                    <div class="footer">
                        <p>We're excited to have you on board!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string ClinicRejected(string clinicName, string ownerName, string rejectDate, string reason, string retryUrl)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Clinic Application Update</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #eb3349 0%, #f45c43 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .reason-box { background: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; margin: 25px 0; border-radius: 4px; }
                    .button { display: inline-block; padding: 14px 35px; background: #eb3349; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Application Update</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{ownerName}}</strong>,</p>
                        <p>We regret to inform you that your clinic application for <strong>{{clinicName}}</strong> could not be approved at this time (reviewed on {{rejectDate:MMMM dd, yyyy}}).</p>

                        <div class="reason-box">
                            <h3 style="margin-top: 0; color: #856404;">Reason for Rejection</h3>
                            <p style="margin-bottom: 0;">{{reason}}</p>
                        </div>

                        <p><strong>What can you do?</strong></p>
                        <p>Don't worry! This is not the end. You can address the issue(s) mentioned above and submit a new application.</p>

                        <div style="text-align: center; margin: 30px 0;">
                            <a href="{{retryUrl}}" class="button">Submit New Application</a>
                        </div>

                        <p><strong>Common reasons for rejection:</strong></p>
                        <ul style="line-height: 1.8;">
                            <li>Incomplete or unclear documentation</li>
                            <li>Invalid license or registration certificates</li>
                            <li>Missing contact information</li>
                            <li>Discrepancies in provided information</li>
                        </ul>

                        <p>If you believe this rejection is in error or need clarification, please contact our support team.</p>
                    </div>
                    <div class="footer">
                        <p>We appreciate your interest in Rehably.</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string PaymentPending(string clinicName, string ownerName, string planName, decimal amount, string currency, string paymentUrl, DateTime dueDate)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Payment Pending</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .details { background: #f8f9fa; padding: 25px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #f5576c; }
                    .amount { font-size: 32px; font-weight: bold; color: #f5576c; margin: 15px 0; }
                    .button { display: inline-block; padding: 14px 35px; background: #f5576c; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Complete Your Payment</h1>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{ownerName}}</strong>,</p>
                        <p>Your clinic <strong>{{clinicName}}</strong> has been approved! To activate your account, please complete your subscription payment.</p>

                        <div class="details">
                            <h3 style="margin-top: 0;">Payment Details</h3>
                            <p><strong>Selected Plan:</strong> {{planName}}</p>
                            <p><strong>Amount Due:</strong></p>
                            <div class="amount">{{amount}} {{currency}}</div>
                            <p><strong>Due Date:</strong> {{dueDate:MMMM dd, yyyy}}</p>
                        </div>

                        <p>Complete your payment securely using any of our supported payment methods:</p>
                        <ul style="line-height: 1.8;">
                            <li>Credit/Debit Cards (Visa, Mastercard, American Express)</li>
                            <li>PayMob (Egyptian payment gateway)</li>
                            <li>Stripe (International payment gateway)</li>
                        </ul>

                        <div style="text-align: center; margin: 30px 0;">
                            <a href="{{paymentUrl}}" class="button">Pay Now</a>
                        </div>

                        <p><strong>Important:</strong></p>
                        <ul style="line-height: 1.8;">
                            <li>Your account will be activated immediately after successful payment</li>
                            <li>Payment is processed through secure, encrypted channels</li>
                            <li>You'll receive a confirmation email once payment is complete</li>
                        </ul>

                        <p>If you have any questions or encounter any issues during payment, please don't hesitate to reach out to our support team.</p>
                    </div>
                    <div class="footer">
                        <p>Almost there! Complete payment to get started.</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string PaymentCompleted(string clinicName, string ownerName, string planName, decimal amount, string currency, DateTime paidDate, DateTime startDate, DateTime endDate, string dashboardUrl)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Welcome to Rehably!</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 40px auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 10px 10px; }
                    .success-box { background: #d4edda; border-left: 4px solid #28a745; padding: 20px; margin: 25px 0; border-radius: 4px; }
                    .details { background: #f8f9fa; padding: 25px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #667eea; }
                    .button { display: inline-block; padding: 14px 35px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Welcome to Rehably!</h1>
                        <p style="font-size: 18px; margin: 10px 0 0 0;">Your Account is Now Active</p>
                    </div>
                    <div class="content">
                        <p>Dear <strong>{{ownerName}}</strong>,</p>

                        <div class="success-box">
                            <p style="margin: 0; color: #155724; font-weight: 600;">Payment Successful! Your clinic <strong>{{clinicName}}</strong> is now active.</p>
                        </div>

                        <p>We're thrilled to have you onboard! Your subscription has been activated and you can now start using all the features of Rehably.</p>

                        <div class="details">
                            <h3 style="margin-top: 0;">Subscription Details</h3>
                            <p><strong>Plan:</strong> {{planName}}</p>
                            <p><strong>Amount Paid:</strong> {{amount}} {{currency}}</p>
                            <p><strong>Payment Date:</strong> {{paidDate:MMMM dd, yyyy}}</p>
                            <p><strong>Subscription Period:</strong> {{startDate:MMMM dd, yyyy}} - {{endDate:MMMM dd, yyyy}}</p>
                        </div>

                        <h3>What's Next?</h3>
                        <ul style="line-height: 1.8;">
                            <li><strong>Complete your profile:</strong> Add your clinic details, working hours, and services</li>
                            <li><strong>Add your staff:</strong> Invite your team members to collaborate</li>
                            <li><strong>Set up patients:</strong> Start adding patient records and appointments</li>
                            <li><strong>Explore features:</strong> Discover all the tools available to manage your clinic efficiently</li>
                        </ul>

                        <div style="text-align: center; margin: 30px 0;">
                            <a href="{{dashboardUrl}}" class="button">Go to Dashboard</a>
                        </div>

                        <p>If you need any assistance getting started, our help documentation and support team are just a click away.</p>
                    </div>
                    <div class="footer">
                        <p>Thank you for choosing Rehably. Let's build a successful practice together!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public static string CashInvoice(
        string clinicName,
        string ownerName,
        string invoiceNumber,
        DateTime invoiceDate,
        string planName,
        decimal amount,
        string currency,
        DateTime startDate,
        DateTime endDate,
        string? notes)
    {
        var notesHtml = !string.IsNullOrEmpty(notes) ? $"<p><strong>Notes:</strong> {notes}</p>" : "";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Invoice - {{invoiceNumber}}</title>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; }
                    .container { max-width: 700px; margin: 40px auto; padding: 20px; }
                    .header { background: #2c3e50; color: white; padding: 25px; border-radius: 8px 8px 0 0; }
                    .header h1 { margin: 0; font-size: 24px; }
                    .invoice-info { color: #ecf0f1; font-size: 14px; margin-top: 10px; }
                    .content { background: white; padding: 40px 30px; border-radius: 0 0 8px 8px; }
                    .invoice-details { display: flex; justify-content: space-between; margin-bottom: 30px; }
                    .detail-section { flex: 1; }
                    .detail-section h3 { margin-top: 0; color: #2c3e50; font-size: 16px; border-bottom: 2px solid #3498db; padding-bottom: 10px; }
                    .table { width: 100%; border-collapse: collapse; margin: 30px 0; }
                    .table th { background: #3498db; color: white; padding: 12px; text-align: left; }
                    .table td { padding: 12px; border-bottom: 1px solid #ecf0f1; }
                    .table tr:last-child td { border-bottom: none; }
                    .total-row { background: #ecf0f1; font-weight: bold; }
                    .total { font-size: 24px; color: #27ae60; text-align: right; margin: 20px 0; }
                    .footer { text-align: center; margin-top: 30px; color: #777; font-size: 13px; }
                    .stamp { border: 3px solid #27ae60; color: #27ae60; display: inline-block; padding: 15px 25px; font-size: 18px; font-weight: bold; transform: rotate(-5deg); margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>INVOICE</h1>
                        <div class="invoice-info">Invoice #{{invoiceNumber}} | {{invoiceDate:MMMM dd, yyyy}}</div>
                    </div>
                    <div class="content">
                        <div class="invoice-details">
                            <div class="detail-section">
                                <h3>Bill To:</h3>
                                <p><strong>{{clinicName}}</strong></p>
                                <p>{{ownerName}}</p>
                            </div>
                            <div class="detail-section" style="text-align: right;">
                                <h3>Rehably</h3>
                                <p>Physiotherapy Clinic<br>Management Solution</p>
                            </div>
                        </div>

                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Description</th>
                                    <th style="text-align: right;">Amount</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>
                                        <strong>{{planName}} Subscription</strong><br>
                                        <small>Period: {{startDate:MMMM dd, yyyy}} - {{endDate:MMMM dd, yyyy}}</small>
                                    </td>
                                    <td style="text-align: right;">{{amount:N2}} {{currency}}</td>
                                </tr>
                                <tr class="total-row">
                                    <td><strong>Total Paid (Cash)</strong></td>
                                    <td style="text-align: right;">{{amount:N2}} {{currency}}</td>
                                </tr>
                            </tbody>
                        </table>

                        <div class="total">
                            TOTAL: {{amount:N2}} {{currency}}
                        </div>

                        <div style="text-align: center;">
                            <div class="stamp">PAID</div>
                        </div>

                        {{notesHtml}}

                        <p style="margin-top: 30px; color: #7f8c8d; font-size: 14px;">
                            <strong>Payment Terms:</strong> Paid in full via cash payment.<br>
                            <strong>Payment Method:</strong> Cash<br>
                            <strong>Status:</strong> Paid
                        </p>
                    </div>
                    <div class="footer">
                        <p>Thank you for your business!</p>
                        <p>&copy; {{DateTime.Now.Year}} Rehably. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
