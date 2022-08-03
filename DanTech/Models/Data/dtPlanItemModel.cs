using DanTech.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace DanTech.Models.Data
{
#nullable enable
    public class dtPlanItemModel
    {
        public dtPlanItemModel(string pTitle, string pNote, string? pStart, string? pEnd, int? pPriority, bool? pAddToCalendar, bool? pCompleted, int pUser, dtUserModel? pdtUser, dtProject? pProject, int? pProjectId, string pProjectNemonic, string pProjectTitle)
        {
            title = pTitle;
            note = pNote;
            day = DateTime.Now;
            if (pStart != null && !string.IsNullOrEmpty(pStart))
            {
                DateTime dt;
                if (DateTime.TryParse(pStart, out dt))
                {
                    day = dt;
                    start = dt;
                }
            }
            if (pEnd != null && !string.IsNullOrEmpty(pEnd))
            {
                DateTime dt;
                if (DateTime.TryParse(pEnd, out dt))
                {
                    duration = new TimeSpan(dt.Ticks - day.Ticks);
                }
            }
            priority = pPriority;
            addToCalendar = pAddToCalendar;
            completed = pCompleted;
            projectMnemonic = pProjectNemonic;
            projectTitle = pProjectTitle;
            userId= pUser;
            if (pdtUser != null)
            {
                userId = pdtUser.id;
                user = pdtUser;
            }
            else
            {
                user = new dtUserModel();
            }
            if (pProject != null)
            {
                projectMnemonic = pProject.shortCode;
                projectTitle = pProject.title;
                var cfg = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<dtProject, dtProjectModel>()
                    .ForMember(dest => dest.color, src => src.MapFrom(src => src.colorCodeNavigation));
                });
                var mapper = new Mapper(cfg);
                project = mapper.Map<dtProjectModel>(pProject);
            }
        }

        public dtPlanItemModel()
        {
            note = string.Empty;
            title = string.Empty;
            user = new dtUserModel();
        }


        public int? id { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public DateTime day { get; set; }
        public DateTime? start { get; set; }
        public TimeSpan? duration { get; set; }
        public int? priority { get; set; }
        public bool? addToCalendar { get; set; }
        public bool? completed { get; set; }
        public int? userId { get; set; }
        public dtUserModel user { get; set; }

#nullable enable
        public dtProjectModel? project { get; set; }
#nullable disable

        public string projectMnemonic { get; set; } = "";
        public string projectTitle { get; set; } = "";        
    }
#nullable disable
}
