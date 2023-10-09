using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class dtStatusModel
    {       
        public int id { get; set; }
        [AllowNull]
        public string title { get; set; }
        [AllowNull]
        public string note { get; set; }

        public int? colorCode { get; set; }
    }
}
