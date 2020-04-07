using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class Answer
    {
        //问答模块
        #region 导航属性
        public virtual User User { get; set; } //关联一个用户
        public long UserId { get; set; }
        #endregion
        public int Id { get; set; }
        public string AnswerContent { get; set; }  //回答内容
        public DateTime AnswerTime { get; set; }  //回答时间
    }
}
