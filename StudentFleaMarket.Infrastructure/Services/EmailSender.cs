using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.Infrastructure.Services;

public class EmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "no-reply@campusfleamarket.local";
    public string FromName { get; set; } = "Campus Thrift & Exchange Hub";

    /// <summary>
    /// true = chỉ ghi log nội dung email ra console (dùng cho môi trường Development
    /// khi chưa cấu hình SMTP thật).
    /// </summary>
    public bool LogOnly { get; set; } = true;
}

public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
    {
        if (_options.LogOnly)
        {
            _logger.LogInformation(
                "[EMAIL-LOG] To={To} | Subject={Subject} | Body={Body}",
                to, subject, isHtml ? StripHtml(body) : body);
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml,
            BodyEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(new MailAddress(to));

        await client.SendMailAsync(message, ct);
    }

    public Task SendOtpAsync(string to, string fullName, string code, CancellationToken ct = default)
    {
        var subject = "Mã xác thực tài khoản Student Flea Market";
        var body = $@"
            <div style='font-family:Segoe UI,Arial,sans-serif;max-width:520px'>
                <h2 style='color:#16a34a'>Xác thực email</h2>
                <p>Chào <b>{System.Net.WebUtility.HtmlEncode(fullName)}</b>,</p>
                <p>Mã xác thực của bạn là:</p>
                <p style='font-size:28px;font-weight:bold;letter-spacing:6px;color:#111'>{code}</p>
                <p>Mã có hiệu lực trong <b>10 phút</b>. Nếu bạn không yêu cầu mã này, hãy bỏ qua email.</p>
            </div>";

        return SendAsync(to, subject, body, true, ct);
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ")
            .Trim();
    }
}
