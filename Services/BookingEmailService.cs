using System.Net;
using System.Net.Mail;
using TinMI.Models;

namespace TinMI.Services;

public class BookingEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(IConfiguration configuration, ILogger<BookingEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendBookingCreatedAsync(KhachHang khachHang)
    {
        var settings = GetEmailSettings();

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.FromEmail) ||
            string.IsNullOrWhiteSpace(settings.ToEmail))
        {
            _logger.LogWarning("SMTP settings are incomplete. Booking email was skipped.");
            return;
        }

        try
        {
            var fromAddress = string.IsNullOrWhiteSpace(settings.FromName)
                ? new MailAddress(settings.FromEmail)
                : new MailAddress(settings.FromEmail, settings.FromName);

            using var message = new MailMessage
            {
                From = fromAddress,
                Subject = "TinMI có lịch làm mi mới",
                Body = $"""
                    Khách vừa đặt lịch làm mi.

                    Họ tên: {khachHang.TenKh}
                    Số điện thoại: {khachHang.Sdt}
                    Lịch làm mi: {khachHang.NgayDK:dd/MM/yyyy HH:mm}
                    """
            };
            message.To.Add(settings.ToEmail);

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password))
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Booking email was sent to {ToEmail}.", settings.ToEmail);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send booking email.");
        }
    }

    private EmailSettings GetEmailSettings()
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var emailSettings = _configuration.GetSection("Email");

        var fromEmail = Read(smtpSettings, emailSettings, "FromEmail", "From");
        var toEmail = Read(smtpSettings, emailSettings, "ToEmail", "To");

        return new EmailSettings
        {
            Host = Read(smtpSettings, emailSettings, "Host"),
            Port = ReadInt(smtpSettings, emailSettings, "Port", 587),
            UseSsl = ReadBool(smtpSettings, emailSettings, "UseSsl", "EnableSsl", true),
            Username = Read(smtpSettings, emailSettings, "Username", "UserName"),
            Password = Read(smtpSettings, emailSettings, "Password"),
            FromEmail = fromEmail,
            FromName = Read(smtpSettings, emailSettings, "FromName"),
            ToEmail = string.IsNullOrWhiteSpace(toEmail) ? fromEmail : toEmail
        };
    }

    private static string? Read(IConfigurationSection primary, IConfigurationSection fallback, string primaryKey, string? fallbackKey = null)
    {
        return primary[primaryKey] ?? fallback[fallbackKey ?? primaryKey];
    }

    private static int ReadInt(IConfigurationSection primary, IConfigurationSection fallback, string key, int defaultValue)
    {
        return int.TryParse(Read(primary, fallback, key), out var value) ? value : defaultValue;
    }

    private static bool ReadBool(
        IConfigurationSection primary,
        IConfigurationSection fallback,
        string primaryKey,
        string fallbackKey,
        bool defaultValue)
    {
        return bool.TryParse(Read(primary, fallback, primaryKey, fallbackKey), out var value) ? value : defaultValue;
    }

    private sealed class EmailSettings
    {
        public string? Host { get; init; }
        public int Port { get; init; }
        public bool UseSsl { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
        public string? FromEmail { get; init; }
        public string? FromName { get; init; }
        public string? ToEmail { get; init; }
    }
}
