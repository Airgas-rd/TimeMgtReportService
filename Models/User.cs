using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Interfaces;

namespace TimeMgtReportService.Models
{
    using System.Runtime.InteropServices.JavaScript;

    public class User : ICsvReport
    {
        public User(int id, int groupId, string userName, string email)
        {
            this.Id = id;
            this.GroupId = groupId;
            this.UserName = userName;
            this.Email = email;
        }

        public User(int id, int groupId, string userName, string email, DateTime date, WorkingDay workDay, double hours)
        {
            this.Id = id;
            this.GroupId = groupId;
            this.UserName = userName;
            this.Email = email;
            this.Date = date;
            this.WorkDay = workDay;
            this.Hours = hours;
        }

        public User(int id, int groupId, string userName, string email, int count)
        {
            this.Id = id;
            this.GroupId = groupId;
            this.UserName = userName;
            this.Email = email;
            this.TotalCount = count;
        }

        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int TotalCount { get; set; }
        public DateTime Date { get; set; }
        public WorkingDay WorkDay { get; set; }
        public double Hours { get; set; }
    }
}
