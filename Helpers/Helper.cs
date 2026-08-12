using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TimeMgtReportService.Models;
using File = TimeMgtReportService.Models.File;

namespace TimeMgtReportService.Helpers
{
    public static class Helper
    {
        public static IConfiguration config;
        public static void Initialize(IConfiguration configuration)
        {
            config = configuration;
        }

        public static DateTime GetNextWeekday(DayOfWeek day)
        {
            DateTime result = DateTime.Now.Date.AddDays(1);
            while (result.DayOfWeek != day)
                result = result.AddDays(1);
            return result;
        }

        /// <summary>
        /// Get the first day of the week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The first date of the week.</returns>
        public static DateTime FirstDayOfWeek(DateTime date)
        {
            DayOfWeek fdow = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int offset = fdow - date.DayOfWeek;
            DateTime fdowDate = date.AddDays(offset);
            return fdowDate;
        }

        /// <summary>
        /// Get the last day of the week.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The last date of the week.</returns>
        public static DateTime LastDayOfWeek(DateTime date)
        {
            DateTime ldowDate = FirstDayOfWeek(date).AddDays(6);
            return ldowDate;
        }
        public static File ToZip(List<File> files, string fileName)
        {
            var compressedFileStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var zipEntry = zipArchive.CreateEntry(file.FileName);

                    using (var originalFileStream = new MemoryStream(file.Bytes))
                    using (var zipEntryStream = zipEntry.Open())
                    {
                        originalFileStream.CopyTo(zipEntryStream);
                    }
                }
            }

            return new File()
            {
                Bytes = compressedFileStream.ToArray(),
                FileName = $"{fileName}.zip"
            };
        }

        public static IEnumerable<Manager> GetManagerEmail(List<Manager> managers, int groupId)
        {
            return managers.Where(m => m.GroupId == groupId);
        }

        public static List<User> GetUserMissingDaysNoLog(IEnumerable<TimeLogReport> timeLogReports, int missingDays)
        {
            return GetUserxDaysNoLog(timeLogReports, missingDays);
        }

        //public static List<User> GetUser15DaysNoLog(IEnumerable<TimeLogReport> timeLogReports)
        //{
        //    return GetUserxDaysNoLog(timeLogReports, 15);
        //}

        public static List<User> GetUserNoLog(IEnumerable<User> regUsers, IEnumerable<TimeLogReport> timeLogReports)
        {
            var userNoLog = (from rgUser in regUsers
                              where timeLogReports.All(s => s.UserId != rgUser.Id)
                              select rgUser).ToList();

            return userNoLog;
        }

        public static List<NotificationEmail> CreatingEmployeeEmail(List<User> users)
        {
            var emails = (from usr in users
                          select new NotificationEmail
                          {
                              Email = usr.Email,
                              Subject = "Time Management Notification",
                              Body = "This is a friendly notification - You have over 10 days no time logs."
                          }).ToList();
            return emails;
        }

        public static List<NotificationEmail> CreatingManagerEmail(List<User> users, List<Manager> mangers)
        {
            List <NotificationEmail> managerEmails = new List<NotificationEmail>();

            foreach (var usr in users)
            {
                var mgrEmails = GetManagerEmail(mangers, usr.GroupId).Select(s => s.Email);

                var mgrMails = mgrEmails.ToList();
                if (mgrMails.Count() > 1)
                {
                    foreach (var mgrMail in mgrMails)
                    {
                        AddManageNotification(managerEmails, mgrMail, usr);
                    }
                }
                else if (mgrMails.Count() == 1)
                {
                    AddManageNotification(managerEmails, mgrMails.FirstOrDefault(), usr);
                }
            }

            return managerEmails;
        }

        public static NotificationEmail CreatingRobEmail(List<User> users, string? robEmail)
        {
            NotificationEmail notification = new NotificationEmail();

            foreach (var usr in users)
            {
                notification.Email = robEmail;
                notification.Subject = config.GetSection("EmailSettings:Subject").Value!;
                if (string.IsNullOrEmpty(notification.Body))
                {
                    notification.Body = "Following employee(s) has/have over 15 days no time logs." + "<br/><br/>" + usr.UserName;
                }
                else
                {
                    notification.Body += "<br/>" + usr.UserName;
                }
            }

            return notification;
        }

        private static void AddManageNotification(List<NotificationEmail> managerEmails, string? email, User employee)
        {
            if (managerEmails.Any(s => s.Email == email))
            {
                managerEmails.First(s => s.Email == email).Body += employee.UserName + "<br/>";
            }
            else
            {
                NotificationEmail notification = new NotificationEmail();
                notification.Email = email;
                notification.Subject = config.GetSection("EmailSettings:Subject").Value!;
                notification.Body = "Your group's employee(s) has/have over 15 days no time logs." + "<br/><br/>" + employee.UserName + "<br/>";
                managerEmails.Add(notification);
            }
        }

        private static List<User> GetUserxDaysNoLog(IEnumerable<TimeLogReport> timeLogReports, int nDays)
        {
            int minDays = 15 - nDays;
            var userswith15days = (from timelog in timeLogReports
                group timelog by new { timelog.UserId, timelog.UserName, timelog.GroupId, timelog.Email, timelog.TimeStamp.Date} into grp
                select new
                {
                    key = grp.Key.UserId, 
                    username = grp.Key.UserName,
                    groupid = grp.Key.GroupId,
                    email = grp.Key.Email,
                    date = grp.Key.Date,
                }).ToList();

            var userWithMissingLogNumber = userswith15days.GroupBy(n => 
                new {key = n.key, username= n.username, groupid = n.groupid, email = n.email}).Select(n => new 
            {
                Id = n.Key.key,
                UserName= n.Key.username,
                GroupId = n.Key.groupid,
                Email = n.Key.email,
                cnt = n.Count()
            }).OrderBy(n => n.Id).Where(n=> n.cnt <= minDays).Select( s=> new User(s.Id, s.GroupId, s.UserName, s.Email)).ToList();

            return userWithMissingLogNumber;
        }
    }
}
