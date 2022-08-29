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
        public dtPlanItemModel(string pTitle, string? pNote, string? pStart, string? pStartTime, string? pEnd, string? pEndTime, int? pPriority, bool? pAddToCalendar, bool? pCompleted, bool? pPreserve, int pUser, dtUserModel pdtUser, int? pProjectId, dtProject pdtProject, bool pLoadUser=false, int? pId = null)
        {
            id = pId;
            title = pTitle;
            note = pNote ?? "";
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
            if (!string.IsNullOrEmpty(pStartTime) && !string.IsNullOrEmpty(pEndTime) && start.HasValue && start.Value < end)
            {
                duration = end - start.Value;
            }
            priority = pPriority.HasValue ? pPriority : 1000;
            addToCalendar = pAddToCalendar;
            preserve = pPreserve;
            completed = pCompleted;
            projectMnemonic = "";
            projectTitle = "";
            userId= pUser;
            if (pLoadUser)
            {
                if (pdtUser != null && pdtUser.id > 0)
                {
                    userId = pdtUser.id;
                    user = pdtUser;
                }
                else
                {
                    user = new dtUserModel();
                }
            }
            projectId = pProjectId;
            if (pdtProject != null && pdtProject.id > 0)
            {
                projectMnemonic = pdtProject.shortCode;
                projectTitle = pdtProject.title;
                var cfg = dtProjectModel.mapperConfiguration;
                var mapper = new Mapper(cfg);
                project = mapper.Map<dtProjectModel>(pdtProject);
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
        //public TimeSpan? duration { get { return duration.HasValue ? duration.Value : new TimeSpan(0,0,0) ; } set { duration = value; } }
        public TimeSpan duration { get; set; }
        public int? priority { get; set; }
        public bool? addToCalendar { get; set; }
        public bool? completed { get; set; }
        public bool? preserve { get; set; }
        public int? userId { get; set; }
        public int? projectId { get; set; }
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
                        //.ForMember(dest => dest.duration, src => src.MapFrom(src=> src.duration.HasValue ? src.duration.Value : new TimeSpan(0,0,0)))
                        .ForMember(dest => dest.projectId, src => src.MapFrom(src => src.project))
                        .ForMember(dest => dest.project, src => src.MapFrom(src => src.projectNavigation))
                        .ForMember(dest => dest.projectTitle, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.title))
                        .ForMember(dest => dest.projectMnemonic, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.shortCode))
                        .ForMember(dest => dest.priority, src => src.MapFrom(src => src.priority.HasValue ? src.priority.Value : 1000))
                        .ForMember(dest => dest.userId, src => src.MapFrom(src => src.user));
                });
            }
        }
    }
#nullable disable
}
