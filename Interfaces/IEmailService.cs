using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using TimeMgtReportService.Models;

namespace TimeMgtReportService.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(MailRequest emailRequestbool, bool hasAttachment = false);

        //Task SendEmailAsync2(MailRequest emailRequest);
        string AttachmentFile { get; set; }
    }
}
