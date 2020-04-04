using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class StockComment
    {
        #region 导航属性
        public virtual Stock Stock { get; set; }
        public long StockId { get; set; }

        public virtual User User { get; set; }
        public long UserId { get; set; }
        #endregion

        public long Id { get; set; }
        public string Point { get; set; }//评论观点
        public DateTime CommentTime { get; set; }//评论时间
    }
}
