using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtColorCode
    {
        public dtColorCode()
        {
            dtProjects = new HashSet<dtProject>();
            dtStatuses = new HashSet<dtStatus>();
        }

        public int id { get; set; }
        public string title { get; set; }
        public string note { get; set; }

        public virtual ICollection<dtProject> dtProjects { get; set; }
        public virtual ICollection<dtStatus> dtStatuses { get; set; }
    }
}
