using DanTech.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace DanTech.Models.Data
{
    public class dtPlanItemModel
    {
        public dtPlanItemModel(string pTitle, string pNote, string? pDay, string? pStart, string? pDuration, int? pPriority, bool? pAddToCalendar, bool? pCompleted, int pUser, dtUserModel? pdtUser, dtProject? pProject, int? pProjectId, string pProjectNemonic, string pProjectTitle)
        {
            title = pTitle;
            note = pNote;
            day = DateTime.Now;
            if (pDay != null && !string.IsNullOrEmpty(pDay))
            {
                DateTime dt;
                if (DateTime.TryParse(pDay, out dt)) day = dt;
            }
            if (pStart != null && !string.IsNullOrEmpty(pStart))
            {
                DateTime dt;
                if (DateTime.TryParse(pStart, out dt)) start = dt;
            }
            if (pDuration != null && !string.IsNullOrEmpty(pDuration))
            {
                TimeSpan ts;
                if (TimeSpan.TryParse(pDuration, out ts)) duration = ts;
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
}
