using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class SellStock
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }
        public long StockAccountId { get; set; }
        #endregion

        public long Id { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public double BuyPrice { get; set; }
        public double SellPrice { get; set; }
        public int SellStockNumber { get; set; }
        public DateTime SellTime { get; set; }
    }
}
