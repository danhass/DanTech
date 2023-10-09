using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DanTech.Data.Models;

namespace DanTech.Data.Models
{
    public class DTViewModel
    {
        [AllowNull]
        public string StatusMessage { get; set; }
        [AllowNull]
        public dtUserModel User { get; set; }
        public bool TestEnvironment { get; set; }
        public bool IsTesting { get; set; }

#nullable enable
        public List<dtPlanItemModel>? PlanItems { get; set; }
#nullable disable
    }
}
