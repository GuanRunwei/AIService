

namespace AIService.Models
{
    using System;
    using System.Collections.Generic;
    
    public class Comment
    {
        #region 导航属性
        public virtual Talk Talk { get; set; }
        #endregion


        public long Id { get; set; } //评论Id
        public long TalkId { get; set; }//评论关联的说说的Id
        public string Point { get; set; }//评论观点
        public DateTime CommentTime { get; set; }//评论时间
        public long UserId { get; set; }//发此条评论的用户Id
        public string Commenter { get; set; }   //发此条评论的用户的用户名
        
    }
}
