using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class Feedback
    {
        #region 导航属性
        public virtual User User { get; set; }
        #endregion

        public long Id { get; set; }//用户反馈Id
        public long UserId { get; set; } //反馈关联的用户Id
        public string Content { get; set; } //反馈内容
        public DateTime FeedbackTime { get; set; } //反馈时间
    }
}
