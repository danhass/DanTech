using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class dtTestingModel
    {
        [AllowNull]
        public string title { get; set; }
        [AllowNull]
        public string note { get; set; }
    }
}
