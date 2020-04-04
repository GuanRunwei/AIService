
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class WordsHistory
    {
        #region 导航属性
        public virtual User User { get; set; }
        #endregion

        public long Id { get; set; } //术语词典搜索历史Id
        public long UserId { get; set; } //搜索的用户的Id
        public DateTime SearchTime { get; set; } //搜索时间
        public string Word { get; set; } //搜索的词语
        public string Explain { get; set; } //词语解释
    
        
    }
}
