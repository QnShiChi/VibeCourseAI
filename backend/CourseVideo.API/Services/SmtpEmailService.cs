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
            var email = new MimeMessage(); // MimeMessage là một lớp trong thư viện MimeKit dùng để tạo và quản lý các email, bao gồm các thành phần như người gửi, người nhận, tiêu đề, nội dung và các phần đính kèm của email.
            email.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail)); // MailboxAddress là một lớp trong thư viện MimeKit đại diện cho một địa chỉ email, bao gồm tên hiển thị và địa chỉ email thực tế. Trong đoạn mã này, nó được sử dụng để thiết lập địa chỉ người gửi của email bằng cách lấy thông tin từ cấu hình SmtpOptions.
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject; // Subject là tiêu đề của email, được thiết lập bằng cách gán giá trị cho thuộc tính Subject của đối tượng MimeMessage. Trong đoạn mã này, giá trị của subject được truyền vào từ tham số của phương thức SendEmailAsync.
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody }; // TextPart là một lớp trong thư viện MimeKit đại diện cho phần nội dung của email, có thể là văn bản thuần túy hoặc HTML. Trong đoạn mã này, nó được sử dụng để tạo một phần nội dung HTML cho email bằng cách gán giá trị của htmlBody vào thuộc tính Text của đối tượng TextPart, và sau đó gán đối tượng TextPart này cho thuộc tính Body của đối tượng MimeMessage.

            using var smtp = new SmtpClient();
            
            // Connect
            await smtp.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, SecureSocketOptions.StartTls, cancellationToken); // ConnectAsync là một phương thức bất đồng bộ trong lớp SmtpClient của thư viện MailKit, được sử dụng để thiết lập kết nối đến máy chủ SMTP. Trong đoạn mã này, phương thức ConnectAsync được gọi với các tham số bao gồm địa chỉ máy chủ SMTP (Host), cổng kết nối (Port), và tùy chọn bảo mật (SecureSocketOptions.StartTls) để đảm bảo rằng kết nối được mã hóa bằng TLS. Tham số cancellationToken cũng được truyền vào để hỗ trợ hủy bỏ thao tác nếu cần thiết.
            
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
