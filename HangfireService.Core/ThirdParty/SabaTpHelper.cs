using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.ThirdParty.Saba;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class SabaTpHelper : BaseTpHelper, ITpHelper
    {
        public int TpId { get; } = 1;

        private readonly Dictionary<int, ThirdPartyModel> _thirdPartyModel;

        public SabaTpHelper(IServiceProvider provider) : base(provider)
        {
            _thirdPartyModel = provider.GetRequiredService<IEnumerable<ThirdPartyModel>>()
                .Where(item => item.TpId == TpId).ToDictionary(item => item.SiteId, item => item);
        }

        public async Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList)
        {
            var result = new List<bool>();

            //要注意第三方api可否接受Parallel，不支援可改用foreach
            Parallel.ForEach(todoList, todo =>
            {
               try
               {
                   var detail = GetBetDetailByTransID(todo).Result;
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
               catch (Exception e)
               {
                   _logger.LogError(e, e.Message);
                   result.Add(false);
               }
            });

            return result;
        }

        private async Task<TpBetRecordDetail> GetBetDetailByTransID(TpDetailTodo todo)
        {
            try
            {
                var tpSetting = GetThirdPartyModel(todo.SiteId).TpSetting;
                var url = $"{tpSetting["ApiUrl"]}/GetBetDetailByTransID/";
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
