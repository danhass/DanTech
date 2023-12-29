using DanTech.Data;
using DanTech.Data.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace DanTech.Services
{
    public interface IDTDBDataService
    {
        Idtdb db();

        // Utility
        void ClearResetFlags();
        Idtdb Instantiate(IConfiguration cfg);
        bool PendingChanges();
        void Save();
        void SetConnString(string? conn);
        void SetUser(int userId);
        bool SetUserPW(string pw);
        void ToggleTestFlag();

        // Accessors for data lists
        List<dtColorCode> ColorCodes { get; }
        List<dtFood> Foods { get; }
        List<dtFoodLog> FoodLogs { get; }
        List<dtPlanItem> PlanItems { get; }
        List<dtProject> Projects { get; }
        List<dtMisc> Misces { get; }
        List<dtSession> Sessions { get; }
        List<dtStatus> Stati { get; }
        List<dtTestDatum> TestData { get; }
        List<dtType> Types { get; }
        List<dtUnitOfMeasure> UnitOfMeasures { get; }
        List<dtUser> Users { get; }
        List<dtRegistration> Registrations { get; }
        Task<List<dtUser>> UsersAsync {  get; }

        // DTO Data Access
        dtColorCodeModel ColorCodeDTO(dtColorCode colorCode);
        List<dtColorCodeModel> ColorCodeDTOs();
        List<dtProjectModel> ProjectDTOs(int userId);
        List<dtProjectModel> ProjectDTOs(dtUser u);
        List<dtProject> ProjectsForUser(int userId);
        List<dtPlanItemModel> PlanItemDTOs(dtUser user,
                                        int daysBack = 1,
                                        bool includeCompleted = false,
                                        bool getAll = false,
                                        int onlyProject = 0,
                                        bool onlyRecurrences = false);
        List<dtPlanItemModel> PlanItemDTOs(int userId,
                                        int daysBack = 1,
                                        bool includeCompleted = false,
                                        bool getAll = false,
                                        int onlyProject = 0,
                                        bool onlyRecurrences = false);
        List<dtRecurrenceModel> RecurrenceDTOs();
        List<dtStatusModel> StatusDTOs();
        dtUserModel UserModelForSession(string session, string hostAddress);

        // Data Manipulation
        bool Adjust(int userId);
        bool Delete(dtPlanItem item);
        bool Delete(dtFood food);
        bool Delete(dtFoodLog item);
        bool Delete(dtMisc item);
        bool Delete(dtProject project);
        bool Delete(dtRegistration item);
        bool Delete(dtSession session);
        bool Delete(dtUnitOfMeasure measure);
        bool Delete(List<dtFood> items);
        bool Delete(List<dtFoodLog> items);
        bool Delete(List<dtPlanItem> planItems);
        bool Delete(List<dtProject> projects);
        bool Delete(List<dtRegistration> registrations);
        bool Delete(List<dtSession> sessions);
        bool Delete(List<dtTestDatum> testData);
        bool Delete(List<dtUnitOfMeasure> items);
        bool Delete(dtUser user);
        bool Delete(List<dtUser> users);
        bool DeletePlanItem(int planItemId, int userId, bool deleteChildren = false);
        bool DeleteProject(int projectId, int userId, bool deleteProjItems = true, int transferProject = 0);
        dtMisc Log(dtMisc aLogEntry);
        bool Propagate(int itemId, int userId);
        void RemoveOutOfDateSessions();
        dtFood Set(dtFood item);
        dtFoodLog Set(dtFoodLog item);
        dtUnitOfMeasure Set(dtUnitOfMeasure item);
        dtMisc Set(dtMisc item);
        dtProject Set(dtProject project);
        dtPlanItem Set(dtPlanItem planItem);
        dtPlanItem Set(dtPlanItemModel planItem);
        dtRegistration Set(dtRegistration item);
        dtUser Set(dtUser aUser);
        dtSession Set(dtSession aSession);

        dtLogin? SetLogin(string email, string hostAddress);
        public dtLogin? SetLogin(string email, string fname, string lname, string hostAddress, int userType, string accessToken, string refreshToken);
        int UpdateRecurrences(int userId, int sourceItem = 0, bool force = false);       
    }
}
