using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IDetailTpHelper
    {
        int TpId { get; }

        Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList, TpDetailOption option);
    }
}
