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
using System.Diagnostics;

namespace DanTech.Services
{
    public class DTDBDataService
    {
        private static dgdb _db = null;
        private static dtUser _currentUser = null;
        private static dtPlanItem _recurringItem = null;
        private const string _testFlagKey = "Testing in progress";

        public DTDBDataService(dgdb db)
        {
            _db = db;
        }

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

        private static List<dtPlanItem> PopulateRecurrences()
        {
            if (_db == null) throw new Exception("DB not set");
            List<dtPlanItem> items = new List<dtPlanItem>();
            if (_db == null) return items;
            if (_recurringItem == null) return items;

            var config = new MapperConfiguration(cfg => { cfg.CreateMap<dtPlanItem, dtPlanItem>(); });
            var mapper = new Mapper(config);
            var seed = mapper.Map<dtPlanItem>(_recurringItem);
            seed.id = 0;
            seed.recurrence = null;
            seed.recurrenceData = null;
            seed.parent = _recurringItem.id;

            // Set up the days in the next 30 days when the recurrence should be placed.
            List<bool> AddOnThisDay = new List<bool>() { false, false, false, false, false, false, false, false, false, false,
                                                         false, false, false, false, false, false, false, false, false, false,
                                                         false, false, false, false, false, false, false, false, false, false};
            if (string.IsNullOrEmpty(_recurringItem.recurrenceData) || _recurringItem.recurrenceData == "-------")
                AddOnThisDay = new List<bool>() { true, true, true, true, true, true, true, true, true, true,
                                                         true, true, true, true, true, true, true, true, true, true,
                                                         true, true, true, true, true, true, true, true, true, true};
            Dictionary<int, bool> dateMap = new Dictionary<int, bool>();
            if (_recurringItem.recurrence == (int)DtRecurrence.Monthly)
            {
                int iBuf = 0;
                foreach (var s in _recurringItem.recurrenceData.Split(','))
                {
                    iBuf = 0;
                    if (int.TryParse(s, out iBuf)) dateMap[iBuf] = true;
                }
            }
            int numWksInCycle = -1;
            string mask = _recurringItem.recurrenceData;
            if ((_recurringItem.recurrence == (int)DtRecurrence.Semi_monthly && _recurringItem.recurrenceData.Split(":").Length > 0) ||
                (_recurringItem.recurrence == (int)DtRecurrence.Monthly_nth_day && _recurringItem.recurrenceData.Split(":").Length > 0))
            {
                int.TryParse(_recurringItem.recurrenceData.Split(":")[0], out numWksInCycle);
                if (_recurringItem.recurrenceData.Split(":").Length > 1) mask = _recurringItem.recurrenceData.Split(":")[1];
            }
            if (!string.IsNullOrEmpty(_recurringItem.recurrenceData) && _recurringItem.recurrenceData.Split(":").Length > 1) mask = _recurringItem.recurrenceData.Split(":")[1];

            for (int i = 0; i < 30 && !string.IsNullOrEmpty(_recurringItem.recurrenceData); i++)
            {
                DateTime test = DateTime.Now.AddDays(i);
                if ((_recurringItem.recurrence == (int)DtRecurrence.Daily_Weekly &&
                     _recurringItem.recurrenceData.Length > (int)test.DayOfWeek &&
                     mask[(int)test.DayOfWeek] == '*') ||
                    (_recurringItem.recurrence == (int)DtRecurrence.Monthly &&
                     dateMap.ContainsKey(test.Day) &&
                     dateMap[test.Day]) ||
                    (_recurringItem.recurrence == (int)DtRecurrence.Semi_monthly &&
                        test >= _recurringItem.start.Value &&
                      (((int)(test.AddDays(1) - _recurringItem.start.Value).TotalDays / 7)) % numWksInCycle == 0 &&
                       mask[(int)test.DayOfWeek] == '*') ||
                    (_recurringItem.recurrence == (int)DtRecurrence.Monthly_nth_day &&
                        numWksInCycle > 0 &&
                        ((int)(test.Day/7) + 1) == numWksInCycle &&
                        mask[(int)test.DayOfWeek] == '*')
                   )
                    AddOnThisDay[i] = true;
            }

            for (int i = 0; i < 30; i++)
            {
                DateTime test = DateTime.Parse(DateTime.Now.ToShortDateString()).AddDays(i);
                if (AddOnThisDay[i])
                {
                    if ((from x in _db.dtPlanItems where x.recurrence == null && x.day == test && x.parent == _recurringItem.id select x).FirstOrDefault() == null)
                    {
                        test = test.AddHours(seed.start.Value.Hour);
                        test = test.AddMinutes(seed.start.Value.Minute);
                        seed.day = DateTime.Parse(test.ToShortDateString());
                        seed.start = test;
                        items.Add(mapper.Map<dtPlanItem>(seed));
                    }
                }
            }
            return items;
        }

        public static DTViewModel SetCredentials(string token)
        {
            var vm = new DTViewModel();
            return vm;
        }

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

