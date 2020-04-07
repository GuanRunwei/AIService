
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class SearchHistory//��������¼Ŀǰ���������ܿͷ�������ʵ�
    {
        #region ��������
        public virtual User User { get; set; }
        public virtual Knowledge Knowledge { get; set; }
        #endregion
        public long Id { get; set; } //���ܿͷ��û�������ʷ��¼Id
        public long UserId { get; set; } //�������û�Id
        public long KnowledgeId { get; set; }  //֪ʶId
        public DateTime SearchTime { get; set; } //����ʱ��
        public string HistoricalText { get; set; } //���ܿͷ������ı�
        public string Answer { get; set; } //���ܿͷ���
    
        
    }
}
