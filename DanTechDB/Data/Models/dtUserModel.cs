using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using DanTech.Data;

namespace DanTech.Data.Models
{
    public class dtUserModel
    { 

        public int id { get; set; }
        [AllowNull]
        public string fName { get; set; }
        [AllowNull]
        public string lName { get; set; }
        [AllowNull]
        public string otherName { get; set; }
        [AllowNull]
        public string email { get; set; }
        [AllowNull]
        public string token { get; set; }
        [AllowNull]
        public string refreshToken { get; set; }
        public DateTime? lastLogin { get; set; }
        public byte? suspended { get; set; }

        public byte? doNotSetPW { get; set; }
  
        public static MapperConfiguration mapperConfiguration
        {
            get
            {
                return new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<dtSession, dtSessionModel>();
                    cfg.CreateMap<dtUser, dtUserModel>();
                });
            }
        }
    }
}
