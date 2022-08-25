using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DanTech.Data;
using DanTech.Models.Data;
using System.IO;
using DanTech.Models;
using System.Net;

namespace DanTech.Services
{
    public class DTDBDataService
    {
        private static dgdb _db = null;
        private const string _testFlagKey = "Testing in progress";

        //Mappings
        private MapperConfiguration PlanItemMapConfig = dtPlanItemModel.mapperConfiguration;

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

        public static DTViewModel SetCredentials(string token)
        {
            var vm = new DTViewModel();
            return vm;
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
 
        public dtUserModel UserModelForSession(string session, string hostAddress)
        {
            dtUserModel mappedUser = null;
            if (!string.IsNullOrEmpty(session))
            {
                var sessionRecord = (from x in _db.dtSessions where x.session == session select x).FirstOrDefault();
                if (sessionRecord == null || sessionRecord.expires < DateTime.Now || sessionRecord.hostAddress != hostAddress)
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
                        var config = dtUserModel.mapperConfiguration;
                        var mapper = new Mapper(config);
                        mappedUser = mapper.Map<dtUserModel>(user);
                        sessionRecord.expires = DateTime.Now.AddDays(7);
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
            var config = dtProjectModel.mapperConfiguration;
            var mapper = new Mapper(config);            
            foreach (var p in ps)
            {
                projects.Add(mapper.Map<dtProjectModel>(p));
            }
            _db.SaveChanges();
            return projects;
        }

        public dtProject Set(dtProject project)
        {
            dtProject existing = null;
            if (project.id > 0)
            {
                existing = (from x in _db.dtProjects where x.id == project.id select x).FirstOrDefault();
            }

            if (existing == null)
            {
                existing = project;
                if (existing.colorCode.HasValue && (from x in _db.dtColorCodes where x.id == existing.colorCode.Value select x).FirstOrDefault() == null) existing.colorCode = null;
            }
            else
            {
                existing.notes = project.notes;
                existing.priority = project.priority;
                existing.shortCode = project.shortCode;
                existing.sortOrder = project.sortOrder;
                existing.status = project.status;
                existing.title = project.title;
                existing.user = project.user;
                if (project.colorCode.HasValue && (from x in _db.dtColorCodes where x.id == project.colorCode.Value select x).FirstOrDefault() != null) existing.colorCode = project.colorCode;
                else existing.colorCode = null;
            }
            if (existing.id < 1) _db.dtProjects.Add(existing);
            _db.SaveChanges();
            return existing;
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
            item.user = planItem.userId??0;
            item.project = planItem.projectId;
            item.preserve = planItem.preserve;
            if (item.id < 1) _db.dtPlanItems.Add(item);
            _db.SaveChanges();
            return item;
        }        

        public List<dtPlanItemModel> GetPlanItems(dtUser user, 
                                                    int daysBack = 1, 
                                                    bool includeCompleted = false, 
                                                    bool getAll = false, 
                                                    int onlyProject = 0)
        {
            if (_db == null) _db = new dgdb();
            if (user == null) return new List<dtPlanItemModel>();
            var mapper = new Mapper(PlanItemMapConfig);       
            var items = (from x in _db.dtPlanItems where x.user == user.id select x).OrderBy(x => x.day).ThenBy(x=> x.start).ToList();
            string result = "Items: " + items.Count + "; User: " + user.id;
            if (!getAll)
            {
                var minDate = DateTime.Parse(DateTime.Now.AddDays(1 - daysBack).ToShortDateString());
                items = items.Where(x => x.day >= minDate).ToList();
            }
            if (!includeCompleted) items = items.Where(x => (!x.completed.HasValue || !x.completed.Value)).ToList();
            if (onlyProject > 0) items = items.Where(x => (x.project.HasValue && x.project.Value == onlyProject)).ToList();
            var mapped = mapper.Map<List<dtPlanItemModel>>(items);
            return mapped;
        }

        public List<dtPlanItemModel> GetPlanItems(dtUserModel userModel, 
                                                  int daysBack = 1, 
                                                  bool includeCompleted = false, 
                                                  bool getAll = false, 
                                                  int onlyProject = 0)
        {
            if (userModel == null || userModel.id < 1) return new List<dtPlanItemModel>();
            return GetPlanItems(userModel.id, daysBack, includeCompleted, getAll);
        }

        public List<dtPlanItemModel> GetPlanItems(int userId, 
                                                  int daysBack = 1,
                                                  bool includeCompleted = false, 
                                                  bool getAll = false, 
                                                  int onlyProject = 0)
        {
            if (_db == null) _db = new dgdb();
            return GetPlanItems((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault(), daysBack, includeCompleted, getAll);
        }

        public List<dtStatusModel> GetStati()
        {
            if (_db == null) _db = new dgdb();
            var mappr = new Mapper( new MapperConfiguration(cfg => { cfg.CreateMap<dtStatus, dtStatusModel>(); }));
            return mappr.Map<List<dtStatusModel>>((from x in _db.dtStatuses select x).OrderBy(x => x.title).ToList());
        }

        public List<dtColorCodeModel> GetColorCodes()
        {
            if (_db == null) _db = new dgdb();
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtColorCode, dtColorCodeModel>(); }));
            return mappr.Map<List<dtColorCodeModel>>((from x in _db.dtColorCodes select x).OrderBy(x => x.title).ToList());
        }

        public static void GeneralUtil(dgdb db)
        {
            /*
            var url = "https://7822-54268.el-alt.com/Planner/Stati";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.AutomaticDecompression = DecompressionMethods.GZip;
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader rdr = new StreamReader(stream);
            string line = rdr.ReadToEnd();
            Console.WriteLine(line);
            */

            dtPlanItem itm = new dtPlanItem();
            itm.day = DateTime.Parse("8/30/2022");
            var ts = TimeSpan.Parse("13:05");
            itm.start = itm.day + ts;
            
            int ct = 2;
            ct++;
        }

        static void Modify (int number) { number++; }
        static void Modify (Person person) { person.Name = "Adjusted"; }
    }

    public class Person
    {
        public string Name { get; set; }
    }
}
