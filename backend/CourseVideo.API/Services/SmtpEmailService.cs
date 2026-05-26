using CourseVideo.API.Configuration;
using CourseVideo.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace CourseVideo.API.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> smtpOptions, ILogger<SmtpEmailService> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            
            // Connect
            await smtp.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, SecureSocketOptions.StartTls, cancellationToken);
            
            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_smtpOptions.Username) && !string.IsNullOrEmpty(_smtpOptions.Password))
            {
                await smtp.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, cancellationToken);
            }

            // Send
            await smtp.SendAsync(email, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation("Gửi email thành công tới {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email tới {Email}", toEmail);
            throw new InvalidOperationException("Không thể gửi email. Vui lòng thử lại sau.", ex);
        }
    }
}
