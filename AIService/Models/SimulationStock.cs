using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class SimulationStock
    {
        #region 导航属性
        public virtual StockAccount StockAccount { get; set; }
        public long StockAccountId { get; set; }
        #endregion

        public long Id { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public double BuyPrice { get; set; }
        public double NowPrice { get; set; }
        public int StockNumber { get; set; }
        [DefaultValue(true)]
        public bool Valid { get; set; }
        public DateTime BuyTime { get; set; }
    }
}
