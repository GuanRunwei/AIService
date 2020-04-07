using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class NewsComment
    {
        #region 导航属性
        public virtual News News { get; set; }
        #endregion


        public long Id { get; set; } //新闻评论Id
        public long NewsId { get; set; }//新闻评论关联的新闻的Id
        public string Point { get; set; }//新闻评论观点
        public DateTime CommentTime { get; set; }//新闻评论时间
        public long UserId { get; set; }//新闻发此条评论的用户Id
        public string Commenter { get; set; }   //新闻发此条评论的用户的用户名
    }
}
