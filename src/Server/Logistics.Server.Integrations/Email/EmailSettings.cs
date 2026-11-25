namespace Logistics.Server.Integrations.Email;

/// <summary>
/// Externalized email provider settings bound from configuration ("Email").
/// Never hardcode credentials in source code.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Supported values: "None", "Smtp", "SendGrid".
    /// </summary>
    public string Provider { get; set; } = "None";

    public string FromEmail { get; set; } = "noreply@logistics.internal";
    public string FromName { get; set; } = "Logistics Management System";
    public int TimeoutSeconds { get; set; } = 15;

    public SmtpOptions Smtp { get; set; } = new();
    public SendGridOptions SendGrid { get; set; } = new();

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed class SendGridOptions
    {
        public string BaseUrl { get; set; } = "https://api.sendgrid.com";
        public string? ApiKey { get; set; }
    }
}
