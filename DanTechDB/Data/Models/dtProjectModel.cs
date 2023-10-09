using AutoMapper;
using DanTech.Data;
using System.Diagnostics.CodeAnalysis;

namespace DanTech.Data.Models
{
    public class dtProjectModel
    {
        public int id { get; set; }
        [AllowNull]
        public string title { get; set; }
        [AllowNull]
        public string shortCode { get; set; }
        [AllowNull]
        public string notes { get; set; }
        public int? priority { get; set; }
        public int? sortOrder { get; set; }
        [AllowNull]
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
