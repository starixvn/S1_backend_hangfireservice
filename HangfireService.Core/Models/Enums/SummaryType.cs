using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.Enums
{
    public enum SummaryType
    {
        TpBetRecord = 1,//下注紀錄
        TpRewardTotalAmount = 2,//活動總金額
        TpRewardFreeGame = 3,//活動免費遊戲
        TpRewardCash = 4,//活動現金獎勵
    }
}
