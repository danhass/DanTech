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
using System.Threading;

namespace DanTech.Services
{
    public class DTDBDataService
    {
        private static dgdb _db = null;
        private static dtPlanItem _recurringItem = null;
        private const string _testFlagKey = "Testing in progress";

        //Mappings
        private MapperConfiguration PlanItemMapConfig = dtPlanItemModel.mapperConfiguration;

        public static void ClearResetFlags()
        {
            if (_db == null) throw new Exception("DB not set");
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
            if (_db == null) throw new Exception("DB not set");
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
        }

        public void ToggleTestFlag()
        {
            if (_db == null) throw new Exception("DB not set");
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

        public bool DeletePlanItem(int planItemId, int userId)
        {
            if (_db == null) throw new Exception("DB not set");
            var item = (from x in _db.dtPlanItems where x.id == planItemId && x.user == userId select x).FirstOrDefault();
            if (item == null) return false;
            _db.dtPlanItems.Remove(item);
            _db.SaveChanges();
            return true;
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
            if (_db == null) throw new Exception("DB not set");
            List<dtProjectModel> projects = new List<dtProjectModel>();
            return DTProjects((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault());
        }
        public List<dtProjectModel> DTProjects(dtUser u)
        {
            if (_db == null) throw new Exception("DB not set");
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
            if (_db == null) throw new Exception("DB not set");
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
            if (_db == null) throw new Exception("DB not set");
            var mapper = new Mapper(PlanItemMapConfig);
            var item = new dtPlanItemModel(planItem);
            return Set(item);
        }
        public dtPlanItem Set(dtPlanItemModel planItem)
        {
            if (_db == null) throw new Exception("DB not set");
            if (planItem == null) return null;

            if (!planItem.userId.HasValue) throw new Exception("Setting plan item requires a user id.");
            //Assume the elements of the model are valid.
            // For example, assume that the user is a valid user
            dtPlanItem item = null;
            if (planItem.id != null && planItem.id.Value > 0)
            {
                item = (from x in _db.dtPlanItems where x.id == planItem.id select x).FirstOrDefault();
                if (item != null && (planItem.userId == null || planItem.userId.Value != item.user)) throw new Exception("Trying to set plan item of different user.");
            }
            if (item == null) item = new dtPlanItem();
            item.addToCalendar = planItem.addToCalendar;
            item.completed = planItem.completed;
            if (!(item.completed.HasValue && item.completed.Value)) item.completed = null;
            item.day = planItem.day;
            item.duration = planItem.duration;
            item.note = planItem.note;
            item.priority = planItem.priority;
            item.start = planItem.start;
            item.title = planItem.title;
            item.user = planItem.userId.Value;
            item.project = planItem.projectId;
            item.preserve = planItem.preserve;
            item.recurrence = planItem.recurrence;
            item.recurrenceData = planItem.recurrenceData;
            if (item.id < 1) _db.dtPlanItems.Add(item);
            try
            {
                _db.SaveChanges();
            }
            catch (Exception e)
            {
                string eType = e.GetType().FullName;
                Console.WriteLine(eType);
            }
            if (item.recurrence.HasValue) 
            {
                _recurringItem = item;
                var rItems = PopulateRecurrences();
                if (rItems.Count > 0)
                {
                    _db.dtPlanItems.AddRange(rItems);
                    try
                    {
                        foreach (var i in rItems) i.recurrence = null;
                        _db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        string eType = e.GetType().FullName;
                        Console.WriteLine(eType);
                    }
                }
            }

            return item;
        }

        private static List<dtPlanItem> PopulateRecurrences()
        {
            if (_db == null) throw new Exception("DB not set");
            List<dtPlanItem> items = new List<dtPlanItem>();
            if (_db == null) return items;
            if (_recurringItem == null) return items;
            List<bool> AddOnThisDay = new List<bool>() { true, true, true, true, true, true, true };

            if (!string.IsNullOrEmpty(_recurringItem.recurrenceData))
            {
                for (int i = 0; i < _recurringItem.recurrenceData.Length; i++) if (_recurringItem.recurrenceData[i] != '*') AddOnThisDay[i] = false;
            }
            var config = new MapperConfiguration(cfg => { cfg.CreateMap<dtPlanItem, dtPlanItem>(); });
            var mapper = new Mapper(config);
            var seed = mapper.Map<dtPlanItem>(_recurringItem);
            seed.recurrence = null;
            seed.recurrenceData = "";
            seed.parent = _recurringItem.id;
            seed.id = 0;
            seed.day = DateTime.Parse(DateTime.Now.ToShortDateString());
            
            for (int i=0; i<30; i++)
            {
                if (AddOnThisDay[(int)seed.day.DayOfWeek])
                {
                    if ((from x in _db.dtPlanItems where x.recurrence == null && x.day == seed.day && x.parent == seed.parent select x).FirstOrDefault() == null)
                    {
                        items.Add(mapper.Map<dtPlanItem>(seed));
                    }
                }
                seed.day = seed.day.AddDays(1);
                if (seed.start.HasValue)
                {
                    var date = seed.start.Value.AddDays(1);
                    seed.start = date;
                }
                seed.id = 0;
            }
            return items;
        }

        public List<dtPlanItemModel> PlanItems(dtUser user,
                                                    int daysBack = 1,
                                                    bool includeCompleted = false,
                                                    bool getAll = false,
                                                    int onlyProject = 0)
        {
            if (_db == null) throw new Exception("DB not set");
            if (user == null) return new List<dtPlanItemModel>();
            var mapper = new Mapper(PlanItemMapConfig);
            var dateToday = DateTime.Parse(DateTime.Now.ToShortDateString());

            // Need to process recurrences
            var recurrances = (from x in _db.dtPlanItems where x.user == user.id && x.recurrence != null select x).ToList();
            List<dtPlanItem> recurranceItems = new List<dtPlanItem>();
            foreach (var r in recurrances)
            {
                _recurringItem = r;
                recurranceItems.AddRange(PopulateRecurrences());
            }
            if (recurranceItems.Count > 0)
            {
                _db.dtPlanItems.AddRange(recurranceItems);
                _db.SaveChanges();

            }

            //Use the data retrieval as the chance to clean up items that need to be removed
            var itemsToRemove = (from x in _db.dtPlanItems
                                 where x.user == user.id
                                    && x.preserve != true
                                    && x.completed.HasValue
                                    && x.completed.Value == true
                                    && x.day < dateToday
                                    && x.recurrence == null
                                 select x);
            _db.dtPlanItems.RemoveRange(itemsToRemove);
            _db.SaveChanges();
            var items = (from x in _db.dtPlanItems where x.user == user.id select x)
                .OrderBy(x => x.day)
                .ThenBy(x => x.completed)
                .ThenBy(x => x.start)
                .ToList();
            string result = "Items: " + items.Count + "; User: " + user.id;
            if (!getAll)
            {
                var minDate = DateTime.Parse(DateTime.Now.AddDays(1 - daysBack).ToShortDateString());
                items = items.Where(x => x.day >= minDate).ToList();
            }
            if (!includeCompleted) items = items.Where(x => (!x.completed.HasValue || !x.completed.Value)).ToList();
            if (onlyProject > 0) items = items.Where(x => (x.project.HasValue && x.project.Value == onlyProject)).ToList();
            var results = new List<dtPlanItemModel>();
            foreach (var i in items) results.Add(new dtPlanItemModel(i));
            return results;
        }

        /*
        public List<dtPlanItemModel> PlanItems(dtUserModel userModel,
                                                  int daysBack = 1,
                                                  bool includeCompleted = false,
                                                  bool getAll = false,
                                                  int onlyProject = 0)
        {
            if (userModel == null || userModel.id < 1) return new List<dtPlanItemModel>();
            return PlanItems(userModel.id, daysBack, includeCompleted, getAll);
        }
        */

        public List<dtPlanItemModel> PlanItems(int userId,
                                                  int daysBack = 1,
                                                  bool includeCompleted = false,
                                                  bool getAll = false,
                                                  int onlyProject = 0)
        {
            if (_db == null) throw new Exception("DB not set");
            return PlanItems((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault(), daysBack, includeCompleted, getAll);
        }

        public List<dtStatusModel> Stati()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtStatus, dtStatusModel>(); }));
            return mappr.Map<List<dtStatusModel>>((from x in _db.dtStatuses select x).OrderBy(x => x.title).ToList());
        }

        public List<dtRecurrenceModel> Recurrences()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtRecurrence, dtRecurrenceModel>(); }));
            return mappr.Map<List<dtRecurrenceModel>>(from x in _db.dtRecurrences select x).ToList();
        }

        public List<dtColorCodeModel> ColorCodes()
        {
            if (_db == null) throw new Exception("DB not set");
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

    }

}
