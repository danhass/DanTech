using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class dtLogin
    {
        [AllowNull]
        public string Session { get; set; }
        [AllowNull]
        public string Email { get; set; }
        [AllowNull]
        public string FName { get; set; }
        [AllowNull]
        public string LName { get; set; }
        [AllowNull]
        public string Message { get; set; }
    }
}
