using DanTech.Data;
using DanTech.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Services
{
    public interface IDTDBDataService
    {
        Idtdb db();

        // Utility
        void ClearResetFlags();
        void Save();
        void SetConnString(string conn);
        void SetUser(int userId);
        bool SetUserPW(string pw);
        void ToggleTestFlag();

        // Data Access
        List<dtColorCodeModel> ColorCodes();
        List<dtProjectModel> DTProjects(int userId);
        List<dtProjectModel> DTProjects(dtUser u);
        List<dtProject> Projects(int userId);
        List<dtPlanItem> PlanItems();
        List<dtPlanItemModel> PlanItems(dtUser user,
                                        int daysBack = 1,
                                        bool includeCompleted = false,
                                        bool getAll = false,
                                        int onlyProject = 0,
                                        bool onlyRecurrences = false);
        List<dtPlanItemModel> PlanItems(int userId,
                                        int daysBack = 1,
                                        bool includeCompleted = false,
                                        bool getAll = false,
                                        int onlyProject = 0,
                                        bool onlyRecurrences = false);
        List<dtRecurrenceModel> Recurrences();
        List<dtSession> Sessions();
        List<dtStatusModel> Stati();
        List<dtTestDatum> TestData();
        dtUserModel UserModelForSession(string session, string hostAddress);
        List<dtUser> Users();

        // Data Manipulation
        bool Adjust(int userId);
        bool Delete(dtSession session);
        bool Delete(dtUser user);
        bool Delete(List<dtPlanItem> planItems);
        bool Delete(List<dtProject> projects);
        bool Delete(List<dtSession> sessions);
        bool Delete(List<dtTestDatum> testData);
        bool DeletePlanItem(int planItemId, int userId, bool deleteChildren = false);
        bool DeleteProject(int projectId, int userId, bool deleteProjItems = true, int transferProject = 0);
        dtMisc Log(dtMisc aLogEntry);
        bool Propagate(int itemId, int userId);
        void RemoveOutOfDateSessions();
        dtProject Set(dtProject project);
        dtPlanItem Set(dtPlanItem planItem);
        dtPlanItem Set(dtPlanItemModel planItem);
        dtUser Set(dtUser aUser);
        dtSession Set(dtSession aSession);
        int UpdateRecurrences(int userId, int sourceItem = 0, bool force = false);
    }
}
