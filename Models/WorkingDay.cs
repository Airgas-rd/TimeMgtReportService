using System.ComponentModel.DataAnnotations;

namespace TimeMgtReportService.Models;
public enum WorkingDay
{
    [Display(Name = "Mon-Fri")]
    MTWHF = 1,
    [Display(Name = "Mon-Thr")]
    MTWT = 2,
    [Display(Name = "Tue-Fri")]
    TWTF = 3
}
