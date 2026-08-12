using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Interfaces;

namespace TimeMgtReportService.Models
{
    public class User : ICsvReport
    {
        public User(int id, int groupId, string userName, string email)
        {
            Id = id;
            GroupId = groupId;
            UserName = userName;
            Email = email;
        }

        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
