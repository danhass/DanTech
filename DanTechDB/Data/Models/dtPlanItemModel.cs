﻿using DanTech.Data;
using AutoMapper;

namespace DanTech.Data.Models
{
#nullable enable
    public class dtPlanItemModel
    {
        public dtPlanItemModel(dtPlanItem pItem)
        {
            title = "";
            note = "";
            string start = pItem.day.ToShortDateString();
            string startTime = pItem.start.HasValue ? pItem.start.Value.ToString("HH:mm") : "";
            string end = "";
            string endTime = "";
            statusColor = "";
            if (pItem.duration.HasValue && pItem.start.HasValue)
            {
                var startDT = pItem.start!.Value;
                var durTS = pItem.duration.Value;
                end = startDT.AddHours(durTS.Hours).AddMinutes(durTS.Minutes).ToShortDateString();
                endTime = startDT.AddHours(durTS.Hours).AddMinutes(durTS.Minutes).ToString("HH:mm");
            }

            init(pItem.title,
                 pItem.note,
                 string.IsNullOrEmpty(start) ? null : start,
                 string.IsNullOrEmpty(startTime) ? null : startTime,
                 string.IsNullOrEmpty(end) ? null : end,
                 string.IsNullOrEmpty(endTime) ? null : endTime,
                 pItem.priority,
                 pItem.addToCalendar,
                 pItem.completed,
                 pItem.preserve,
                 pItem.user,
                 null,
                 pItem.project,
                 null,
                 false,
                 pItem.id,
                 pItem.recurrence,
                 pItem.recurrenceData,
                 pItem.parent,
                 pItem.fixedStart,
                 ""
                 );
        }

        public dtPlanItemModel(string pTitle,
                               string? pNote,
                               string? pStart,
                               string? pStartTime,
                               string? pEnd,
                               string? pEndTime,
                               int? pPriority,
                               bool? pAddToCalendar,
                               bool? pCompleted,
                               bool? pPreserve,
                               int pUser,
                               dtUserModel? pdtUser,
                               int? pProjectId,
                               dtProject? pdtProject,
                               bool pLoadUser = false,
                               int? pId = null,
                               int? pRecurrence = null,
                               string? pRecurrenceData = null,
                               int? pParent = null,
                               bool? pFixedStart = null,
                               string? pStatusColor = ""
            )
        {
            title = "";
            note = "";
            statusColor = "";
            init(pTitle, pNote, pStart, pStartTime, pEnd, pEndTime, pPriority, pAddToCalendar, pCompleted, pPreserve, pUser, pdtUser, pProjectId, pdtProject, pLoadUser, pId, pRecurrence, pRecurrenceData, pParent, pFixedStart, pStatusColor);
        }

        private void init(string pTitle,
                               string? pNote,
                               string? pStart,
                               string? pStartTime,
                               string? pEnd,
                               string? pEndTime,
                               int? pPriority,
                               bool? pAddToCalendar,
                               bool? pCompleted,
                               bool? pPreserve,
                               int pUser,
                               dtUserModel? pdtUser,
                               int? pProjectId,
                               dtProject? pdtProject,
                               bool pLoadUser = false,
                               int? pId = null,
                               int? pRecurrence = null,
                               string? pRecurrenceData = null,
                               int? pParent = null,
                               bool? pFixedStart = null,
                               string? pStatusColor = ""
                               )
        { 
            id = pId;
            title = pTitle;
            note = pNote ?? "";
            parent = pParent;
            day = DateTime.Parse(DateTime.Now.AddHours(DTDBConstants.TZOffset).ToShortDateString());
            if (pStart != null && !string.IsNullOrEmpty(pStart))
            {
                DateTime dt;
                if (DateTime.TryParse(pStart, out dt))
                {
                    day = dt;
                }
            }
            start = day;
            if (!string.IsNullOrEmpty(pStartTime))
            {
                TimeSpan ts;
                TimeSpan.TryParse(pStartTime, out ts);
                if (ts.Ticks > 0)
                {
                    start = start.Value.AddHours(ts.Hours);
                    start = start.Value.AddMinutes(ts.Minutes);
                }
            }
            var end = start ?? day;
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
            recurrence = pRecurrence;
            recurrenceData = pRecurrenceData;
            fixedStart = pFixedStart;
            statusColor = "";
            if (pStatusColor != null) statusColor = pStatusColor;
        }

        public dtPlanItemModel()
        {
            note = string.Empty;
            title = string.Empty;
            user = new dtUserModel();
            priority = 1000;
            statusColor = "";
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
        public string statusColor { get; set; }
#nullable enable
        public dtUserModel? user { get; set; }
        public dtProjectModel? project { get; set; }
#nullable disable
        public string projectMnemonic { get; set; } = "";
        public string projectTitle { get; set; } = "";
        public int? recurrence { get; set; }
        public int? parent { get; set; }
        public string recurrenceData { get; set; }
        public bool? fixedStart { get; set; }

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
                        .ForMember(dest => dest.recurrence, src => src.MapFrom(src => src.recurrence.HasValue ? src.recurrence : null))
                        .ForMember(dest => dest.projectId, src => src.MapFrom(src => src.project))
                        .ForMember(dest => dest.project, src => src.MapFrom(src => src.projectNavigation))
                        .ForMember(dest => dest.projectTitle, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.title))
                        .ForMember(dest => dest.projectMnemonic, src => src.MapFrom(src => src.projectNavigation == null ? "" : src.projectNavigation.shortCode))
                        .ForMember(dest => dest.priority, src => src.MapFrom(src => src.priority ?? 1000))
                        .ForMember(dest => dest.userId, src => src.MapFrom(src => src.user));
                });
            }
        }
    }
#nullable disable
}
