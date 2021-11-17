using HangfireService.Core.Models.DBs;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface ITpHelper
    {
        int TpId { get;}

        Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList);
    }
}
