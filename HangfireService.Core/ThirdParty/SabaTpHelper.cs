using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.ThirdParty.Saba;
using HangfireService.Core.ThirdParty.Interfaces;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace HangfireService.Core.ThirdParty
{
    public class SabaTpHelper : BaseTpHelper, IDetailTpHelper
    {
        public int TpId { get; } = 41;

        private readonly Dictionary<int, ThirdPartyModel> _thirdPartyModel;
        private readonly List<int> _kenoGameTypeList;

        public SabaTpHelper(IServiceProvider provider) : base(provider)
        {
            _thirdPartyModel = provider.GetRequiredService<IEnumerable<ThirdPartyModel>>()
                .Where(item => item.TpId == TpId).ToDictionary(item => item.SiteId, item => item);

            _kenoGameTypeList = new List<int>
            {
                73,  // 202 OW Keno
                74, // 220 OW Lottery               
                75  // 222 OW Table Game
            };
        }

        public async Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList, TpDetailOption option)
        {
            var result = new List<bool>();


            //要注意第三方api可否接受Parallel，不支援可改用foreach
            Parallel.ForEach(
                todoList,
                new ParallelOptions() { MaxDegreeOfParallelism = option.TaskCount },
                todo =>
                    {
                        try
                        {
                            if (todo.GameType == 80)//LG虛擬體育目前無api，在ThirdPartyBetHistory處理細單，所以直接更新成功
                            {
                                var ignore = UpdateTpDetailTodo(new TpBetRecordDetail(todo)
                                {
                                    IsSuccess = true
                                }).Result;
                                result.Add(ignore);
                            }
                            else
                            {
                                var detail = GetBetDetail(todo).Result;
                                if (detail.IsSuccess)
                                {
                                    //成功新增Detail
                                    var insert = AddTpBetRecordDetail(detail).Result;
                                    result.Add(insert);
                                }
                                else
                                {
                                    //失敗更新todo status
                                    var update = UpdateTpDetailTodo(detail).Result;
                                    result.Add(update);
                                }
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
        private async Task<TpBetRecordDetail> GetBetDetail(TpDetailTodo todo)
        {
            try
            {
                var tpSetting = GetThirdPartyModel(todo.SiteId).TpSetting;
                var url = _kenoGameTypeList.Contains(todo.GameType) ?
                    $"{tpSetting["ApiUrl"]}/GetKenoDetailByTransID/" : $"{tpSetting["ApiUrl"]}/GetBetDetailByTransID/";
                var parameters = new Dictionary<string, string>
                {
                    { "vendor_id", tpSetting["VendorId"] },
                    { "trans_id", todo.TransId }
                };

                var result = await HttpPostAsync(url, parameters);
                using Stream s = await result.Content.ReadAsStreamAsync();
                using StreamReader sr = new StreamReader(s);
                using JsonReader reader = new JsonTextReader(sr);
                JsonSerializer serializer = new JsonSerializer();
                var response = serializer.Deserialize<GetBetDetailByTransIDResponse>(reader);

                return new TpBetRecordDetail(todo)
                {
                    IsSuccess = response.error_code.Equals("0"),
                    Detail = response.Data,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new TpBetRecordDetail(todo)
                {
                    IsSuccess = false,
                };
            }
        }

        private ThirdPartyModel GetThirdPartyModel(int SiteId)
        {
            return _thirdPartyModel.TryGetValue(SiteId, out var result) ? result : _thirdPartyModel[0];
        }
    }
}
