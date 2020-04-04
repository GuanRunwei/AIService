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
        public double ValidMoney { get; set; }
        [DefaultValue(0)]
        public double SumMoney { get; set; }
        [DefaultValue(0)]
        public double SumStockValue { get; set; }
        [DefaultValue(0)]
        public double Profit_or_Loss { get; set; }
        [DefaultValue(0)]
        public long Rank { get; set; }

    }
}
