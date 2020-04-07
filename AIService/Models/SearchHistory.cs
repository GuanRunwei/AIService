
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class SearchHistory//此搜索记录目前仅用作智能客服和术语词典
    {
        #region 导航属性
        public virtual User User { get; set; }
        public virtual Knowledge Knowledge { get; set; }
        #endregion
        public long Id { get; set; } //智能客服用户搜索历史记录Id
        public long UserId { get; set; } //关联的用户Id
        public long KnowledgeId { get; set; }  //知识Id
        public DateTime SearchTime { get; set; } //搜索时间
        public string HistoricalText { get; set; } //智能客服搜索文本
        public string Answer { get; set; } //智能客服答案
    
        
    }
}
