namespace DanTech.Data
{
    public enum DtRecurrence
    {
        Daily_Weekly = 1,
        Monthly = 2,
        Semi_monthly = 3,
        Monthly_nth_day = 4
    }

    public enum DtStatus
    {
        Active = 1,
        Pending = 2,
        Closed = 3,
        Complete = 4,
        Cancelled = 5,
        Inactive = 6,
        Postponed = 7,
        Delayed = 8,
        Test = 9,
        Out_of_date = 10,
        Future = 11,
        Current = 12,
        Working = 13,
        Subitem = 14,
        Conflict = 15,
        Pastdue = 16
    }

    public enum DtUserType
    {
        User = 1,
        Admin = 2,
        Guest = 3,
        dtPlaner = 4,
        gmail = 5
    }

    public enum DtAuthorizationKey
    {
        Planner = 1,
        Gmail = 2
    }
}
