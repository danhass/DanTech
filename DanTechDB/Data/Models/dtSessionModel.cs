using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using DanTech.Data;

namespace DanTech.Data.Models
{
    public class dtSessionModel
    {  
        public int id { get; set; }
        public int user { get; set; }
        [AllowNull]
        public string session { get; set; }
        [AllowNull]
        public string hostAddress { get; set; }
        public DateTime expires { get; set; }

        public static MapperConfiguration mapperConfiguration {
            get
            {
                return new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<dtSession, dtSessionModel>();
                });
            } }
    }
}
