using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeMgtReportService.Interfaces;

namespace TimeMgtReportService.Models
{
    public class File
    {
        public byte[] Bytes { get; set; }
        public string FileName { get; set; }
    }
}
