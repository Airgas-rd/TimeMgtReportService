using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Interfaces;

namespace TimeMgtReportService.Models
{
    public class NotificationEmail : ICsvReport
    {
        public string? Email { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
