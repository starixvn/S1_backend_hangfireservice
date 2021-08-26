using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.ThirdParty.PG;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var detailResult = await GetBetDetailByList(todoList);
            Parallel.ForEach(todoList, async todo => 
            {
                try
                {
                    var data = detailResult?.data?.FirstOrDefault(item => item.betId.ToString() == todo.TransId);
                    var detail = new TpBetRecordDetail(todo)
                    {
                        IsSuccess = detailResult?.error == null && data != null,
                        Detail = data,
                    };

                    if (detail.IsSuccess)
                    {
                        //成功新增Detail
                        var insert = await AddTpBetRecordDetail(detail);
                        result.Add(insert);
                    }
                    else
                    {
                        //失敗更新todo status
                        var update = await UpdateTpDetailTodo(detail);
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

        private async Task<GetBetDetailsResponse> GetBetDetailByList(IEnumerable<TpDetailTodo> todoList)
        {
            try
            {
                var tpSetting = _thirdPartyModel.TpSetting;
                var url = $"{tpSetting["ApiUrl"]}-datagrabber/Bet/v4/GetBetDetails?trace_id={Guid.NewGuid()}";
                var parameters = new StringBuilder($"operator_token={tpSetting["operator_token"]}&secret_key={tpSetting["secret_key"]}&bet_ids=");
                parameters.AppendJoin("&bet_ids=", todoList.Select(item => item.TransId));

                var result = await HttpPostAsync(url, parameters.ToString(), "application/x-www-form-urlencoded");
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