        public List<dtColorCodeModel> ColorCodes()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtColorCode, dtColorCodeModel>(); }));
            return mappr.Map<List<dtColorCodeModel>>((from x in _db.dtColorCodes select x).OrderBy(x => x.title).ToList());
        }

        public bool DeletePlanItem(int planItemId, int userId)
        {
            if (_db == null) throw new Exception("DB not set");
            var item = (from x in _db.dtPlanItems where x.id == planItemId && x.user == userId select x).FirstOrDefault();
            if (item == null) return false;
            if (item.recurrence.HasValue)
            {
                var children = (from x in _db.dtPlanItems where x.parent.Value == item.id select x).ToList();
                foreach (var c in children)
                {
                    c.parent = null;
                }
                _db.SaveChanges();
            }
            _db.dtPlanItems.Remove(item);
            _db.SaveChanges();
            return true;
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

        public bool InTesting { get { return (from x in _db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null; } }

        public List<dtPlanItemModel> PlanItems(dtUser user,
                                                    int daysBack = 1,
                                                    bool includeCompleted = false,
                                                    bool getAll = false,
                                                    int onlyProject = 0,
                                                    bool onlyRecurrences = false)
        {
            if (_db == null) throw new Exception("DB not set");
            if (user == null) return new List<dtPlanItemModel>();
            var mapper = new Mapper(PlanItemMapConfig);
            var dateToday = DateTime.Parse(DateTime.Now.ToShortDateString());

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
                // If we are not getting all items, we want the items that have a day that is current or later,
                // and items from the past days that were not completed
                items = items.Where(x => x.day >= minDate || (x.completed == null && x.recurrence == null)).ToList();
            }
            if (!includeCompleted) items = items.Where(x => (!x.completed.HasValue || !x.completed.Value)).ToList();
            if (onlyProject > 0) items = items.Where(x => (x.project.HasValue && x.project.Value == onlyProject)).ToList();
            if (onlyRecurrences) items = items.Where(x => (x.recurrence.HasValue && x.recurrence.Value > 0)).ToList();
            var results = new List<dtPlanItemModel>();
            foreach (var i in items) results.Add(new dtPlanItemModel(i));
            ThreadStart updateRecurrencesRef = new ThreadStart(UpdateRecurrances);
            Thread updateRecurrences = new Thread(updateRecurrencesRef);
            updateRecurrences.Start();
            return results;
        }

        // We want this to run in its own thread and the app likely has already returned the controller method.
        // We use the static to avoid problems with the db context leaving scope.
        public List<dtPlanItemModel> PlanItems(int userId,
                                                  int daysBack = 1,
                                                  bool includeCompleted = false,
                                                  bool getAll = false,
                                                  int onlyProject = 0,
                                                  bool onlyRecurrences = false
                                                  )
        {
            if (_db == null) throw new Exception("DB not set");
            return PlanItems((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault(), daysBack, includeCompleted, getAll, onlyProject, onlyRecurrences);
        }

        public bool Propagate(int itemId, int userId)
        {
            if (_db == null) throw new Exception("DB not set");
            bool result = true;
            var item = (from x in _db.dtPlanItems where x.id == itemId && x.user == userId select x).FirstOrDefault();
            if (item == null || (!item.recurrence.HasValue && !item.parent.HasValue)) return false;
            dtPlanItem parent = null;
            if (item.parent.HasValue)
            {
                parent = (from x in _db.dtPlanItems where x.id == item.parent.Value select x).FirstOrDefault();
                if (parent == null) return false;
                parent.note = item.note;
                parent.preserve = item.preserve;
                parent.priority = item.priority;
                parent.project = item.project;
                parent.title = item.title;
                parent.start = parent.day;
                parent.start = parent.start.Value.AddHours(item.start.Value.Hour);
                parent.start = parent.start.Value.AddMinutes(item.start.Value.Minute);
                parent.duration = item.duration;
            }
            else
            {
                parent = item;
            }
            if (!parent.recurrence.HasValue) return false;
            var children = (from x in _db.dtPlanItems where x.parent.Value == parent.id select x).ToList();
            foreach (var c in children)
            {
                c.note = parent.note;
                c.preserve = parent.preserve;
                c.priority = parent.priority;
                c.project = parent.project;
                c.title = parent.title;
                c.start = c.day;
                c.start = c.start.Value.AddHours(parent.start.Value.Hour);
                c.start = c.start.Value.AddMinutes(parent.start.Value.Minute);
                c.duration = parent.duration;
            }
            _db.SaveChanges();
            return result;
        }
        
        public List<dtRecurrenceModel> Recurrences()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtRecurrence, dtRecurrenceModel>(); }));
            return mappr.Map<List<dtRecurrenceModel>>(from x in _db.dtRecurrences select x).ToList();
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

        public List<dtStatusModel> Stati()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtStatus, dtStatusModel>(); }));
            return mappr.Map<List<dtStatusModel>>((from x in _db.dtStatuses select x).OrderBy(x => x.title).ToList());
        }

        public string TestFlagKey { get { return _testFlagKey; } }

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
        
        private void UpdateRecurrances()
        {
            if (_currentUser == null || _db == null) return;
            var user = _currentUser;
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
        }

        // This is left at the bottom because it is a special method that is not part of normal use.
        public static void GeneralUtil(dgdb db)
        {
            var targets = (from x in db.dtPlanItems where x.parent != null select x).ToList();
            dtMisc log = new dtMisc() { title = "Util log", value = "There are " + targets.Count + " items." };
            db.dtMiscs.Add(log);
            foreach (var t in targets)
            {
                if (t.start.Value.ToLongDateString() != t.day.ToLongDateString())
                {
                    var buf = DateTime.Parse(t.day.ToShortDateString()).AddHours(t.start.Value.Hour).AddMinutes(t.start.Value.Minute);
                    dtMisc log1 = new dtMisc() { title = t.id.ToString() + " values", value = buf.ToLongDateString() + " " + buf.ToLongTimeString() + " \n " + 
                        t.start.Value.ToLongDateString() + " " + t.start.Value.ToLongTimeString() + " \n " + 
                        t.day.ToLongDateString() + " " + t.day.ToLongTimeString() };
                    t.start = buf;
                    db.dtMiscs.Add(log1);
                }
            }
            db.SaveChanges();
         }

    }

}
