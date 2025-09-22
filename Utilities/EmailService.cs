using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;

    public EmailService(string smtpServer, int smtpPort, string smtpUser, string smtpPass)
    {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
    }
    // Option 1: Store template in file
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Validate email addresses
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email address cannot be empty", nameof(toEmail));
            }

            // Get the project root directory (going up from bin/Debug)
            string projectDirectory = Directory.GetCurrentDirectory();
            string templatePath = Path.Combine(projectDirectory, "Templates", "email-template.html");

            // For debugging, you can print the path
            Console.WriteLine($"Looking for template at: {templatePath}");

            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.EnableSsl = true;

                // Create the from address separately to catch any issues
                MailAddress fromAddress;
                try
                {
                    fromAddress = new MailAddress(_smtpUser, "CORE HR Business Solutions");
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Invalid sender email address: {_smtpUser}", ex);
                }

                var mailMessage = new MailMessage
                {
                    From = fromAddress,
                    Subject = subject,
                    Body = htmlTemplate
                        .Replace("{user}", toEmail)
                        .Replace("{body}", body),
                    IsBodyHtml = true,
                };

                // Add recipient separately to catch any issues
                try
                {
                    mailMessage.To.Add(toEmail);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Invalid recipient email address: {toEmail}", ex);
                }

                await client.SendMailAsync(mailMessage);
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"Failed to send email: {ex.Message}");
            throw; // Re-throw to let the caller handle it
        }
    }

    public async Task SendWelcomeEMailAsync(string toEmail, string subject, string body)
    {
        // Get the project root directory (going up from bin/Debug)
        string projectDirectory = Directory.GetCurrentDirectory();
        string templatePath = Path.Combine(projectDirectory, "Templates", "welcome-email.html");

        // For debugging, you can print the path
        Console.WriteLine($"Looking for template at: {templatePath}");

        string htmlTemplate = await File.ReadAllTextAsync(templatePath);

        using (var client = new SmtpClient(_smtpServer, _smtpPort))
        {
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
            client.EnableSsl = true;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser, "CORE HR Business Solutions"),
                Subject = subject,
                Body = htmlTemplate
                    .Replace("{userId}", body),
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
    }
}