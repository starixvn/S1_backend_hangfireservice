using System.Net;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Service;

public class MpaUpdateExchangeRateService
{
    private readonly HttpHelper _httpHelper;
    private readonly ILogger<BaseJob> _logger;
    private readonly MpaExchangeRateConfig _mpaExchangeRateConfig;
    private readonly MainDBDataLayer _mainDBDataLayer;

    /// <summary>
    /// 目前 Nova88、Bwing、Goal123 有使用的幣別，只要抓這些就好，其他的幣別對應匯率暫時用不到也不用塞入db
    /// </summary>
    private List<string> _includeCurrencyName = new()
    {
        "MYR", "USD", "THB", "CNY", "IDR", "VND","INR","BDT", "KRW", "BRL", "JPY"
    };

    public MpaUpdateExchangeRateService(IServiceProvider provider)
    {
        _mpaExchangeRateConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<MpaExchangeRateConfig>("MpaExchangeRate");
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task<object> RunJobAsync(string reRunDate = "")
    {
        var accessToken = await AuthGetAccessToken();
        if (string.IsNullOrEmpty(accessToken)) return false;
        var updateDailyExchangeRateResult = await SetDailyExchangeRate(accessToken, reRunDate);
        return updateDailyExchangeRateResult;
    }

    /// <summary>
    /// 登入驗證，取得Token
    /// </summary>
    /// <returns>AccessToken</returns>
    private async Task<string> AuthGetAccessToken()
    {
        try
        {
            var postData = new
            {
                Username = _mpaExchangeRateConfig.Username,
                Password = _mpaExchangeRateConfig.Password,
            };

            var parameters = JsonConvert.SerializeObject(postData);
            var httpResponse = await _httpHelper.HttpPostNoProxyAsync($"{_mpaExchangeRateConfig.ApiUrl}/Auth", null, parameters, "application/json");

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<AuthResponse>(content);
                return response.AccessToken;
            }

            var failResponseBody = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError($"{_mpaExchangeRateConfig}/Auth api fail statusCode:{httpResponse.StatusCode} responseBody :{failResponseBody}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $@"MpaUpdateExchangeRateService.AuthGetAccessToken:{ex.Message}");
        }
        finally
        {
            await Task.CompletedTask;
        }

        return string.Empty;
    }


    /// <summary>
    /// 呼叫 VN Team API 取得匯率，並 新增 OR 更新至 DB
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="reRunDate">指定重跑日期，單獨跑那一天，手動觸發Job才會有 reRunDate </param>
    /// <returns>isSuccess</returns>
    private async Task<bool> SetDailyExchangeRate(string accessToken, string reRunDate = "")
    {
        var isSuccess = false;
        try
        {
            var dateList = new List<string>();
            if (!string.IsNullOrEmpty(reRunDate))
            {
                dateList.Add(reRunDate);
            }
            else
            {
                dateList.Add(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
                var dayDiff = DateTime.Today.AddDays(-3); // 抓最近三天內的匯率更新
                while (DateTime.Today >= dayDiff)
                {
                    dateList.Add(dayDiff.ToString("yyyy-MM-dd"));
                    dayDiff = dayDiff.AddDays(1);
                }
            }

            foreach (var date in dateList)
            {
                var postData = new DailyExchangeRateRequest()
                {
                    ExchangeDate = date
                };

                var parameters = JsonConvert.SerializeObject(postData);
                var httpResponse = await _httpHelper.HttpPostNoProxyAsync($"{_mpaExchangeRateConfig.ApiUrl}/DailyExchangeRate?access_token={accessToken}", null, parameters, "application/json");

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<List<DailyExchangeRateResponse>>(content);
                    response = response.Where(x => _includeCurrencyName.Contains(x.TargetCurrencyName)).ToList();

                    AddFakeCurrency(response);

                    response.ForEach(x =>
                    {
                        if (x.CurrencyId is 15 or 51)
                        {
                            x.TargetCurrencyRate /= 1000;
                            if (x.CurrencyId == x.TargetCurrencyId)
                            {
                                x.TargetCurrencyRate /= 1000;
                            }
                        }

                        x.RateDate = DateTime.Parse(date);
                    });

                    // insert to db
                    var tasks = response.Select(item => Task.Run(() => _mainDBDataLayer.InsertDailyExchangeRate(item))).ToList();
                    await Task.WhenAll(tasks);
                }
            }

            isSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $@"MpaUpdateExchangeRateService.SetDailyExchangeRate:{ex.Message}");
        }

        return isSuccess;
    }

    private record FakeCurrencyInfo(int CurrencyId, string CurrencyName);

    private static void AddFakeCurrency(List<DailyExchangeRateResponse> response)
    {
        // 以後要做假幣別匯率，KEY 是你想要 Copy 哪個幣別，FakeCurrencyInfo 是新幣別 ID 跟名稱
        var fakeCurrencyInfos = new Dictionary<int, FakeCurrencyInfo>()
        {
            { 3, new FakeCurrencyInfo(72, "KHUSD") } // USD COPY TO KHUSD
        };

        foreach (var currencyInfo in fakeCurrencyInfos)
        {
            foreach (var item in response.FindAll(y => y.TargetCurrencyId == currencyInfo.Key))
            {
                response.Add(new DailyExchangeRateResponse()
                {
                    CurrencyName = item.CurrencyName,
                    CurrencyId = item.CurrencyId,
                    TargetCurrencyName = currencyInfo.Value.CurrencyName,
                    TargetCurrencyRate = item.TargetCurrencyRate,
                });
            }

            foreach (var item in response.FindAll(y => y.CurrencyId == currencyInfo.Key))
            {
                response.Add(new DailyExchangeRateResponse()
                {
                    CurrencyName = currencyInfo.Value.CurrencyName,
                    CurrencyId = currencyInfo.Value.CurrencyId,
                    TargetCurrencyName = item.TargetCurrencyName,
                    TargetCurrencyRate = item.TargetCurrencyRate,
                });
            }
        }
    }

