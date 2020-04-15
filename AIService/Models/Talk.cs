
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
        #region 导航属性
        public List<Comment> Comments { get; set; }
        public List<Picture> Pictures { get; set; }
        public User User { get; set; }
        #endregion
         
        public long Id { get; set; } //说说Id

        public long UserId { get; set; } //发说说的用户Id
        public string Content { get; set; } //说说内容Id
        public DateTime TalkTime { get; set; } //发说说的时间
        public TalkType TalkType { get; set; } //说说类型（0是原创，1是转载）
        //说说点赞数量、转发数量用Redis存储
        
    }
}
