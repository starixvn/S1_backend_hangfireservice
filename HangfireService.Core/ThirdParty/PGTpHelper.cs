using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.ThirdParty.PG;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.ThirdParty
{
    public class PGTpHelper : BaseTpHelper, ITpHelper
    {
        public int TpId { get; } = 14;

        private readonly ThirdPartyModel _thirdPartyModel;

        public PGTpHelper(IServiceProvider provider) : base(provider)
        {
            _thirdPartyModel = provider.GetRequiredService<IEnumerable<ThirdPartyModel>>().First(item => item.TpId == TpId);
        }

        public async Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList)
        {
            var result = new List<bool>();
            Parallel.ForEach(todoList, todo =>
            {
                try
                {
                    var detailResult = GetBetDetailByTransID(todo).Result;
                    var data = detailResult?.data?.FirstOrDefault();
                    //資料會延遲3-5分鐘，先回傳成功等待下次處理
                    if (detailResult?.error == null && data == null)
                    {
                        result.Add(true);
                        return;
                    }

                    var detail = new TpBetRecordDetail(todo)
                    {
                        IsSuccess = detailResult?.error == null && data != null,
                        Detail = data,
                    };

                    if (detail.IsSuccess)
                    {
                        //成功新增Detail
                        var insert = AddTpBetRecordDetail(detail).Result;
                        result.Add(insert);
                    }
                    else
                    {
                        //失敗更新todo status
                        _logger.LogWarning($"PG Error: {JsonConvert.SerializeObject(detailResult)}");
                        var update = UpdateTpDetailTodo(detail).Result;
                        result.Add(update);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    result.Add(false);
                }

            });
            return result;
        }

        private async Task<GetBetDetailsResponse> GetBetDetailByTransID(TpDetailTodo todo)
        {
            try
            {
                var tpSetting = _thirdPartyModel.TpSetting;
                var url = $"{tpSetting["ApiUrl"]}-datagrabber/Bet/v4/GetBetDetails?trace_id={Guid.NewGuid()}";
                var parameters = $"operator_token={tpSetting["operator_token"]}&secret_key={tpSetting["secret_key"]}&bet_ids={todo.TransId}";
                var result = await HttpPostAsync(url, parameters, "application/x-www-form-urlencoded");

                using Stream s = await result.Content.ReadAsStreamAsync();
                using StreamReader sr = new StreamReader(s);
                using JsonReader reader = new JsonTextReader(sr);
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<GetBetDetailsResponse>(reader);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return default;
            }
        }

    }
}
