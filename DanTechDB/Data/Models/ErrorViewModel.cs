using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class ErrorViewModel
    {
        [AllowNull]
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
