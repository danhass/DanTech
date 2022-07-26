using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DanTech.Data;
using DanTech.Models.Data;

namespace DanTech.Services
{
    public class DTDBDataService
    {
        private dgdb _db = new dgdb();
        private const string _testFlagKey = "Testing in progress";

        public string TestFlagKey { get { return _testFlagKey; } }
        public bool InTesting { get { return (from x in _db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null; } }

        public static bool SetIfTesting(dgdb db, string key, string value)
        {
            var TestingFlag = (from x in db.dtTestData where x.title == "Testing in progress" select x).FirstOrDefault();
            if (TestingFlag == null || TestingFlag.value != "1") return false;

            var datum = (from x in db.dtTestData where x.title == key select x).FirstOrDefault();
            if (datum == null)
            {
                datum = new dtTestDatum() { title = key, value = value };
                db.dtTestData.Add(datum);
            }
            else
            {
                datum.value = value;
            }

            var miscKey = key + " - testing";
            var m = (from x in db.dtMiscs where x.title == miscKey select x).FirstOrDefault();
            if (m != null) m.value = value;
            db.SaveChanges();
            return true;
        }

        public DTDBDataService(dgdb db)
        {
            _db = db;
        }       

        public void ToggleTestFlag()
        {
            var testFlag = (from x in _db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault();
            if (testFlag == null)
            {
                testFlag = new dtTestDatum() { title = _testFlagKey, value = "1" };
                _db.dtTestData.Add(testFlag);
            }
            else
            {
                _db.dtTestData.Remove(testFlag);
            }

            _db.SaveChanges();
        }

        public void ClearTestData()
        {
            var testData = (from x in _db.dtTestData where 1 == 1 select x).ToList();
            _db.dtTestData.RemoveRange(testData);
            _db.SaveChanges();
        }

        //Returns true if testing flag is set after setting the value
        public bool SetIfTesting(string key, string value)
        {
            var TestingFlag = (from x in _db.dtTestData where x.title == "Testing in progress" select x).FirstOrDefault();
            if (TestingFlag == null || TestingFlag.value != "1") return false;

            var datum = (from x in _db.dtTestData where x.title == key select x).FirstOrDefault();
            if (datum == null)
            {
                datum = new dtTestDatum() { title = key, value = value };
                _db.dtTestData.Add(datum);
            }
            else
            {
                datum.value = value;
            }
            _db.SaveChanges();
            return true;
        }

        public dtUserModel UserModelForSession(string session, string ipAddress)
        {
            dtUserModel mappedUser = null;
            if (!string.IsNullOrEmpty(session))
            {
                var sessionRecord = (from x in _db.dtSessions where x.session == session select x).FirstOrDefault();
                if (sessionRecord == null || sessionRecord.expires < DateTime.Now || sessionRecord.hostAddress != ipAddress)
                {
                    if (sessionRecord != null)
                    {
                        _db.dtSessions.Remove(sessionRecord);
                    }
                }
                else
                {
                    var user = (from x in _db.dtUsers where x.id == sessionRecord.user select x).FirstOrDefault();
                    if (user == null)
                    {
                        _db.dtSessions.Remove(sessionRecord);
                    }
                    else
                    {
                        var config = new MapperConfiguration(cfg =>
                        {
                            cfg.CreateMap<dtSession, dtSessionModel>();
                            cfg.CreateMap<dtUser, dtUserModel>().
                                ForMember(dest => dest.session, act => act.MapFrom(src => src.dtSession));
                        });
                        var mapper = new Mapper(config);
                        mappedUser = mapper.Map<dtUserModel>(user);
                        sessionRecord.expires = DateTime.Now.AddDays(1);
                    }
                }
                _db.SaveChanges();
            }
            return mappedUser;

        }    
        public List<dtProjectModel> DTProjects(int userId)
        {
            List<dtProjectModel> projects = new List<dtProjectModel>();
            return projects;
        }

        public List<dtProjectModel> DTProjects(dtUser dtUser)
        {
            List<dtProjectModel> projects = new List<dtProjectModel>();
            var ps = (from x in _db.dtProjects where x.user == dtUser.id select x).ToList();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<dtUser, dtUserModel>();
                cfg.CreateMap<dtProject, dtProjectModel>().
                    ForMember(dest => dest.user, act => act.MapFrom(src => src));
            });
            var mapper = new Mapper(config);            
            foreach (var p in ps)
            {
                projects.Add(mapper.Map<dtProjectModel>(p));
            }
            return projects;
        }
    }
}
