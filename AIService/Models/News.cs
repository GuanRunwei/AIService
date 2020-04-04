
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

        #region 导航属性
        public virtual List<NewsComment> NewsComments { get; set; }
        #endregion

        public long Id { get; set; } //新闻Id
        public string Title { get; set; } //新闻标题
        public string Content { get; set; } //新闻内容
        public DateTime IssueTime { get; set; } //新闻发布时间
        public string PicUrl1 { get; set; } //图片1
        public string PicUrl2 { get; set; } //图片2
        public string PicUrl3 { get; set; } //图片3
        public string Source { get; set; } //来源
        public NewsType NewsType { get; set; } //新闻类型
        //新闻点赞数量用Redis存储
    }
}