    #region Model

    private class AuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int Expires { get; set; }
    }

    public class DailyExchangeRateResponse
    {
        private int _ExchangeId { get; set; }

        /// <summary>
        /// 原幣別ID
        /// </summary>
        [JsonProperty("ExchangeId")]
        public int CurrencyId
        {
            get => _ExchangeId == 97 ? 74 : _ExchangeId; // OW 的BDT是97，我們的 DB 是 74，直接在這裡轉好寫回DB
            set => _ExchangeId = value;
        }

        /// <summary>
        /// 原幣別名稱
        /// </summary>
        [JsonProperty("ExchangeCode")]
        public string CurrencyName { get; set; }

        /// <summary>
        /// 目標幣別ID
        /// </summary>
        public int TargetCurrencyId
        {
            get
            {
                return TargetCurrencyName switch
                {
                    "MYR" => 2,
                    "USD" => 3,
                    "THB" => 4,
                    "CNY" => 13,
                    "IDR" => 15,
                    "JPY" => 32,
                    "KRW" => 45,
                    "VND" => 51,
                    "INR" => 61,
                    "KHUSD" => 72,
                    "BDT" => 74,
                    "BRL" => 82,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// 目標幣別名稱
        /// </summary>
        [JsonProperty("Currency")]
        public string TargetCurrencyName { get; set; }

        /// <summary>
        /// 目標幣別匯率
        /// </summary>
        [JsonProperty("ExchangeRate")]
        public decimal TargetCurrencyRate { get; set; }

        public DateTime RateDate { get; set; }
    }

    private class DailyExchangeRateRequest
    {
        /// <summary>
        ///2   MYR 马元	1:1
        ///3   USD 美元	1:1
        ///4   THB 泰铢	1:1
        ///6   EUR 欧元	1:1
        ///12  GBP 英镑	1:1
        ///13  RMB 人民币	1:1
        ///15  IDR 印度尼西亚盾	1:1000
        ///20  UUS 虚拟货币(测试环境使用)    -
        ///    32  JAP 日圆	1:1
        ///41  CHF 瑞士法郎	1:1
        ///42  PHP 菲律宾比索	1:1
        ///45  WON 韩元	1:1
        ///46  BND 文莱元	1:1
        ///49  MXN 墨西哥比绍	1:1
        ///50  CAN 加币	1:1
        ///51  INH(VND)    越南盾 1:1000
        ///52  DKK 丹麦克朗	1:1
        ///53  SEK 瑞典克朗	1:1
        ///54  NOK 挪威克朗	1:1
        ///55  RUB 俄罗斯卢布	1:1
        ///56  PLN 波兰兹罗提	1:1
        ///57  CZK 捷克克朗	1:1
        ///58  RON 罗马尼亚列伊	1:1
        ///61  INR 印度卢比	1:1
        ///70  MMK(MKK)    缅甸元 1:1000
        ///71  KHR 柬埔寨瑞尔	1:1000
        ///73  LIR 土耳其里拉	1:1
        ///79  KES 肯尼亚先令	1:1
        ///82  BRL 巴西雷亚尔	1:1
        ///90  AED 阿联酋迪拉姆	1:1
        ///93  LAK 老挝基普(寮国基普)  1:1000
        ///96  USDT USDT	1:1
        ///97  BDT 孟加拉塔卡	1:1
        ///121 PKR 巴基斯坦卢比	1:1
        ///122 KZT 哈萨克坦吉	1:1
        ///123 NPR 尼泊尔卢比	1:1
        ///124 RMB2 人民币(只支援沙巴白牌中国版) 1:1
        ///125 INH2(VND)   越南盾(只支援沙巴白牌中国版) 1:1000
        ///126 TB2 泰铢(只支援沙巴白牌中国版)  1:1
        ///127 INR2 印度卢比(只支援沙巴白牌中国版)    1:1
        ///128 PE2 菲律宾比索(只支援沙巴白牌中国版)   1:1
        ///129 IN2(IDR)    印度尼西亚盾(只支援沙巴白牌中国版)  1:1000
        ///130 Won2 韩元(只支援沙巴白牌中国版)  1:1
        ///131 RM2 马元(只支援沙巴白牌中国版)  1:1
        ///132 US$2    美元(只支援沙巴白牌中国版)  1:1
        ///133 TND 突尼斯第纳尔	1:1
        ///134 ZMW 赞比亚克瓦查	1:1
        ///135 TZS 坦桑尼亚先令	1:1
        /// </summary>
        public string ExchangeIds => "2,3,4,13,15,32,45,51,61,82,97"; // 未來擴充增加 Id 即可
        public string ExchangeDate { get; set; }
    }

    #endregion
}



