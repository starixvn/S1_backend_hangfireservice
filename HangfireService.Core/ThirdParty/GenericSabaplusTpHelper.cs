using System;
using HangfireService.Core.Attributes;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Enums;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.ThirdParty;

public class GenericSabaplusTpHelper : BaseTpHelper, IFinanceTpHelper
{
    private readonly static Dictionary<int, TpSpNames> financeTpSpNames = new()
    {
        [(int)Tp.PP] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_PP_Summary_Get"
        },
        [(int)Tp.PG] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_PG_Summary_Get"
        },
        [(int)Tp.FaChai] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_FaChai_Summary_Get"
        },
        [(int)Tp.JILI] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_JILI_Summary_Get"
        },
        [(int)Tp.RNGKeno] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_Keno_Summary_Get"
        },
        [(int)Tp.TableGame] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_TableGame_Summary_Get"
        },
        [(int)Tp.MaxGame] = new TpSpNames {
            BettransSummaryGet = "Finance_BettransRng_MaxGame_Summary_Get"
        },
    };

    public bool CheckTransId => false;
    public bool BindCommonCurrency => false;
    public TimeOption TimeOptionId => TimeOption.SettledTime;

    int IFinanceTpHelper.TpId => TpId;
    public int TpId;
    private readonly PartitionService _partitionService;
    private readonly FincanceService _financeService;

    public GenericSabaplusTpHelper(IServiceProvider provider, int tpId) : base(provider)
    {
        _partitionService = provider.GetRequiredService<PartitionService>();
        _financeService = provider.GetRequiredService<FincanceService>();
        TpId = tpId;

    }

    public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
    {
        var beginTime = checkOption.BeginTime.GetUnixTimeByDay();
        var endTime = checkOption.EndTime.GetUnixTimeByDay() - 1;
        var getBettransSumarySpName = GetSpNames(TpId)?.BettransSummaryGet
            ?? throw new NotImplementedException($"SP GetBettransSummary for tp {TpId} not implemented");
        var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
        tasks.AddRange(
            _partitionService
            .QueryAsync<BettransCheckData>(
                checkOption.BeginTime, 
                checkOption.EndTime, 
                getBettransSumarySpName,
                new {
                    _BeginTime = beginTime,
                    _EndTime = endTime
                })
            );
        try {
            await Task.WhenAll(tasks);
        } catch (Exception ex) {
            _logger.LogError($"Error on calling SP {getBettransSumarySpName}");
            throw ex;
        }

        var checkData = new List<BettransCheckData>();
        foreach (var task in tasks) {
            var data = task.Result;
            checkData.AddRange(data);
        }
        
        var bettransCheckData = checkData
            .GroupBy(item => item.CurrencyId)
            .Select(item => new BettransCheckData{
                TpId = TpId,
                CurrencyId = item.Key,
                Stake = item.Sum(i => i.Stake),
                ValidBet = item.Sum(i => i.ValidBet),
                Payout = item.Sum(i => i.Payout),
                WinlostAmount = item.Sum(i => i.WinlostAmount),
                Revenue = 0,
                ProgressiveBet = 0,
                ProgressiveWin = 0
            });
            
        return bettransCheckData;
    }

    public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
    {
        return await _financeService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
    }

    private TpSpNames GetSpNames(int tpId) {
        if (!financeTpSpNames.TryGetValue(tpId, out var tpSpNames)) return null;
        return tpSpNames;
    }
}

public class TpSpNames {
    public string BettransSummaryGet { get; set;}
}