using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Interfaces;

namespace TimeMgtReportService.Models
{
    public class TimeLogReport : ICsvReport
    {
        public TimeLogReport(int id, string userName, int groupId, string group, DateTime timestamp, bool timeOff, double hours, bool workHome, string jobDetail, string? reason, string project, string task, string subTask, string email, int userId)
        {
            this.Id = id;
            this.UserName = userName;
            this.GroupId = groupId;
            this.Group = group;
            this.TimeStamp = timestamp;
            this.TimeOff = timeOff;
            this.Hours = hours;
            this.WorkHome = workHome;
            this.JobDetail = jobDetail;
            this.Reason = reason;
            this.Project = project;
            this.Task = task;
            this.SubTask = subTask;
            this.Email = email;
            this.UserId = userId;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public int GroupId { get; set; }
        public string Group { get; set; }
        [DisplayName("Date Time")]
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime TimeStamp { get; set; }
        public bool TimeOff { get; set; }
        public bool DayOff { get; set; }
        public double Hours { get; set; }
        public string JobDetail { get; set; }
        public bool WorkHome { get; set; }
        public string? Reason { get; set; }
        public string Project { get; set; }
        public string Task { get; set; }
        public string SubTask { get; set; }
        public string Email { get; set; }
        public int UserId { get; set; }
    }
}
