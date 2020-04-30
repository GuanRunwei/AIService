using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class StockSearch
    {
        #region 导航属性
        public virtual User User { get; set; }
        #endregion
        public long Id { get; set; }
        public long UserId { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public DateTime SearchTime { get; set; }
        public int Status { get; set; }
    }
}
