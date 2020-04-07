using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class SimulationStock  //模拟炒股中持有的股票
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }  //关联的一个股票账户
        public long StockAccountId { get; set; }
        #endregion

        public long Id { get; set; }
        public string StockCode { get; set; }  //股票代码
        public string StockName { get; set; }  //股票名称
        public double BuyPrice { get; set; }  //购买价格
        public double NowPrice { get; set; }  //最新价格
        public int StockNumber { get; set; }  //持有数量
        [DefaultValue(true)]
        public bool Valid { get; set; } //是否有效（持有是有效，卖光为无效）
        public DateTime BuyTime { get; set; }  //购买时间
    }
}
