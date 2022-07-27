using System;
using System.Collections;
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
        private static dgdb _db = null;
        private const string _testFlagKey = "Testing in progress";

        //Mappings
        private MapperConfiguration PlanItemMapConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<dtUser, dtUserModel>();
            cfg.CreateMap<dtProject, dtProjectModel>();
            cfg.CreateMap<dtPlanItem, dtPlanItemModel>()
                .ForMember(dest => dest.user, src => src.MapFrom(src => src.userNavigation))
                .ForMember(dest => dest.project, src => src.MapFrom(src => src.projectNavigation));
        });

        public static void ClearResetFlags()
        {
            if (_db == null) _db = new dgdb();
            var element = (from x in _db.dtMiscs where x.title == DTConstants.AuthTokensNeedToBeResetKey select x).FirstOrDefault();
            if (element != null)
            {
                _db.dtMiscs.Remove(element);
                _db.SaveChanges();
            }
        }

        public string TestFlagKey { get { return _testFlagKey; } }
        public bool InTesting { get { return (from x in _db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null; } }

        public static bool SetIfTesting(string key, string value)
        {
            if (_db == null) _db = new dgdb();
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

            var miscKey = key + " - testing";
            var m = (from x in _db.dtMiscs where x.title == miscKey select x).FirstOrDefault();
            if (m != null) m.value = value;
            _db.SaveChanges();
            return true;
        }

        public DTDBDataService(dgdb db)
        {
            _db = db;
            if (_db == null) _db = new dgdb();
        }       

        public void ToggleTestFlag()
        {
            if (_db == null) _db = new dgdb();
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

        public static void ClearTestData()
        {
            if (_db == null) _db = new dgdb();
            var testData = (from x in _db.dtTestData where x.title != DTConstants.AuthTokensNeedToBeResetKey select x).ToList();
            _db.dtTestData.RemoveRange(testData);
            _db.SaveChanges();
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
            if (_db == null) _db = new dgdb();
            List<dtProjectModel> projects = new List<dtProjectModel>();
            return DTProjects((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault());
        }

        public List<dtProjectModel> DTProjects(dtUser u)
        {
            if (_db == null) _db = new dgdb();
            List<dtProjectModel> projects = new List<dtProjectModel>();
            if (u == null) return projects;
            var ps = (from x in _db.dtProjects where x.user == u.id select x).ToList();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<dtUser, dtUserModel>();
                cfg.CreateMap<dtProject, dtProjectModel>().
                    ForMember(dest => dest.user, act => act.MapFrom(src => src.userNavigation));
            });
            var mapper = new Mapper(config);            
            foreach (var p in ps)
            {
                projects.Add(mapper.Map<dtProjectModel>(p));
            }
            return projects;
        }

        public dtPlanItem Set(dtPlanItem planItem)
        {       
            var mapper = new Mapper(PlanItemMapConfig);
            var item = mapper.Map<dtPlanItemModel>(planItem);
            return Set(item);
        }
        public dtPlanItem Set(dtPlanItemModel planItem)
        {
            if (_db == null) _db = new dgdb();
            if (planItem == null) return null;

            //Assume the elements of the model are valid.
            // For example, assume that the user is a valid user
            dtPlanItem item = null;
            if (planItem.id != null && planItem.id > 0) item = (from x in _db.dtPlanItems where x.id == planItem.id select x).FirstOrDefault();
            if (item == null)  item = new dtPlanItem();
            item.addToCalendar = planItem.addToCalendar;
            item.completed = planItem.completed;
            item.day = planItem.day;
            item.duration = planItem.duration;
            item.note = planItem.note;
            item.priority = planItem.priority;
            item.start = planItem.start;
            item.title = planItem.title;
            item.user = planItem.user.id;
            item.project = planItem.project?.id;
            if (item.id < 1) _db.dtPlanItems.Add(item);
            _db.SaveChanges();
            return item;
        }        

        public List<dtPlanItemModel> Get(dtUser user)
        {
            if (_db == null) _db = new dgdb();
            if (user == null) return new List<dtPlanItemModel>();
            var mapper = new Mapper(PlanItemMapConfig);
            return mapper.Map<List<dtPlanItemModel>>((from x in _db.dtPlanItems where x.user == user.id select x).OrderBy(x => x.day).ToList());
        }

        public List<dtPlanItemModel> Get(dtUserModel userModel)
        {
            return Get(userModel.id);
        }

        public List<dtPlanItemModel> Get(int userId)
        {
            if (_db == null) _db = new dgdb();
            return Get((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault());
        }
    }
}
