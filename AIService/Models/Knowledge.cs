
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class Knowledge
    {
        public long Id { get; set; } //知识库知识Id
        public string Question { get; set; } //标准提问
        public string Answer { get; set; } //标准答案
        public string PossibleQuestion { get; set; } //用户可能提问（此字段用作备用）
    }
}
