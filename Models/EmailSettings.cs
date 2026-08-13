namespace TimeMgtReportService.Models
{
    public class EmailSettings
    {
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string? EmailToBoss { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string Subject { get; set; }
        public int Port { get; set; }
        public int DayOfWeek { get; set; }
        public int DataRetrievedUpToDays { get; set; }
        public int MissLogDaysX { get; set; }
        public int MissLogDaysY { get; set; }
        public string AttachmentPath { get; set; }
        public bool DisableEmailing { get; set; }
    }
}
