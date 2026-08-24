using System.Data;
using Microsoft.Data.SqlClient;
using TimeMgtReportService.Interfaces;
using TimeMgtReportService.Models;

namespace TimeMgtReportService
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string? connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            this.connectionString = configuration.GetConnectionString("dbConnection");
        }

        public IEnumerable<User> GetUsers()
        {
            using (SqlConnection sql = new SqlConnection(this.connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_getusers", sql);

                cmd.CommandType = CommandType.StoredProcedure;

                sql.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetFieldValue<int>(0);
                        int groupId = reader.GetFieldValue<int>(1);
                        string userName = reader.GetFieldValue<string>(2);
                        string email = reader.GetFieldValue<string>(3);

                        User user = new User(id, groupId, userName, email);
                        yield return user;
                    }
                }
            }
        }

        public IEnumerable<Manager> GetManagers()
        {
            using (SqlConnection sql = new SqlConnection(this.connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_getmanagers", sql);

                cmd.CommandType = CommandType.StoredProcedure;

                sql.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetFieldValue<int>(0);
                        int groupId = reader.GetFieldValue<int>(1);
                        string userName = reader.GetFieldValue<string>(2);
                        string email = reader.GetFieldValue<string>(3);
                        int roleId = reader.GetFieldValue<int>(4);
                        string title = reader.GetFieldValue<string>(5);

                        Manager manager = new Manager(id, groupId, userName, email, roleId, title);
                        yield return manager;
                    }
                }
            }
        }

        public List<TimeLogReport> GetTimeLogsRpt2(DateTime startDate, DateTime endDate)
        {
            using (SqlConnection sql = new SqlConnection(this.connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_getworklogs", sql);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("startdate", startDate);
                cmd.Parameters.AddWithValue("enddate", endDate);

                List<TimeLogReport> testing = new List<TimeLogReport>();

                sql.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            int id = reader.GetFieldValue<int>(0);
                            string user = reader.GetFieldValue<string>(1);
                            int groupId = reader.GetFieldValue<int>(2);
                            string group = reader.GetFieldValue<string>(3);
                            DateTime dateTime = reader.GetFieldValue<DateTime>(4);
                            bool timeOff = reader.GetFieldValue<bool>(5);
                            double hours = (double)reader.GetFieldValue<decimal>(7);
                            string jobDetail = reader.GetFieldValue<string>(8);
                            bool workHome = reader.GetFieldValue<bool>(9);
                            string? reason = reader.IsDBNull(10) ? reader.GetFieldValue<string>(10) : null;
                            string project = reader.GetFieldValue<string>(11);
                            string task = reader.GetFieldValue<string>(12);
                            string subtask = reader.GetFieldValue<string>(13);
                            string email = reader.GetFieldValue<string>(14);
                            int userId = reader.GetFieldValue<int>(15);
                            WorkingDay workHour = (WorkingDay)reader.GetFieldValue<int>(16);
                            TimeLogReport timeLogsRpt = new TimeLogReport(id, user, groupId, group, dateTime, timeOff, hours, workHome, jobDetail, reason, project, task, subtask, email, userId, workHour);
                            testing.Add(timeLogsRpt);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
                return testing;
            }
        }

        public IEnumerable<TimeLogReport> GetTimeLogsRpt(DateTime startDate, DateTime endDate)
        {
            using (SqlConnection sql = new SqlConnection(this.connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_getworklogs", sql);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("startdate", startDate);
                cmd.Parameters.AddWithValue("enddate", endDate);

                sql.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetFieldValue<int>(0);
                        string user = reader.GetFieldValue<string>(1);
                        int groupId = reader.GetFieldValue<int>(2);
                        string group = reader.GetFieldValue<string>(3);
                        DateTime dateTime = reader.GetFieldValue<DateTime>(4);
                        bool timeOff = reader.GetFieldValue<bool>(5);
                        double hours = (double)reader.GetFieldValue<decimal>(7);
                        string jobDetail = reader.GetFieldValue<string>(8);
                        bool workHome = reader.GetFieldValue<bool>(9);
                        string? reason = !reader.IsDBNull(10) ? reader.GetFieldValue<string>(10) : null;
                        string project = reader.GetFieldValue<string>(11);
                        string task = reader.GetFieldValue<string>(12);
                        string subtask = reader.GetFieldValue<string>(13);
                        string email = reader.GetFieldValue<string>(14);
                        int userId = reader.GetFieldValue<int>(15);
                        WorkingDay workHour = (WorkingDay)reader.GetFieldValue<int>(16);
                        TimeLogReport timeLogsRpt = new TimeLogReport(id, user, groupId, group, dateTime, timeOff, hours, workHome, jobDetail, reason, project, task, subtask, email, userId, workHour);
                        yield return timeLogsRpt;
                    }
                }
            }
        }
    }
}
