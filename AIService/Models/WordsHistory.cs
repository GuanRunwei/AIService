
namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class WordsHistory
    {
        #region ��������
        public virtual User User { get; set; }
        #endregion

        public long Id { get; set; } //����ʵ�������ʷId
        public long UserId { get; set; } //�������û���Id
        public DateTime SearchTime { get; set; } //����ʱ��
        public string Word { get; set; } //�����Ĵ���
        public string Explain { get; set; } //�������
    
        
    }
}
