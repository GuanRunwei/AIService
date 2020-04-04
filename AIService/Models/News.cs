
namespace AIService.Models
{
    using AIService.Enums;
    using System;
    using System.Collections.Generic;
    
    public class News
    {
        public News()
        {
            this.NewsComments = new List<NewsComment>();
        }

        #region ��������
        public virtual List<NewsComment> NewsComments { get; set; }
        #endregion

        public long Id { get; set; } //����Id
        public string Title { get; set; } //���ű���
        public string Content { get; set; } //��������
        public DateTime IssueTime { get; set; } //���ŷ���ʱ��
        public string PicUrl1 { get; set; } //ͼƬ1
        public string PicUrl2 { get; set; } //ͼƬ2
        public string PicUrl3 { get; set; } //ͼƬ3
        public string Source { get; set; } //��Դ
        public NewsType NewsType { get; set; } //��������
        //���ŵ���������Redis�洢
    }
}
