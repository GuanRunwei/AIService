using AIService.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class TradeHistory
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }
        public long StockAccountId { get; set; }
        #endregion
        public long Id { get; set; }
        public string StockName { get; set; }
        public string StockCode { get; set; }
        public double TransactionValue { get; set; }
        public TransactionType TransactionType { get; set; }
        public double TransactionPrice { get; set; }
        public int TransactionAmount { get; set; }
        public DateTime TradeTime { get; set; }
    }
}
