using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class SellStock  //模拟炒股模块中的卖出股票
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }  //关联一个股票账户
        public long StockAccountId { get; set; }
        #endregion

        public long Id { get; set; }  //卖出股票的Id
        public string StockCode { get; set; }  //股票代码
        public string StockName { get; set; }  //股票名称
        public double BuyPrice { get; set; }   //购买价格
        public double SellPrice { get; set; }  //卖出价格
        public int SellStockNumber { get; set; }  //卖出数量
        public DateTime SellTime { get; set; }  //卖出时间
    }
}
