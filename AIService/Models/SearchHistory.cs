
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class SearchHistory//��������¼Ŀǰ���������ܿͷ�������ʵ�
    {
        #region ��������
        public virtual User User { get; set; }
        #endregion
        public long Id { get; set; } //�û�������ʷ��¼Id
        public long UserId { get; set; } //�������û�Id
        public DateTime SearchTime { get; set; } //����ʱ��
        public string HistoricalText { get; set; } //�����ı�
        public string Answer { get; set; } //������
    
        
    }
}
