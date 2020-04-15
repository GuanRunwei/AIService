
namespace AIService.Models
{
    using AIService.Enums;
    using System;
    using System.Collections.Generic;

    public class Talk
    {
        public Talk()
        {
            this.Comments = new List<Comment>();
            this.Pictures = new List<Picture>();
        }
        #region ��������
        public List<Comment> Comments { get; set; }
        public List<Picture> Pictures { get; set; }
        public User User { get; set; }
        #endregion
         
        public long Id { get; set; } //˵˵Id

        public long UserId { get; set; } //��˵˵���û�Id
        public string Content { get; set; } //˵˵����Id
        public DateTime TalkTime { get; set; } //��˵˵��ʱ��
        public TalkType TalkType { get; set; } //˵˵���ͣ�0��ԭ����1��ת�أ�
        //˵˵����������ת��������Redis�洢
        
    }
}
