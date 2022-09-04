using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;

namespace DanTech.Models.Data
{
    public class dtProjectModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string shortCode { get; set; }
        public string notes { get; set; }
        public int? priority { get; set; }
        public int? sortOrder { get; set; }
        public dtUserModel user { get; set; }
        public int colorCodeId { get; set; }
        public int status { get; set; }

        public static MapperConfiguration mapperConfiguration
        {
            get
            {
                return new MapperConfiguration(cfg =>
                   {
                       cfg.CreateMap<dtStatus, dtStatusModel>();
                       cfg.CreateMap<dtColorCode, dtColorCode>();
                       cfg.CreateMap<dtUser, dtUserModel>();
                       cfg.CreateMap<dtProject, dtProjectModel>()
                           .ForMember(dest => dest.colorCodeId, src => src.MapFrom(c => c.colorCode ?? 0))
                           .ForMember(dest => dest.user, src => src.MapFrom(src => src.userNavigation));
                   }
                );
            }
        }
    }
}
