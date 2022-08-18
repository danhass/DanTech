using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DanTech.Data;
    
namespace DanTech.Models.Data
{
    public class dtSessionModel
    {  
        public int id { get; set; }
        public int user { get; set; }
        public string session { get; set; }
        public string hostAddress { get; set; }
        public DateTime expires { get; set; }

        public static MapperConfiguration mapperConfiguration {
            get
            {
                return new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<dtSession, dtSessionModel>();
                    cfg.CreateMap<dtUser, dtUserModel>().
                        ForMember(dest => dest.session, act => act.MapFrom(src => src.dtSession));
                });
            } }
    }
}
