using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Models;

namespace TimeMgtReportService.Interfaces
{
    public interface IDatabaseService
    {
        IEnumerable<User> GetUsers();
        IEnumerable<Manager> GetManagers();
        IEnumerable<TimeLogReport> GetTimeLogsRpt(DateTime startDate, DateTime endDate);
        public List<TimeLogReport> GetTimeLogsRpt2(DateTime startDate, DateTime endDate);
    }
}
