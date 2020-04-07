using AIService.Controllers;
using AIService.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class StockAccount
    {
        #region 导航属性
        public virtual User User { get; set; }
        public long UserId { get; set; }
        #endregion

        public long Id { get; set; }
        public double ValidMoney { get; set; }  //股票账户
        [DefaultValue(0)]
        public double SumMoney { get; set; }  //总资产
        [DefaultValue(0)]
        public double SumStockValue { get; set; }  //股票总市值
        [DefaultValue(0)]
        public double Profit_or_Loss { get; set; }  //盈亏
        [DefaultValue(0)]
        public long Rank { get; set; }  //资产排行

    }
}
