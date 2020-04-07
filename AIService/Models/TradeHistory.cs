using AIService.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class TradeHistory  //模拟交易模块里的交易历史
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }  //关联一个股票账户
        public long StockAccountId { get; set; }
        #endregion
        public long Id { get; set; }
        public string StockName { get; set; }  //股票名称
        public string StockCode { get; set; }  //股票代码
        public double TransactionValue { get; set; }  //交易金额
        public TransactionType TransactionType { get; set; }  //交易类型（0为买入，1为卖出）
        public double TransactionPrice { get; set; }  //该只股票交易价格
        public int TransactionAmount { get; set; }  //交易数量
        public DateTime TradeTime { get; set; }  //交易时间
    }
}
