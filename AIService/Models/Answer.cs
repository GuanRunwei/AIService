using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class Answer
    {
        #region 导航属性
        public virtual User User { get; set; }
        public long UserId { get; set; }
        #endregion
        public int Id { get; set; }
        public string AnswerContent { get; set; }
        public DateTime AnswerTime { get; set; }
    }
}
