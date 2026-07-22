using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CsvHelper;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using TimeMgtReportService.Helpers;
using TimeMgtReportService.Interfaces;
using TimeMgtReportService.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Boolean = System.Boolean;

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
            TimeSpan nextSendingTimeDiff = TimeSpan.Zero;

            while (!stoppingToken.IsCancellationRequested)
            {
                this._logger.LogInformation("====== Schedule Execution Starts ======");
                try
                {
                    _retrievedStartDate = DateTime.Now.AddDays(-this._options.Value.DataRetrievedUpToDays);
                    DateTime startDate = _retrievedStartDate;
                    DateTime endDate = DateTime.Now;
                    var timeLogs = this._databaseService.GetTimeLogsRpt(startDate, endDate).ToList();
                    var users = _databaseService.GetUsers();

                    var managers = _databaseService.GetManagers();
                    var noLogsUsers = Helper.GetUserNoLog(users, timeLogs);
                    var noLog10DaysUsers = Helper.GetUserMissingDaysNoLog(timeLogs, this._options.Value.MissLogDays10)
                        .Join(users, s=>s.Id, usrs=>usrs.Id, (s, usrs)=> new User(usrs.Id, usrs.GroupId, usrs.Email, usrs.UserName)).ToList();
                    //var noLog15DaysUsers = Helper.GetUserMissingDaysNoLog(timeLogs, this._options.Value.MissLogDays15);

                    //testing 
                    //var systemEngineering = users.Where(s => s.GroupId == 5 || s.GroupId == 4);
                    //var managerEmails = Helper.CreatingManagerEmail(systemEngineering.ToList(), managers.ToList());
                    //var employeeEmails10days = Helper.CreatingEmployeeEmail(systemEngineering.ToList());
                    //var managerEmail = Helper.GetManagerEmail(managers.ToList(), 1);

                    var employeeEmailsNo10days = Helper.CreatingEmployeeEmail(noLog10DaysUsers);
                    //var managerEmailsNo15days = Helper.CreatingManagerEmail(noLog15DaysUsers, managers.ToList());
                    var managerEmailsNoLogs = Helper.CreatingManagerEmail(noLogsUsers, managers.ToList());

                    List<NotificationEmail>?bossEmailsNoLogs = new List<NotificationEmail>();
                    var valueEmailToBoss = this._options.Value.EmailToBoss;
                    if (valueEmailToBoss != null)
                    {
                        if (valueEmailToBoss.Contains(","))
                        {
                            foreach (string email in valueEmailToBoss.Split(",").ToArray())
                            {
                                bossEmailsNoLogs.Add(Helper.CreatingRobEmail(noLogsUsers, email.Trim()));
                            }
                        }
                        else
                        {
                            bossEmailsNoLogs.Add(Helper.CreatingRobEmail(noLogsUsers, valueEmailToBoss.Trim()));
                        }
                    }

                    //var robEmailsNoLogs = Helper.CreatingRobEmail(noLogsUsers, this._options.Value.EmailToBoss);

                    //testing
                    //managerEmailsNologs.Remove(managerEmailsNologs.First());
                    //managerEmailsNologs.Remove(managerEmailsNologs.First());
                    //managerEmailsNologs.First().Email = "wenli.huang@airgas.com";
                    //managerEmailsNologs.Skip(1).First().Email = "wenli.huang@airgas.com";
                    //testing

                    List<NotificationEmail>? finalEmails = new List<NotificationEmail>();

                    //sending email
                    if (employeeEmailsNo10days.Count > 0)
                    {
                        await SendingEmail(employeeEmailsNo10days);
                        finalEmails = finalEmails.Concat(employeeEmailsNo10days).ToList();
                    }

                    if (managerEmailsNoLogs.Count > 0)
                    {
                        await SendingEmail(managerEmailsNoLogs);
                        finalEmails = finalEmails.Concat(managerEmailsNoLogs).ToList();
                    }

                    if (bossEmailsNoLogs.Count > 0)
                    {
                        await SendingEmail(bossEmailsNoLogs);
                        finalEmails = finalEmails.Concat(bossEmailsNoLogs).ToList();
                    }

                    //if (!string.IsNullOrEmpty(robEmailsNoLogs.Email))
                    //{
                    //    await SendingEmail(robEmailsNoLogs);
                    //    FinalEmails.Add(robEmailsNoLogs);
                    //}

                    //Write into csv file ...
                    this.SaveCsvFile(timeLogs.ToList(), startDate, endDate);
                    this.SaveCsvFile(finalEmails.ToList(), startDate, endDate);

                    this._logger.LogInformation("All Emails have been sent - ");

                    Thread.Sleep(3000);

                    //The next sending day ...
                    this._logger.LogInformation("Next Review Date - " + Helper.GetNextWeekday((DayOfWeek)this._options.Value.DayOfWeek).AddHours(+1));
                    //The reason for adding extra hour is to avoid collision with AcuGrav service emailing time.
                    nextSendingTimeDiff = (Helper.GetNextWeekday((DayOfWeek)this._options.Value.DayOfWeek).AddHours(+1) - DateTime.Now);
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

        private async Task SendingEmail()
        {
            MailRequest mailRequest = new MailRequest
            {
                ToEmail = this._options.Value.EmailTo,
                Subject = this._options.Value.Subject,
                Body = this.GetHtmlContent()
            };
            await this._emailService.SendEmailAsync(mailRequest);
        }

        private Task SendingEmail(NotificationEmail notification)
        {
            MailRequest mailRequest = new MailRequest
            {
                ToEmail = notification.Email,
                Subject = notification.Subject,
                Body = this.GetHtmlContent(notification.Body)
            };

            this._emailService.SendEmailAsync(mailRequest);
            return Task.CompletedTask;
        }

        private Task SendingEmail(List<NotificationEmail> notifications)
        {
            foreach (var notification in notifications)
            {
                MailRequest mailRequest = new MailRequest
                {
                    ToEmail = notification.Email,
                    Subject = notification.Subject,
                    Body = this.GetHtmlContent(notification.Body)
                };

                this._emailService.SendEmailAsync(mailRequest);
            }

            return Task.FromResult(Task.CompletedTask);
        }

        private string GetHtmlContent()
        {
            string response = "<h3>Time-Management Weekly Report</h3>";
            response += "<h4>Please review the attachment</h4>";
            response += "<h4>From " + DateTime.Now.AddDays(-7).ToString("MM-dd-yyyy") + " To " + DateTime.Now.ToString("MM-dd-yyyy") + "</h4>";
            response += "<div><h5>Contact the support if there is any issue</h5></div>";
            return response;
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
    }
}
