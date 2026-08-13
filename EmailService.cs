using System.Collections;
using System.Linq;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TimeMgtReportService.Interfaces;
using TimeMgtReportService.Models;
using File = System.IO.File;

namespace TimeMgtReportService
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> logger;
        private readonly EmailSettings emailSettings;

        public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> options)
        {
            this.logger = logger;
            //MailingSmtp = new SmtpClient();
            this.emailSettings = options.Value;

            //MailingSmtp.MessageSent += this.Smtp_MessageSent;
        }
        public async Task SendEmailAsync(MailRequest emailRequest, bool hasAttachment = false)
        {
            this.logger.LogInformation("Preparing email ...");
            string attachmentName = $"attachment{DateTime.Now.Date.ToString("MMddyyyy")}.csv";

            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(this.emailSettings.EmailFrom);
            email.From.Add(MailboxAddress.Parse(this.emailSettings.EmailFrom));

            InternetAddressList list = new InternetAddressList();
            if (emailRequest.ToEmail != null && emailRequest.ToEmail.Contains(","))
            {
                foreach (var address in emailRequest.ToEmail.Split(',').ToList())
                {
                    list.Add(MailboxAddress.Parse(address.Trim()));
                }
            }
            else
            {
                list.Add(MailboxAddress.Parse(emailRequest.ToEmail!.Trim()));
            }
            email.To.AddRange(list);

            //email.To.Add(MailboxAddress.Parse(this.emailSettings.EmailTo));
            email.Subject = emailRequest.Subject;
            var builder = new BodyBuilder();

            this.logger.LogInformation("Building attachment ...");

            if (hasAttachment)
            {
                byte[] fileBytes;
                if (File.Exists(this.AttachmentFile))
                {
                    FileStream file = new FileStream(this.AttachmentFile, FileMode.Open, FileAccess.Read);
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        fileBytes = ms.ToArray();
                    }

                    builder.Attachments.Add(attachmentName, fileBytes, ContentType.Parse("application/octet-stream"));
                }
            }

            builder.HtmlBody = emailRequest.Body;
            email.Body = builder.ToMessageBody();

            //OpenSmtp();
            using var smtp = new SmtpClient();
            smtp.MessageSent += this.Smtp_MessageSent;
            smtp.Connected += this.Smtp_Connected;
            smtp.Disconnected += this.Smtp_Disconnected;
            //smtp.Connect(this.emailSettings.Host, emailSettings.Port, SecureSocketOptions.Auto);
            //smtp.Authenticate();
            this.logger.LogInformation("Sending email ... " + email.To);

            if (!this.emailSettings.DisableEmailing)
            {
                smtp.Connect(this.emailSettings.Host, emailSettings.Port, SecureSocketOptions.Auto);
                await smtp.SendAsync(email);
            }
            
            //CloseSmtp();
            smtp.Disconnect(true);
        }

        public string AttachmentFile { get; set; }

        private void Smtp_Connected(object? sender, MailKit.ConnectedEventArgs e)
        {
            this.logger.LogInformation("smtp connected ...");
        }

        private void Smtp_Disconnected(object? sender, MailKit.DisconnectedEventArgs e)
        {
            this.logger.LogInformation("smtp disconnected ...");
        }

        private void Smtp_MessageSent(object? sender, MailKit.MessageSentEventArgs e)
        {
            this.logger.LogInformation("{0} - Email server response ... {1}", string.Join(",", e.Message.To), e.Response);
        }
    }
}
