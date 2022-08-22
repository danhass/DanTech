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
        public dtPlanItemModel(string pTitle, string pNote, string? pStart, string? pStartTime, string? pEnd, string? pEndTime, int? pPriority, bool? pAddToCalendar, bool? pCompleted, int pUser, dtUserModel? pdtUser, dtProject? pProject, int? pProjectId, string pProjectNemonic, string pProjectTitle, bool loadUser=false)
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
                }
            }
            day = day.AddHours(0 - day.Hour);
            day = day.AddMinutes(0 - day.Minute);
            day = day.AddSeconds(0 - day.Second);
            day = day.AddMilliseconds(0 - day.Millisecond);
            if (!string.IsNullOrEmpty(pStartTime))
            {
                TimeSpan ts;
                TimeSpan.TryParse(pStartTime, out ts);
                if (ts.Ticks > 0)
                {
                    start = day;
                    start = start.Value.AddHours(ts.Hours);
                    start = start.Value.AddMinutes(ts.Minutes);
                }
            }
            var end = start.HasValue ? start.Value : day;
            if (!string.IsNullOrEmpty(pEnd))
            {
                DateTime dt;
                if (DateTime.TryParse(pEnd, out dt))
                {
                    end = dt;
                }
            }
            end = end.AddMinutes(0 - end.Minute);
            end = end.AddHours(0 - end.Hour);
            end = end.AddMilliseconds(0 - end.Millisecond);
            end = end.AddSeconds(0 - end.Second);
            if (!string.IsNullOrEmpty(pEndTime))
            {
                TimeSpan ts;
                TimeSpan.TryParse(pEndTime, out ts);
                if (ts.Ticks > 0)
                {
                    end = end.AddHours(ts.Hours);
                    end = end.AddMinutes(ts.Minutes);
                }

            }
            if (!string.IsNullOrEmpty(pStartTime) && !string.IsNullOrEmpty(pEndTime))
            {
                duration = end - start;
            }
            priority = pPriority.HasValue ? pPriority : 1000;
            addToCalendar = pAddToCalendar;
            completed = pCompleted;
            projectMnemonic = pProjectNemonic;
            projectTitle = pProjectTitle;
            userId= pUser;
            if (loadUser)
            {
                if (pdtUser != null)
                {
                    userId = pdtUser.id;
                    user = pdtUser;
                }
                else
                {
                    user = new dtUserModel();
                }
            }
            if (pProject != null)
            {
                projectMnemonic = pProject.shortCode;
                projectTitle = pProject.title;
                var cfg = dtProjectModel.mapperConfiguration;
                var mapper = new Mapper(cfg);
                project = mapper.Map<dtProjectModel>(pProject);
            }
        }

        public dtPlanItemModel()
        {
            note = string.Empty;
            title = string.Empty;
            user = new dtUserModel();
            priority = 1000;
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
#nullable enable
        public dtUserModel? user { get; set; }

        public dtProjectModel? project { get; set; }
#nullable disable

        public string projectMnemonic { get; set; } = "";
        public string projectTitle { get; set; } = "";       
        
        public static MapperConfiguration mapperConfiguration
        {
            get
            {
                return new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<dtUser, dtUserModel>();
                    cfg.CreateMap<dtColorCode, dtColorCodeModel>();
                    cfg.CreateMap<dtStatus, dtStatusModel>();
                    cfg.CreateMap<dtProject, dtProjectModel>()
                        .ForMember(dest => dest.status, src => src.MapFrom(src => src.status))
                        .ForMember(dest => dest.colorCodeId, src => src.MapFrom(c => c.colorCode.HasValue ? c.colorCode : 0));
                    cfg.CreateMap<dtPlanItem, dtPlanItemModel>()
                        .ForMember(dest => dest.user, src => src.MapFrom(src => src.userNavigation))
                        .ForMember(dest => dest.project, src => src.MapFrom(src => src.projectNavigation))
                        .ForMember(dest => dest.projectTitle, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.title))
                        .ForMember(dest => dest.projectMnemonic, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.shortCode))
                        .ForMember(dest => dest.priority, src => src.MapFrom(src => src.priority.HasValue ? src.priority : 1000))
                        .ForMember(dest => dest.userId, src => src.MapFrom(src => src.user));
                });
            }
        }
    }
#nullable disable
}
