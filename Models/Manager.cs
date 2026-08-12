using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeMgtReportService.Models
{
    public class Manager : User
    {
        public Manager(int id, int groupId, string userName, string email, int roleId, string title) 
            : base(id,  groupId,  userName,  email)
        {
            RoleId = roleId;
            Title = title;
        }

        public int RoleId { get; set; }
        public string Title { get; set; }
    }
}
