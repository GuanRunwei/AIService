
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class Knowledge
    {
        public long Id { get; set; } //֪ʶ��֪ʶId
        public string Question { get; set; } //��׼����
        public string Answer { get; set; } //��׼��
        public string PossibleQuestion { get; set; } //�û��������ʣ����ֶ��������ã�
    }
}
