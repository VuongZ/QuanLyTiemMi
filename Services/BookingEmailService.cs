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
        var section = _configuration.GetSection("Email");
        var host = section["Host"];
        var from = section["From"];
        var to = section["To"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(to))
        {
            _logger.LogInformation("Email settings are not configured. Booking email was skipped.");
            return;
        }

        try
        {
            using var message = new MailMessage(from, to)
            {
                Subject = "TinMI có lịch làm mi mới",
                Body = $"""
                    Khách vừa đặt lịch làm mi.

                    Họ tên: {khachHang.TenKh}
                    Số điện thoại: {khachHang.Sdt}
                    Lịch làm mi: {khachHang.NgayDK:dd/MM/yyyy HH:mm}
                    """
            };

            using var client = new SmtpClient(host, section.GetValue("Port", 587))
            {
                EnableSsl = section.GetValue("EnableSsl", true)
            };

            var username = section["UserName"];
            var password = section["Password"];
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send booking email.");
        }
    }
}
