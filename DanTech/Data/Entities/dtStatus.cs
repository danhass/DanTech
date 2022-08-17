﻿using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtStatus
    {
        public dtStatus()
        {
            dtProjects = new HashSet<dtProject>();
        }

        public int id { get; set; }
        public string title { get; set; }
        public string note { get; set; }

        public virtual ICollection<dtProject> dtProjects { get; set; }
    }
}