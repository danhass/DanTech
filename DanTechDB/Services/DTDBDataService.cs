using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DanTech.Data;
using DanTech.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DanTech.Services;
using System.Diagnostics.CodeAnalysis;
using ZstdSharp.Unsafe;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DanTech.Services
{
#pragma warning disable 8629, 8600, 8603
    public class DTDBDataService : IDTDBDataService
    {
        [AllowNull]
        private Idtdb _dbi = null;
        [AllowNull]
        private dtdb _db = null;
        [AllowNull]
        private static dtUser _currentUser = null;
        private static int _userId = -1;
        [AllowNull]
        private static dtPlanItem _recurringItem = null;
        private static string _conn = string.Empty;
        private const string _testFlagKey = "Testing in progress";

        public DTDBDataService(IConfiguration cfg, dtdb dbctx)
        {
            _conn = cfg.GetConnectionString("DG").ToString();
            _dbi = dbctx;
            _db = dbctx;
            if (!DTDBConstants.Initialized()) DTDBConstants.Init(_db!);
        }

        public DTDBDataService(string conn)
        {
            _conn = conn;
            _dbi = InstantiateDB();
            _db = (_dbi as dtdb);
            if (!DTDBConstants.Initialized()) DTDBConstants.Init(_db!);
        }

        public bool PendingChanges()
        {
            try
            {
                return (_db.ChangeTracker.Entries().ToList().Count > 0);
            } catch (Exception) 
            {
                return false; 
            }
        }
        private Idtdb InstantiateDB()
        {
            if (string.IsNullOrEmpty(_conn)) throw new Exception("Must set connection string to instantiate the data service");
            if (_db == null)
            {
                _db = new dtdb(_conn);
            }
            return _db;
        }

        public Idtdb Instantiate(IConfiguration cfg)
        {
            _conn = cfg.GetConnectionString("DG")!;
            if (_db == null)
            {
                _dbi = InstantiateDB();
            }
            return _db!;
        }

        //Mappings
        private MapperConfiguration PlanItemMapConfig = dtPlanItemModel.mapperConfiguration;

        public Idtdb db() { return _dbi; }

        public dtdb dtdb() { return _db; }

        #region Utility
        public void ClearResetFlags()
        {
            if (_db == null) throw new Exception("DB not set");
            var element = (from x in _db.dtMiscs where x.title == DTDBConstants.AuthTokensNeedToBeResetKey select x).FirstOrDefault();
            if (element != null)
            {
                _db.dtMiscs.Remove(element);
                Save();
            }
        }
        public static DTViewModel SetCredentials(string token)
        {
            var vm = new DTViewModel();
            return vm;
        }
        public void Save()
        {
            try
            {
                _db.SaveChanges();
            }
            catch (Exception ex) { if (!string.IsNullOrEmpty(ex.Message)) Debug.WriteLine(ex.Message); }
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
            Save();
        }
        #endregion Utility

        #region Data Accessors
        public List<dtColorCode> ColorCodes
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtColorCodes select x).ToList();
            }
        }
        public List<dtMisc> Misces
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtMiscs select x).ToList();
            }
        }
        public List<dtPlanItem> PlanItems
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtPlanItems select x).ToList();
            }
        }
        public List<dtProject> Projects
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtProjects select x).ToList();
            }
        }
        public List<dtSession> Sessions
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtSessions select x).ToList();
            }
        }
        public List<dtStatus> Stati
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtStatuses select x).ToList();
            }
        }
        public List<dtTestDatum> TestData
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtTestData select x).ToList();
            }
        }
        public List<dtType> Types
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtTypes select x).ToList();
            }
        }
        public Task<List<dtUser>> UsersAsync
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return Task.Run(() => (from x in _db.dtUsers select x).ToList());
            }
        }

        public List<dtUser> Users
        {
            get
            {
                if (_db == null) throw new Exception("DB not set");
                return (from x in _db.dtUsers select x).ToList();
            }
        }
        #endregion Data Accessors

        #region DTO Data Access
        public dtColorCodeModel ColorCodeDTO(dtColorCode colorCode)
        {
            return new dtColorCodeModel(colorCode);
        }
        public List<dtColorCodeModel> ColorCodeDTOs()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtColorCode, dtColorCodeModel>(); }));
            return mappr.Map<List<dtColorCodeModel>>((from x in _db.dtColorCodes select x).OrderBy(x => x.title).ToList());
        }
        public List<dtProjectModel> ProjectDTOs(int userId)
        {
            if (_db == null) _db = InstantiateDB() as dtdb;
            List<dtProjectModel> projects = new List<dtProjectModel>();
            return ProjectDTOs((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault());
        }
        public List<dtProjectModel> ProjectDTOs(dtUser? u)
        {
            if (_db == null) throw new Exception("DB not set");
            List<dtProjectModel> projects = new List<dtProjectModel>();
            if (u == null) return projects;
            var ps = (from x in _db.dtProjects where x.user == u.id select x).ToList();
            var config = dtProjectModel.mapperConfiguration;
            var mapper = new Mapper(config);
            foreach (var p in ps)
            {
                var mdl = mapper.Map<dtProjectModel>(p);
                mdl.user.refreshToken = "";
                mdl.user.token = "";
                projects.Add(mdl);
            }
            Save();
            return projects;
        }
        public List<dtPlanItemModel> PlanItemDTOs(dtUser? user,
                                                    int daysBack = 1,
                                                    bool includeCompleted = false,
                                                    bool getAll = false,
                                                    int onlyProject = 0,
                                                    bool onlyRecurrences = false)
        {
            var results = new List<dtPlanItemModel>();
            if (_db == null) _db = InstantiateDB() as dtdb;
            if (user == null) return new List<dtPlanItemModel>();
            _currentUser = user;
            var mapper = new Mapper(PlanItemMapConfig);
            var dateToday = DateTime.Parse(DateTime.Now.AddHours(DTDBConstants.TZOffset).ToShortDateString());

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
            Save();
            var items = (from x in _db.dtPlanItems where x.user == user.id select x)
                .OrderBy(x => x.day)
                .ThenBy(x => x.completed)
                .ThenBy(x => x.start)
                .ThenByDescending(x => (x.priority == null ? 0 : (x.priority.HasValue ? x.priority.Value : 0)))
                .ThenByDescending(x => (x.projectNavigation == null ? 0 : (x.projectNavigation.priority.HasValue ? x.projectNavigation.priority.Value : 0)))
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
            DateTime startOfToday = DateTime.Parse(DateTime.Now.AddHours(DTDBConstants.TZOffset).ToShortDateString());
            foreach (var i in items)
            {
                var mdl = new dtPlanItemModel(i);
                if (!mdl.recurrence.HasValue)
                {
                    mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Future];
                    if (mdl.day < startOfToday) mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Out_of_date];
                    if (mdl.day == startOfToday && mdl.start < DateTime.Now.AddHours(DTDBConstants.TZOffset) && (mdl.start + mdl.duration) < DateTime.Now.AddHours(DTDBConstants.TZOffset)) mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Pastdue];
                    if (mdl.completed.HasValue && mdl.completed.Value) mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Complete];
                    if (mdl.day == startOfToday && !(mdl.completed.HasValue && mdl.completed.Value) && mdl.statusColor != DTDBConstants.StatusColors[(int)DtStatus.Pastdue])
                    {
                        mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Current];
                        if (mdl.start < DateTime.Now.AddHours(DTDBConstants.TZOffset) && (mdl.start + mdl.duration) > DateTime.Now.AddHours(DTDBConstants.TZOffset)) mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Working];
                        for (int j = results.Count - 1; j >= 0 && results[j].day == mdl.day && (mdl.statusColor == DTDBConstants.StatusColors[(int)DtStatus.Current] || mdl.statusColor == DTDBConstants.StatusColors[(int)DtStatus.Working]); j--)
                        {
                            if (!results[j].recurrence.HasValue)
                            {
                                if (results[j].start <= mdl.start && (results[j].start + results[j].duration) > mdl.start)
                                {
                                    mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Conflict];
                                    if (mdl.duration.TotalMinutes < 1) mdl.statusColor = DTDBConstants.StatusColors[(int)DtStatus.Subitem];
                                }
                            }
                        }
                    }
                }
                results.Add(mdl);
            }
            return results;
        }

        // We want this to run in its own thread and the app likely has already returned the controller method.
        // We use the static to avoid problems with the db context leaving scope.
        public List<dtPlanItemModel> PlanItemDTOs(int userId,
                                                  int daysBack = 1,
                                                  bool includeCompleted = false,
                                                  bool getAll = false,
                                                  int onlyProject = 0,
                                                  bool onlyRecurrences = false
                                                  )
        {
            if (_db == null)
            {
                _db = InstantiateDB() as dtdb;
            }
            return PlanItemDTOs((from x in _db.dtUsers where x.id == userId select x).FirstOrDefault(), daysBack, includeCompleted, getAll, onlyProject, onlyRecurrences);
        }

        public List<dtProject> ProjectsForUser(int userId)
        {
            if (_db == null) throw new Exception("DB not set");
            return (from x in _db.dtProjects where x.user == userId select x).ToList();
        }
        public List<dtRecurrenceModel> RecurrenceDTOs()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtRecurrence, dtRecurrenceModel>(); }));
            return mappr.Map<List<dtRecurrenceModel>>(from x in _db.dtRecurrences select x).ToList();
        }
        public List<dtStatusModel> StatusDTOs()
        {
            if (_db == null) throw new Exception("DB not set");
            var mappr = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtStatus, dtStatusModel>(); }));
            return mappr.Map<List<dtStatusModel>>((from x in _db.dtStatuses select x).OrderBy(x => x.title).ToList());

        }
        public dtUserModel UserModelForSession(string session, string hostAddress)
        {
            if (_db == null) _db = InstantiateDB() as dtdb;
            dtUserModel mappedUser = new dtUserModel();
            if (!string.IsNullOrEmpty(session))
            {
                var sessionRecord = (from x in _db.dtSessions where x.session == session && x.hostAddress == hostAddress select x).FirstOrDefault();
                if (sessionRecord == null || sessionRecord.expires < DateTime.Now)
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
                        //Log(new dtMisc() { title = "UserModelForSession data: ", value = "User is null." });
                        _db.dtSessions.Remove(sessionRecord);
                    }
                    else
                    {
                        var config = dtUserModel.mapperConfiguration;
                        var mapper = new Mapper(config);
                        mappedUser = mapper.Map<dtUserModel>(user);
                        mappedUser.token = "";
                        mappedUser.refreshToken = "";
                        sessionRecord.expires = DateTime.Now.AddDays(7);
                    }
                }
                Save();
            }
            return mappedUser!;
        }
        #endregion DTO Data Access

        #region Data Maniuplation
        public bool Adjust(int userId)
        {
            var today = DateTime.Parse(DateTime.Now.AddHours(DTDBConstants.TZOffset).ToShortDateString());
            if (_db == null) throw new Exception("DB not set");
            var variedItems = (from x in _db.dtPlanItems
                               where x.user == userId
                         && x.day == today
                         && (!x.recurrence.HasValue || !(x.recurrence.Value > 0))
                         && (!x.completed.HasValue || !(x.completed.Value))
                         && x.duration.HasValue
                         && (!x.fixedStart.HasValue || !(x.fixedStart.Value))
                               select x)
                .OrderBy(x => x.day)
                .ThenByDescending(x => x.priority == null ? 0 : (x.priority.HasValue ? x.priority.Value : 0))
                .ThenByDescending(x => (x.projectNavigation == null ? 0 : (x.projectNavigation.priority.HasValue ? x.projectNavigation.priority.Value : 0)))
                .ThenBy(x => x.start)
                .ToList();
            variedItems = variedItems.Where(x => (x.duration != null && x.duration.HasValue && (x.duration.Value.TotalSeconds > 0))).ToList();
            var fixedItems = (from x in _db.dtPlanItems
                              where x.user == userId
                              && x.day == today
                              && (!x.recurrence.HasValue || !(x.recurrence.Value > 0))
                              && (!x.completed.HasValue || !(x.completed.Value))
                              && x.fixedStart.HasValue
                              && x.fixedStart.Value
                              && x.duration.HasValue
                              select x)
                .OrderBy(x => x.day)
                .ThenBy(x => x.start)
                .ThenBy(x => (x.priority == null ? 0 : (x.priority.HasValue ? x.priority.Value : 0)))
                .ThenByDescending(x => (x.projectNavigation == null ? 0 : (x.projectNavigation.priority.HasValue ? x.projectNavigation.priority.Value : 0)))
                .ToList();
            fixedItems = fixedItems.Where(x => x.duration.Value.TotalSeconds > 0).ToList();
            foreach (var item in variedItems)
            {
                var nextOpenTime = DateTime.Now.AddHours(DTDBConstants.TZOffset);
                var targetEnd = nextOpenTime + item.duration.Value;
                bool foundSpot = false;
                for (int i = 0; i < fixedItems.Count && !foundSpot; i++)
                {
                    if (targetEnd <= fixedItems[i].start.Value)
                    {
                        foundSpot = true;
                        item.start = nextOpenTime;
                        fixedItems.Insert(i, item);
                    }
                    else
                    {
                        nextOpenTime = fixedItems[i].start.Value + fixedItems[i].duration.Value;
                        targetEnd = nextOpenTime + item.duration.Value;
                    }
                }
                if (!foundSpot)
                {
                    item.start = nextOpenTime;
                    fixedItems.Add(item);
                }
            }
            var itemsToUpdate = fixedItems.Where(x => (!(x.fixedStart.HasValue) || !(x.fixedStart.Value))).ToList();
            foreach (var item in itemsToUpdate)
            {
                Set(item);
            }
            return true;
        }
        public bool Delete(dtMisc item)
        {
            if (item == null) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtMiscs.Remove(item);
            Save();
            return true;
        }
        public bool Delete(dtPlanItem item)
        {
            if (item == null) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtPlanItems.Remove(item);
            Save();
            return true;
        }
        public bool Delete(dtProject project)
        {
            if (project == null) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtProjects.Remove(project);
            Save();
            return true;
        }
        public bool Delete(dtSession session)
        {
            if (session == null) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtSessions.Remove(session);
            Save();
            return true;
        }
        public bool Delete(dtUser user)
        {
            if (user == null) return false;
            if (_db == null) throw new Exception("DB not set");
            var u = _db.dtUsers.Where(x => x.id == user.id).Include(x => x.dtPlanItems).FirstOrDefault();
             _db.dtUsers.Remove(user);
            Save();
            return true;
        }
        public bool Delete(List<dtPlanItem> planItems)
        {
            if (planItems == null || planItems.Count == 0) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtPlanItems.RemoveRange(planItems);
            Save();
            return true;

        }
        public bool Delete(List<dtProject> projects)
        {
            if (projects == null || projects.Count == 0) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtProjects.RemoveRange(projects);
            Save();
            return true;
        }
        public bool Delete(List<dtSession> sessions)
        {
            if (sessions == null || sessions.Count == 0) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtSessions.RemoveRange(sessions);
            Save();
            return true;
        }
        public bool Delete(List<dtTestDatum> testData)
        {
            if (testData == null || testData.Count == 0) return false;
            if (_db == null) throw new Exception("DB not set");
            _db.dtTestData.RemoveRange(testData);
            Save();
            return true;
        }
        public bool DeletePlanItem(int planItemId, int userId, bool deleteChildren = false)
        {
            if (_db == null) throw new Exception("DB not set");
            var item = (from x in _db.dtPlanItems where x.id == planItemId && x.user == userId select x).FirstOrDefault();
            if (item == null) return false;
            if (item.recurrence.HasValue)
            {
                var children = (from x in _db.dtPlanItems where x.parent.Value == item.id select x).ToList();
                if (deleteChildren)
                {
                    _db.dtPlanItems.RemoveRange(children);
                }
                else
                {
                    foreach (var c in children)
                    {
                        c.parent = null;
                    }
                }
                Save();
            }
            _db.dtPlanItems.Remove(item);
            Save();

            return true;
        }
        public bool DeleteProject(int projectId, int userId, bool deleteProjItems = true, int transferProject = 0)
        {
            if (_db == null) throw new Exception("DB not set");
            var project = (from x in _db.dtProjects where x.id == projectId select x).FirstOrDefault();
            if (project == null) throw new Exception("Project does not exist.");
            if (deleteProjItems)
            {
                var planItems = (from x in _db.dtPlanItems where x.project == project.id && x.recurrence == null select x).ToList();
                _db.dtPlanItems.RemoveRange(planItems);
                Save();
                planItems = (from x in _db.dtPlanItems where x.project == project.id select x).ToList();
                _db.dtPlanItems.RemoveRange(planItems);
                Save();
            }
            if (transferProject > 0)
            {
                var newProj = (from x in _db.dtProjects where x.id == transferProject select x).FirstOrDefault();
                if (newProj != null)
                {
                    var planItems = (from x in _db.dtPlanItems where x.project == project.id select x).ToList();
                    foreach (var i in planItems) i.project = transferProject;
                    Save();
                }
            }
            var remainingLinkedItems = (from x in _db.dtPlanItems where x.project == project.id select x).ToList();
            foreach (var i in remainingLinkedItems) i.project = null;
            _db.dtProjects.Remove(project);
            Save();
            return true;
        }
        // Returns false if no propagations are made. True if at least one propagation is made.
        public dtMisc Log(dtMisc aLogEntry)
        {
            if (aLogEntry == null) return new dtMisc();
            if (_db == null) _db = InstantiateDB() as dtdb;
            if (aLogEntry.id > 0) throw new Exception("Attempted to alter log entry");
            _db.Add(aLogEntry);
            Save();
            return aLogEntry;
        }
        private List<dtPlanItem> PopulateRecurrences()
        {
            List<dtPlanItem> items = new List<dtPlanItem>();
            try
            {
                //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                // or Trace.Listeners.Add(new ConsoleTraceListener());
                //Trace.WriteLine("Hello World");              
                if (_db == null) return items;
                var db = _db;
                if (_recurringItem == null) return items;

                var config = new MapperConfiguration(cfg => { cfg.CreateMap<dtPlanItem, dtPlanItem>(); });
                var mapper = new Mapper(config);
                var seed = mapper.Map<dtPlanItem>(_recurringItem);
                seed.id = 0;
                seed.recurrence = null;
                seed.recurrenceData = null;
                seed.parent = _recurringItem.id;
                seed.recurrenceNavigation = null;

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
                if ((_recurringItem.recurrence == (int)DtRecurrence.Semi_monthly && _recurringItem.recurrenceData != null && _recurringItem.recurrenceData.Split(":").Length > 0) ||
                    (_recurringItem.recurrence == (int)DtRecurrence.Monthly_nth_day && _recurringItem.recurrenceData != null && _recurringItem.recurrenceData.Split(":").Length > 0))
                {
                    int.TryParse(_recurringItem.recurrenceData.Split(":")[0], out numWksInCycle);
                    if (_recurringItem.recurrenceData.Split(":").Length > 1) mask = _recurringItem.recurrenceData.Split(":")[1];
                }
                if (!string.IsNullOrEmpty(_recurringItem.recurrenceData) && _recurringItem.recurrenceData.Split(":").Length > 1) mask = _recurringItem.recurrenceData.Split(":")[1];

                DateTime rStart = _recurringItem.start.Value;
                //Trace.WriteLine(rStart);
                //Trace.WriteLine(numWksInCycle);
                DateTime today = DateTime.Parse(DateTime.Now.ToShortDateString());
                today = today.AddHours(rStart.Hour).AddMinutes(rStart.Minute);
                //Trace.WriteLine(today);
                for (int i = 0; i < 30 && !string.IsNullOrEmpty(_recurringItem.recurrenceData); i++)
                {
                    DateTime test = today.AddDays(i);
                    //Trace.WriteLine(i + ": " + test.Day + " / " + numWksInCycle);
                    //Trace.WriteLine(_recurringItem.recurrence == (int)DtRecurrence.Monthly_nth_day ? "T" : "F");
                    //Trace.WriteLine((int)(test.Day / 7) == numWksInCycle ? "T" : "F");
                    //Trace.WriteLine(mask.Length > (int)test.DayOfWeek && mask[(int)test.DayOfWeek] != '-');
                    if (test >= _recurringItem.start.Value &&
                        (
                            (_recurringItem.recurrence == (int)DtRecurrence.Daily_Weekly &&
                                _recurringItem.recurrenceData.Length > (int)test.DayOfWeek &&
                                (mask.Length > (int)test.DayOfWeek && mask[(int)test.DayOfWeek] != '-'
                            ) ||
                            (_recurringItem.recurrence == (int)DtRecurrence.Monthly &&
                                dateMap.ContainsKey(test.Day) &&
                                dateMap[test.Day]
                            ) ||
                            (_recurringItem.recurrence == (int)DtRecurrence.Semi_monthly &&
                                numWksInCycle > 0 &&
                                (int)((test - rStart).TotalDays / 7) % numWksInCycle == 0 &&
                                (mask.Length > (int)test.DayOfWeek && mask[(int)test.DayOfWeek] != '-')
                            ) ||
                            (_recurringItem.recurrence == (int)DtRecurrence.Monthly_nth_day &&
                                numWksInCycle > 0 &&
                                (int)((test.Day + 6) / 7) == numWksInCycle &&
                                (mask.Length > (int)test.DayOfWeek && mask[(int)test.DayOfWeek] != '-')
                            )
                       ))
                      )
                    {
                        //Trace.WriteLine("ADD ON THIS DAY");
                        AddOnThisDay[i] = true;
                    }
                }

                for (int i = 0; i < 30; i++)
                {
                    DateTime test = DateTime.Parse(DateTime.Now.ToShortDateString()).AddDays(i);
                    if (AddOnThisDay[i])
                    {
                        if ((from x in db.dtPlanItems where x.recurrence == null && x.day == test && x.parent == _recurringItem.id select x).FirstOrDefault() == null)
                        {
                            test = test.AddHours(seed.start.Value.Hour);
                            test = test.AddMinutes(seed.start.Value.Minute);
                            seed.day = DateTime.Parse(test.ToShortDateString());
                            seed.start = test;
                            var item = (mapper.Map<dtPlanItem>(seed));
                            items.Add(item);
                            db.dtPlanItems.Add(item);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return items;
        }
        public void RemoveOutOfDateSessions()
        {
            var outOfDates = (from x in _db.dtSessions where x.expires < DateTime.Now select x).ToList();
            _db.RemoveRange(outOfDates);
            Save();
        }
        public bool Propagate(int itemId, int userId)
        {
            if (_db == null) _db = InstantiateDB() as dtdb;
            bool result = false;
            var item = (from x in _db.dtPlanItems where x.id == itemId && x.user == userId select x).FirstOrDefault();
            if (item == null || (!item.recurrence.HasValue && !item.parent.HasValue)) return false;
            if (item.id != itemId) result = true; // If the child was a seed, then AT LEAST the parent will be updated.
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
                parent.fixedStart = item.fixedStart;
            }
            else
            {
                parent = item;
            }
            if (!parent.recurrence.HasValue) return false;
            var children = (from x in _db.dtPlanItems where x.parent.Value == parent.id select x).ToList();
            if (children.Count > 0) result = true;
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
                c.fixedStart = parent.fixedStart;
            }
            Save();
            return result;
        }
        public dtMisc Set(dtMisc item)
        {
            if (_db == null) _db = InstantiateDB() as dtdb;
            dtMisc? existing = null;
            if (item.id > 0)
            {
                existing = (from x in _db.dtMiscs where x.id == item.id select x).FirstOrDefault();
            }
            if (existing == null)
            {
                existing = item;
            }
            else
            {
                existing.title = item.title;
                existing.value = item.value;
            }
            if (existing.id < 1) _db.dtMiscs.Add(existing);
            Save();
            return existing;
        }
        public dtProject Set(dtProject project)
        {
            if (_db == null) _db = InstantiateDB() as dtdb;
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
            Save();
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
            if (_db == null) _db = InstantiateDB() as dtdb;
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
            if (planItem.fixedStart.HasValue) item.fixedStart = planItem.fixedStart.Value;
            else item.fixedStart = null;
            if (item.id < 1) _db.dtPlanItems.Add(item);
            Save();
            if (item.recurrence.HasValue)
            {
                _recurringItem = item;
                var rItems = PopulateRecurrences();
            }

            return item;
        }
        public dtSession Set(dtSession aSession)
        {
            if (aSession == null) return null;
            if (_db == null) _db = InstantiateDB() as dtdb;

            dtSession sessionToSave = null;
            if (aSession.id > 0) sessionToSave = (from x in _db.dtSessions where x.id == aSession.id select x).FirstOrDefault();
            if (sessionToSave == null) sessionToSave = new dtSession();
            sessionToSave.session = aSession.session;
            sessionToSave.expires = DateTime.Now.AddDays(7);
            sessionToSave.user = aSession.user;
            sessionToSave.hostAddress = aSession.hostAddress;
            if (sessionToSave.id <= 0) _db.dtSessions.Add(sessionToSave);
            Save();
            return sessionToSave;
        }
        public dtUser Set(dtUser aUser)
        {
            if (aUser == null) return null;
            if (_db == null) _db = InstantiateDB() as dtdb;

            dtUser userToSave = null;
            if (aUser.id > 0) userToSave = (from x in _db.dtUsers where x.id == aUser.id select x).FirstOrDefault();
            if (userToSave == null) userToSave = new dtUser();
            userToSave.updated = DateTime.Now;
            userToSave.pw = aUser.pw;
            userToSave.suspended = aUser.suspended;
            userToSave.email = aUser.email;
            userToSave.token = aUser.token;
            userToSave.type = aUser.type;
            userToSave.fName = aUser.fName;
            userToSave.lName = aUser.lName;
            userToSave.otherName = aUser.otherName;
            userToSave.refreshToken = aUser.refreshToken;
            userToSave.lastLogin = aUser.lastLogin;
            if (userToSave.id <= 0) _db.Add(userToSave);
            Save();

            return userToSave;
        }
        public void SetConnString(string? conn)
        {
            if (conn != null) _conn = conn;
        }
        public bool SetIfTesting(string key, string value)
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
        public dtLogin? SetLogin(string email, string hostAddress)
        {
            dtLogin login = new dtLogin();
            RemoveOutOfDateSessions();
            var user = Users.Where(x => x.email == email).FirstOrDefault();
            if (user == null) return null;
            var session = Sessions.Where(x => x.user == user.id && x.hostAddress == hostAddress).FirstOrDefault();
            if (session == null)
            {
                session = Set(new dtSession() { user = user.id, hostAddress = hostAddress, expires = DateTime.Now.AddDays(7), session = Guid.NewGuid().ToString() });
            }
            if (session != null) 
            { 
                login.Session = session.session;
                login.Email = user.email;
                login.FName = user.fName;
                login.LName = user.lName;
                if (user.doNotSetPW != null) login.DoNotSetPW = user.doNotSetPW;
                if ((!string.IsNullOrEmpty(user.pw) || !string.IsNullOrWhiteSpace(user.pw)) && user.doNotSetPW == null) login.DoNotSetPW = true;
                session.expires = DateTime.Now.AddDays(7);
                session.hostAddress = hostAddress;
                session = Set(session);
            }
            else
            {
                Log(new dtMisc() { title = "Cannot set session", value = "Invalid session; email: " + email + "; host: " + hostAddress });
                login.Message = "Invalid session";
            }
            return login;
        }
        public dtLogin? SetLogin(string email, string fname, string lname, string hostAddress, int userType, string accessToken, string refreshToken)
        {
            dtUser? usr = Users.Where(x => x.email == email).FirstOrDefault();
            if (usr == null)
            {
                usr = new dtUser() { email = email };
            }
            usr.fName = fname;
            usr.lName = lname;
            usr.token = accessToken;
            usr.refreshToken = refreshToken;
            usr.type = userType;
            usr = Set(usr);
            return SetLogin(email, hostAddress);
        }
        public void SetUser(int userId)
        {
            _userId = userId;
        }
        public bool SetUserPW(string? pw)
        {
            if (_db == null) throw new Exception("DB not set");
            var usr = (from x in _db.dtUsers where x.id == _userId select x).FirstOrDefault();
            if (usr == null) return false;
            if (pw != null) usr.pw = pw;
            else
            {
                usr.pw = null;
                usr.doNotSetPW = true;
            }
            Save();
            return true;
        }
        // Returns the number of new items created by the update.
        public int UpdateRecurrences(int userId, int sourceItem = 0, bool force = false)
        {
            int itemCt = 0;
            _userId = userId;
            if (_db == null) _db = InstantiateDB() as dtdb;
            var user = (from x in _db.dtUsers where x.id == _userId select x).FirstOrDefault();
            if (user == null) return 0;
            if (!force && user.updated.HasValue && (DateTime.Parse(DateTime.Now.ToShortDateString()) - DateTime.Parse(user.updated.Value.ToShortDateString())).TotalDays < 1) return 0;
            user.updated = DateTime.Parse(DateTime.Now.ToShortDateString());
            Save();
            _currentUser = user;
            var recurrences = new List<dtPlanItem>();
            if (sourceItem > 0)
            {
                var item = (from x in _db.dtPlanItems where x.id == sourceItem && x.user == _userId select x).FirstOrDefault();
                if (item != null && item.parent.HasValue)
                {
                    item = (from x in _db.dtPlanItems where x.id == item.parent.Value select x).FirstOrDefault();
                }
                if (item != null && item.recurrence.HasValue) recurrences.Add(item);
            }
            else
            {
                recurrences = (from x in _db.dtPlanItems where x.user == _userId && x.recurrence != null select x).ToList();
            }
            // Need to process recurrences
            foreach (var r in recurrences)
            {
                _recurringItem = r;
                itemCt += PopulateRecurrences().Count;
            }
            return itemCt;
        }
        #endregion Data Manipulation

        // This is left at the bottom because it is a special method that is not part of normal use.
        public static void GeneralUtil(dtdb db)
        {
            /*
            _currentUser = (from x in db.dtUsers where x.id == 2 select x).FirstOrDefault();
            _userId = _currentUser.id;
            _recurringItem = (from x in db.dtPlanItems where x.id == 4919 select x).FirstOrDefault();
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
            */
        }

    }
#pragma warning restore
}
