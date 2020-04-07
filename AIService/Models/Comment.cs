

namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class Comment
    {
        #region ��������
        public virtual Talk Talk { get; set; }
        #endregion


        public long Id { get; set; } //˵˵����Id
        public long TalkId { get; set; }//˵˵���۹�����˵˵��Id
        public string Point { get; set; }//˵˵���۹۵�
        public DateTime CommentTime { get; set; }//˵˵����ʱ��
        public long UserId { get; set; }//˵˵���������۵��û�Id
        public string Commenter { get; set; }   //˵˵���������۵��û����û���

    }
}
