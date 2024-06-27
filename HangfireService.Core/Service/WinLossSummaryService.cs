using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.WinLossSummary;

namespace HangfireService.Core.Service;

public class WinLossSummaryService
{
    private readonly ReportBackupDBDataLayer _reportBackupDBDataLayer;
    private readonly ICacheHelper _redisHelper;

    public WinLossSummaryService(IServiceProvider provider)
    {
        _reportBackupDBDataLayer = provider.GetRequiredService<ReportBackupDBDataLayer>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }

    #region Main Task

    public IEnumerable<WinLossSummaryModel> ProcessData(IEnumerable<WinLossSourceModel> source, DateTime startTime, DateTime endTime)
    {
        var result = SetBAData(source);
        SetCommissionByNineW(result, startTime, endTime);
        SetGgr(result);
        CalculateCommission(result);

        return result;
    }

    #endregion

    #region Logic Process

    // ba
    // commonsion
    // ggr
    //special poker game(9w,v8...)


    // BA用戶是指saba後台設定的特殊用戶，這些用戶的輸贏直接歸沙巴，沙巴會給平台傭金
    public IEnumerable<WinLossSummaryModel> SetBAData(IEnumerable<WinLossSourceModel> source)
    {
        var result = new List<WinLossSummaryModel>();

        // BA只計算SBSW Sport
        foreach (var item in source)
        {
            if (item.GameType != (int)MpaCommissionCustomizeGameTypeEnum.SBSW_Sports)
            {
                // 把BA資料區隔出來，避免影響一般資料計算
                item.BaStake = 0;
                item.BaValidBet = 0;
                item.BaRevenue = 0;
                item.BaWinLoss = 0;
                item.BaProgressiveBet = 0;
                item.BaProgressiveWin = 0;
            }
            decimal baCommission = 0;
            if (item.GameType == (int)MpaCommissionCustomizeGameTypeEnum.SBSW_Sports && item.BaValidBet != 0)
            {
                baCommission = item.BaValidBet * 0.0025m;  // BACommission = ValidTurnOver * 0.25%
            }
            result.Add(new WinLossSummaryModel(item, baCommission));
        }

        return result;
    }

    // 用戶的輸贏直接歸9W，9W會給平台傭金
    public void SetCommissionByNineW(IEnumerable<WinLossSummaryModel> source, DateTime startTime, DateTime endTime)
    {
        var nineWCommissionData = _reportBackupDBDataLayer.GetWinLoss9WCommissionSourceData(startTime, endTime);

        foreach (var item in nineWCommissionData)
        {
            // 用commission抓第一筆符合9w/date/custId的資料，塞入傭金金額
            var matchRecord = source.FirstOrDefault(r =>
                r.TpId == (int)MpaCommissionCustomizeTpIdEnum.NineW
                && (r.TransTime == item.TransTime || r.SettledTime == item.SettledTime)
                && r.CustId == item.CustId);
            if (matchRecord != null)
            {
                matchRecord.Revenue = item.Commission;
            }
        }
    }

    public void SetGgr(IEnumerable<WinLossSummaryModel> source)
    {
        foreach (var item in source)
        {
            item.Ggr = CalculateGgr(item.GameType, (item.WinLoss - item.BaWinLoss), item.ProgressiveWin, item.ProgressiveBet, item.Revenue) + item.BaCommission;
        }
    }


    private decimal CalculateGgr(int gameType, decimal winLossSummary, decimal progressiveWin, decimal progressiveBet, decimal revenue)
    {
        switch (gameType)
        {
            case (int)MpaGgrCustomizeGameTypeEnum.NineW:
                //GGR for 9W = -PlayerWinLoss + Commission
                var nineWCommission = revenue;
                return GetGgr(winLossSummary, progressiveWin, progressiveBet) + nineWCommission;

            case (int)MpaGgrCustomizeGameTypeEnum.OneGamingPoker:
            case (int)MpaGgrCustomizeGameTypeEnum.V8Poker:
                //若為 Poker，commission: GGR = sum(revenue)
                winLossSummary = revenue * -1; // 因為GetGgr內計算會 * -1所以乘回來
                return GetGgr(winLossSummary, progressiveWin, progressiveBet);

            default:
                return GetGgr(winLossSummary, progressiveWin, progressiveBet);
        }
    }

    private decimal GetGgr(decimal winLossSummary, decimal progressiveWinSummary, decimal progressiveBetSummary)
    {
        return winLossSummary * -1 + (progressiveWinSummary - progressiveBetSummary);
    }

    private void CalculateCommission(IEnumerable<WinLossSummaryModel> source)
    {
        //目前除9W外，其他commission皆為0
        foreach (var s in source)
        {
            if (s.TpId == (int)MpaCommissionCustomizeTpIdEnum.NineW)
            {
                s.Revenue *= -1;
            }
            else
            {
                s.Revenue = 0;
            }
        }
    }

    #endregion
}
