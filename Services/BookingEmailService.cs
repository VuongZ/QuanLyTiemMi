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

    public Task<bool> SendTestEmailAsync()
    {
        var testBooking = new KhachHang
        {
            TenKh = "TinMI Test",
            Sdt = "0000000000",
            NgayDK = DateTime.Now
        };

        return SendBookingCreatedAsync(testBooking);
    }

    public async Task<bool> SendBookingCreatedAsync(KhachHang khachHang)
    {
        var settings = GetEmailSettings();

        _logger.LogInformation(
            "SMTP config: host={Host}, port={Port}, ssl={UseSsl}, usernameSet={UsernameSet}, passwordSet={PasswordSet}, from={FromEmail}, to={ToEmail}",
            settings.Host,
            settings.Port,
            settings.UseSsl,
            !string.IsNullOrWhiteSpace(settings.Username),
            !string.IsNullOrWhiteSpace(settings.Password),
            settings.FromEmail,
            settings.ToEmail);

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.FromEmail) ||
            string.IsNullOrWhiteSpace(settings.ToEmail))
        {
            _logger.LogWarning("SMTP settings are incomplete. Booking email was skipped.");
            return false;
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
                EnableSsl = settings.UseSsl,
                Timeout = 10000
            };

            if (!string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password))
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Booking email was sent to {ToEmail}.", settings.ToEmail);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Could not send booking email. Booking was still saved. SMTP error: {Message}", exception.Message);
            return false;
        }
    }

    private EmailSettings GetEmailSettings()
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var emailSettings = _configuration.GetSection("Email");

        var host = Read(smtpSettings, emailSettings, "Host");
        var port = ReadInt(smtpSettings, emailSettings, "Port", 587);
        var useSsl = ReadBool(smtpSettings, emailSettings, "UseSsl", "EnableSsl", true);
        var fromEmail = Read(smtpSettings, emailSettings, "FromEmail", "From");
        var toEmail = Read(smtpSettings, emailSettings, "ToEmail", "To");

        if (IsGmailSmtp(host, port) && !useSsl)
        {
            _logger.LogWarning("Gmail SMTP on port {Port} requires SSL/STARTTLS. Forcing SSL on.", port);
            useSsl = true;
        }

        return new EmailSettings
        {
            Host = host,
            Port = port,
            UseSsl = useSsl,
            Username = Read(smtpSettings, emailSettings, "Username", "UserName"),
            Password = Read(smtpSettings, emailSettings, "Password"),
            FromEmail = fromEmail,
            FromName = Read(smtpSettings, emailSettings, "FromName"),
            ToEmail = string.IsNullOrWhiteSpace(toEmail) ? fromEmail : toEmail
        };
    }

    private static bool IsGmailSmtp(string? host, int port)
    {
        return host?.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase) == true &&
            port is 465 or 587;
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
