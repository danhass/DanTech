using System;
using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class dtRecurrenceModel
    {
        public int id { get; set; }
        [AllowNull]
        public string title { get; set; }
        [AllowNull]
        public string note { get; set; }
        [AllowNull]
        public string description { get; set; }
        public DateTime? effective { get; set; }
        public DateTime? stops { get; set; }
        public int? daysToPopulate { get; set; }
    }
}
