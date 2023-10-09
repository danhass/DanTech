using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtPlanItem
{
    public int id { get; set; }

    public int user { get; set; }

    public int? project { get; set; }

    public string title { get; set; }

    public string note { get; set; }

    public DateTime day { get; set; }

    public DateTime? start { get; set; }

    public TimeSpan? duration { get; set; }

    public int? priority { get; set; }

    public bool? addToCalendar { get; set; }

    public bool? completed { get; set; }

    public bool? preserve { get; set; }

    public int? recurrence { get; set; }

    public int? parent { get; set; }

    public string recurrenceData { get; set; }

    public bool? fixedStart { get; set; }

    public virtual ICollection<dtPlanItem> InverseparentNavigation { get; set; } = new List<dtPlanItem>();

    public virtual dtPlanItem parentNavigation { get; set; }

    public virtual dtProject projectNavigation { get; set; }

    public virtual dtRecurrence recurrenceNavigation { get; set; }

    public virtual dtUser userNavigation { get; set; }
}
