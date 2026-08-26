using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Options;
using TimeMgtReportService.Helpers;
using TimeMgtReportService.Interfaces;
using TimeMgtReportService.Models;

namespace TimeMgtReportService
{
    public class ReportService : BackgroundService
    {
        private readonly ILogger<ReportService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IEmailService _emailService;
        private readonly IOptions<EmailSettings> _options;
        private DateTime _retrievedStartDate;

        public ReportService(ILogger<ReportService> logger, IDatabaseService databaseService, IEmailService emailService, IOptions<EmailSettings> options)
        {
            this._logger = logger;
            this._databaseService = databaseService;
            this._emailService = emailService;
            this._options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var nextSendingTimeDiff = TimeSpan.Zero;

            while (!stoppingToken.IsCancellationRequested)
            {
                this._logger.LogInformation("====== Schedule Execution Starts ======");
                try
                {
                    this._retrievedStartDate = DateTime.Now.AddDays(-this._options.Value.DataRetrievedUpToDays);
                    var startDate = this._retrievedStartDate;
                    var endDate = DateTime.Now;
                    var timeLogs = this._databaseService.GetTimeLogsRpt(startDate, endDate).ToList();
                    var timeMonthLogs = this._databaseService.GetTimeLogsRpt(endDate.AddMonths(-1).AddDays(-1), endDate).ToList();
                    var users = this._databaseService.GetUsers();

                    var managers = this._databaseService.GetManagers();
                    var noLogsUsers = Helper.GetUserNoLog(users, timeMonthLogs);
                    var noLogOneDaysUsers = Helper.GetUserMissingDaysNoLog(timeLogs, this._options.Value.MissLogDaysX, startDate, endDate);
                    var noLogFiveDaysUsers = noLogOneDaysUsers.Where(usr=>usr.Count > 4).ToList();

                    var employeeEmailsNoOneDays = Helper.CreatingEmployeeEmail(noLogOneDaysUsers);
                    
                    var managerEmailsNoFiveDays = Helper.CreatingManagerEmail(noLogFiveDaysUsers, managers.ToList());

                    var bossEmailsNoLogs = new List<NotificationEmail>();
                    var valueEmailToBoss = this._options.Value.EmailToBoss;
                    if (valueEmailToBoss != null && noLogsUsers.Any())
                    {
                        if (valueEmailToBoss.Contains(","))
                        {
                            foreach (var email in valueEmailToBoss.Split(",").ToArray())
                            {
                                bossEmailsNoLogs.Add(Helper.CreatingRobEmail(noLogsUsers, email.Trim()));
                            }
                        }
                        else
                        {
                            bossEmailsNoLogs.Add(Helper.CreatingRobEmail(noLogsUsers, valueEmailToBoss.Trim()));
                        }
                    }

                    var finalEmails = new List<NotificationEmail>();

                    //sending email
                    if (employeeEmailsNoOneDays.Count > 0)
                    {
                        await this.SendingEmail(employeeEmailsNoOneDays);
                        finalEmails = finalEmails.Concat(employeeEmailsNoOneDays).ToList();
                    }

                    if (managerEmailsNoFiveDays.Count > 0)
                    {
                        await this.SendingEmail(managerEmailsNoFiveDays);
                        finalEmails = finalEmails.Concat(managerEmailsNoFiveDays).ToList();
                    }

                    if (bossEmailsNoLogs.Count > 0)
                    {
                        await this.SendingEmail(bossEmailsNoLogs);
                        finalEmails = finalEmails.Concat(bossEmailsNoLogs).ToList();
                    }

                    //Write into csv file ...
                    this.SaveCsvFile(timeLogs.ToList(), startDate, endDate);
                    this.SaveCsvFile(finalEmails.ToList(), startDate, endDate);

                    this._logger.LogInformation("All Emails have been sent - ");

                    Thread.Sleep(3000);

                    //The next sending day ...
                    //this._logger.LogInformation("Next Review Date - " + Helper.GetNextWeekday((DayOfWeek)this._options.Value.DayOfWeek).AddHours(+1));
                    this._logger.LogInformation("Next Review Date - " + (DateTime.Today.DayOfWeek == DayOfWeek.Friday ? DateTime.Now.AddHours(+72) : DateTime.Now.AddHours(+24)));
                    //The reason for adding extra hour is to avoid collision with AcuGrav service emailing time.
                    //nextSendingTimeDiff = (Helper.GetNextWeekday((DayOfWeek)this._options.Value.DayOfWeek).AddHours(+1) - DateTime.Now);
                    //Set nest sending time after 24 hours.

                    //nextSendingTimeDiff = DateTime.Today.DayOfWeek == DayOfWeek.Friday ? new TimeSpan(72, 0, 0) : new TimeSpan(24, 0, 0); 
                    nextSendingTimeDiff = this.GetNext5PmWeekday(DateTime.Now, 18).TimeOfDay;
                    this._logger.LogInformation("Differences in seconds - " + nextSendingTimeDiff.TotalSeconds);
                    this._logger.LogInformation("Converting to seconds - " + Convert.ToInt32(nextSendingTimeDiff.TotalSeconds));
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, ex.Message);
                }
                await Task.Delay(1000 * Convert.ToInt32(nextSendingTimeDiff.TotalSeconds), stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("--------------------------------------------");
            this._logger.LogInformation("====== Time-Management Service Starts ======");
            this._logger.LogInformation("--------------------------------------------");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Service stopped");
            return base.StopAsync(cancellationToken);
        }

        private Task SendingEmail(List<NotificationEmail> notifications)
        {
            foreach (var mailRequest in notifications.Select(notification => new MailRequest
                     {
                         ToEmail = notification.Email,
                         Subject = notification.Subject,
                         Body = this.GetHtmlContent(notification.Body)
                     }))
            {
                this._emailService.SendEmailAsync(mailRequest);
            }

            return Task.FromResult(Task.CompletedTask);
        }

        private string GetHtmlContent(string content)
        {
            string response = "<h3>Time-Management Notification</h3>";
            response += "<h4>From " + _retrievedStartDate.ToString("MM-dd-yyyy") + " To " + DateTime.Now.ToString("MM-dd-yyyy") + "</h4>";
            response += "<h4>" + content + "</h4>";
            response += "<div><h5>Contact the support if there is any issue</h5></div>";
            return response;
        }

        private void SaveCsvFile<T>(List<T> csvData, DateTime startDate, DateTime endDate)
        {
            string rptName = "Time-Mgt-Weekly-Rpt";
            string fileNameBase = string.Format("{0}-{1}-{2}-{3}", rptName, startDate.ToString("MMddyyyy"), endDate.ToString("MMddyyyy"), DateTime.Now.Millisecond + ".csv");

            this._emailService.AttachmentFile = this._options.Value.AttachmentPath + fileNameBase;

            using var writer = new StreamWriter(this._options.Value.AttachmentPath + fileNameBase);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<T>();
            csv.NextRecord();
            foreach (var record in csvData)
            {
                csv.WriteRecord(record);
                csv.NextRecord();
            }
        }
        private DateTime GetNext5PmWeekday(DateTime now, int clockHour)
        {
            // Set target to today at 17:00 (5:00 PM), which means clockHour = 17
            var target = new DateTime(now.Year, now.Month, now.Day, clockHour, 0, 0);

            // If it is already past 5:00 PM today, move to tomorrow
            if (now >= target)
            {
                target = target.AddDays(1);
            }

            // Skip Saturday (DayOfWeek.Saturday) and Sunday (DayOfWeek.Sunday)
            while (target.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                target = target.AddDays(1);
            }

            return target;
        }
    }
}
