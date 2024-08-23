namespace HangfireService.Core.Models.Enums;

internal enum CustomerStatusEnum
{
    Running = 1,
    Suspend = 2,
    Closed = 3,
    Lock = 4,
    FreezeOneWeek = 97,
    FreezeOneMonth = 98,
    FreezeThreeMonth = 99 
}

public enum CustTaskType
{
    LastStatusUpdateTime = 2,
}