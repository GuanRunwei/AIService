using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class Question
    {
        #region 导航属性
        public virtual User User { get; set; }
        public long UserId { get; set; }
        #endregion

        public int Id { get; set; }  //提问Id
        [DefaultValue(0)]
        public int AnswerId { get; set; }   //回答的Id
        public string QuestionContent { get; set; }  //提问内容
        public DateTime QuestionTime { get; set; }  //提问时间

    }
}
