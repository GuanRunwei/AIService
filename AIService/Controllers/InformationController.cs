using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Helper.BaiduAPIHelper;
using AIService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AIService.Controllers
{
    [Route("api/information/[action]")]
    [ApiController]
    public class InformationController : Controller
    {
        #region 数据库连接
        private readonly DbEntity db = new DbEntity();
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        #region 加载Redis连接
        private readonly Lazy<RedisHelper> RedisHelper = new Lazy<RedisHelper>();
        #endregion

        #region 首页新闻List
        [HttpGet]
        public IActionResult GetMainPageNewsList(int Page)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            IList<News> news = db.News.Where(s => s.NewsType == Enums.NewsType.首页).OrderByDescending(s=>s.IssueTime).ToList();
            Console.WriteLine(news.Count);
            int page = (news.Count / 7) + (news.Count * 1.0 % 7 == 0 ? 0 : 1);
            if (Page > page)
                return Json(new 
                {
                    code = 400,
                    message = "没有更多数据了"
                });
            Dictionary<News, string[]> newsList = new Dictionary<News, string[]>();
            if ((news.Count - (Page - 1) * 7) >= 7)
            {
                for (int i = (Page - 1) * 7; i < Page * 7; i++)
                {
                    string NewsPraise_Key = "NewsId=" + news[i].Id + "&Praise";
                    string NewsTransmit_Key = "NewsId=" + news[i].Id + "&Transmit";
                    string NewsComment_Key = "NewsId=" + news[i].Id + "&CommentNumber";
                    string NewsRead_Key = "NewsId=" + news[i].Id + "&ReadNumber";
                    if (redisDatabase.KeyExists(NewsPraise_Key) == false)
                        redisDatabase.StringSetAsync(NewsPraise_Key, 0);
                    if (redisDatabase.KeyExists(NewsTransmit_Key) == false)
                        redisDatabase.StringSetAsync(NewsTransmit_Key, 0);
                    if (redisDatabase.KeyExists(NewsComment_Key) == false)
                        redisDatabase.StringSetAsync(NewsComment_Key, 0);
                    if (redisDatabase.KeyExists(NewsRead_Key) == false)
                        redisDatabase.StringSetAsync(NewsRead_Key, 0);
                    string praiseNumber = redisDatabase.StringGet(NewsPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(NewsTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(NewsComment_Key);
                    newsList.Add(news[i], new string[3] { praiseNumber, transmitNumber, commentNumber });
                }
            }
            else
            {
                for (int i = (Page - 1) * 7; i < news.Count; i++)
                {
                    string NewsPraise_Key = "NewsId=" + news[i].Id + "&Praise";
                    string NewsTransmit_Key = "NewsId=" + news[i].Id + "&Transmit";
                    string NewsComment_Key = "NewsId=" + news[i].Id + "&CommentNumber";
                    string NewsRead_Key = "NewsId=" + news[i].Id + "&ReadNumber";
                    if (redisDatabase.KeyExists(NewsPraise_Key) == false)
                        redisDatabase.StringSetAsync(NewsPraise_Key, 0);
                    if (redisDatabase.KeyExists(NewsTransmit_Key) == false)
                        redisDatabase.StringSetAsync(NewsTransmit_Key, 0);
                    if (redisDatabase.KeyExists(NewsComment_Key) == false)
                        redisDatabase.StringSetAsync(NewsComment_Key, 0);
                    if (redisDatabase.KeyExists(NewsRead_Key) == false)
                        redisDatabase.StringSetAsync(NewsRead_Key, 0);
                    string praiseNumber = redisDatabase.StringGet(NewsPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(NewsTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(NewsComment_Key);
                    newsList.Add(news[i], new string[3] { praiseNumber, transmitNumber, commentNumber });
                }
            }
                
            return Json(new
            {
                page,
                data = newsList.Select(s => new
                {
                    s.Key.Id,
                    s.Key.Title,
                    IssueTime = ParamHelper.TalkTimeConvert(s.Key.IssueTime),
                    s.Key.PicUrl1,
                    s.Key.Source,
                    PraiseNumber = s.Value[0],
                    TransmitNumber = s.Value[1],
                    CommentNumber = s.Value[2]
                })
            });

        }
        #endregion

        #region 新闻Detail
        [HttpGet]
        public IActionResult GetNewsDetail(long NewsId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            News news = db.News.FirstOrDefault(s => s.Id == NewsId);
            string TalkPraise_Key = "NewsId=" + news.Id + "&Praise";
            string TalkTransmit_Key = "NewsId=" + news.Id + "&Transmit";
            string TalkComment_Key = "NewsId=" + news.Id + "&CommentNumber";
            string NewsRead_Key = "NewsId=" + news.Id + "&ReadNumber";
            string News_User_Key = "NewsId=" + NewsId.ToString() + "&UserId=" + UserId.ToString();
            redisDatabase.StringIncrementAsync(NewsRead_Key, 1);

            string praiseNumber = redisDatabase.StringGet(TalkPraise_Key);
            string transmitNumber = redisDatabase.StringGet(TalkTransmit_Key);
            string commentNumber = redisDatabase.StringGet(TalkComment_Key);
            string readNumber = redisDatabase.StringGet(NewsRead_Key);
            string if_Praise = redisDatabase.KeyExists(News_User_Key).ToString();
            List<string> keywords = BaiduPlatform.GetKeyWords(Title: news.Title, Content: news.Content);
            List<NewsComment> newsComments = db.NewsComments.Where(s => s.NewsId == NewsId).OrderByDescending(s => s.CommentTime).ToList();
            return Json(new 
            {
                news.Id,
                news.Title,
                news.Content,
                news.Source,
                IssueTime = ParamHelper.TalkTimeConvert(news.IssueTime),
                news.PicUrl1,
                news.PicUrl2,
                news.PicUrl3,
                PraiseNumber = praiseNumber,
                TransmitNumber = transmitNumber,
                CommentNumber = commentNumber,
                ReadNumber = readNumber,
                If_Praise = if_Praise,
                NewsCommentsData = newsComments.Select(s=>new 
                {
                    s.Id,
                    Commenter = db.Users.FirstOrDefault(u=>u.Id==s.UserId).Username,
                    StockAge = "股龄" + db.Users.FirstOrDefault(t=>t.Id==s.UserId).StockAge,
                    CommentTime = ParamHelper.TalkTimeConvert(s.CommentTime),
                    s.Point,
                    Comment_If_Praise = redisDatabase.KeyExists("NewsCommentId=" + s.Id.ToString() + "&UserId=" + UserId.ToString()).ToString(),
                    Comment_PraiseNumber = redisDatabase.StringGet("NewsCommentId=" + s.Id.ToString() + "&PraiseNumber")
                }),
                KeywordsData = keywords
            });
        }
        #endregion

        #region 资讯List
        [HttpGet]
        public IActionResult GetKuaiXunList(int Page)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;            
            IList<News> news = db.News.Where(s => s.NewsType == Enums.NewsType.快讯).OrderByDescending(s => s.IssueTime).ToList();
            Console.WriteLine(news.Count);
            int page = (news.Count / 7) + (news.Count * 1.0 % 7 == 0 ? 0 : 1);
            if (Page > page)
                return Json(new
                {
                    code = 400,
                    message = "没有更多数据了"
                });
            Dictionary<News, string[]> newsList = new Dictionary<News, string[]>();
            if((news.Count- (Page - 1) * 7)>=7)
            {
                for (int i = (Page - 1) * 7; i < Page * 7; i++)
                {
                    string NewsPraise_Key = "NewsId=" + news[i].Id + "&Praise";
                    string NewsTransmit_Key = "NewsId=" + news[i].Id + "&Transmit";
                    string NewsComment_Key = "NewsId=" + news[i].Id + "&CommentNumber";
                    string NewsRead_Key = "NewsId=" + news[i].Id + "&ReadNumber";
                    if (redisDatabase.KeyExists(NewsPraise_Key) == false)
                        redisDatabase.StringSet(NewsPraise_Key, 0);
                    if (redisDatabase.KeyExists(NewsTransmit_Key) == false)
                        redisDatabase.StringSet(NewsTransmit_Key, 0);
                    if (redisDatabase.KeyExists(NewsComment_Key) == false)
                        redisDatabase.StringSet(NewsComment_Key, 0);
                    if (redisDatabase.KeyExists(NewsRead_Key) == false)
                        redisDatabase.StringSet(NewsRead_Key, 0);
                    string praiseNumber = redisDatabase.StringGet(NewsPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(NewsTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(NewsComment_Key);
                    newsList.Add(news[i], new string[3] { praiseNumber, transmitNumber, commentNumber });
                }
            }
            else
            {
                for (int i = (Page - 1) * 7; i < news.Count; i++)
                {
                    string NewsPraise_Key = "NewsId=" + news[i].Id + "&Praise";
                    string NewsTransmit_Key = "NewsId=" + news[i].Id + "&Transmit";
                    string NewsComment_Key = "NewsId=" + news[i].Id + "&CommentNumber";
                    string NewsRead_Key = "NewsId=" + news[i].Id + "&ReadNumber";
                    if (redisDatabase.KeyExists(NewsPraise_Key) == false)
                        redisDatabase.StringSet(NewsPraise_Key, 0);
                    if (redisDatabase.KeyExists(NewsTransmit_Key) == false)
                        redisDatabase.StringSet(NewsTransmit_Key, 0);
                    if (redisDatabase.KeyExists(NewsComment_Key) == false)
                        redisDatabase.StringSet(NewsComment_Key, 0);
                    if (redisDatabase.KeyExists(NewsRead_Key) == false)
                        redisDatabase.StringSet(NewsRead_Key, 0);
                    string praiseNumber = redisDatabase.StringGet(NewsPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(NewsTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(NewsComment_Key);
                    newsList.Add(news[i], new string[3] { praiseNumber, transmitNumber, commentNumber });
                }
            }
            
            return Json(new
            {
                code = 200,
                page,
                data = newsList.Select(s => new
                {
                    s.Key.Id,
                    s.Key.Title,
                    Content = s.Key.Content.Replace("\n", "").Replace("\r","").Length>120? s.Key.Content.Replace("\n", "").Replace("\r", "").Substring(0, 120) + "……": s.Key.Content.Replace("\n", "").Replace("\r", "")+"……",
                    IssueTime = ParamHelper.TalkTimeConvert(s.Key.IssueTime),
                    s.Key.Source,
                })
            });
        }
        #endregion

        #region 新闻评论
        [HttpPost]
        public HttpResponseMessage SendNewsComment(long NewsId, long UserId, string Point)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string NewsComment_Key = "NewsId=" + NewsId.ToString() + "&CommentNumber";    
            NewsComment newsComment = new NewsComment();
            User commenter = db.Users.FirstOrDefault(s => s.Id == UserId);
            try
            {
                newsComment.News = db.News.FirstOrDefault(s => s.Id == NewsId);
                newsComment.NewsId = NewsId;
                newsComment.UserId = UserId;
                newsComment.Point = Point;
                newsComment.Commenter = commenter.Username;
                newsComment.CommentTime = DateTime.Now;
                db.NewsComments.Add(newsComment);
                db.SaveChanges();               
                redisDatabase.StringIncrement(NewsComment_Key, 1);
                string NewsComment_Praise_Key = "NewsCommentId=" + newsComment.Id.ToString() + "&PraiseNumber";
                redisDatabase.StringSet(NewsComment_Praise_Key, 0);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("评论失败！");
            }
            return ApiResponse.Ok(new 
            { 
                newsComment.Id,
                newsComment.NewsId,
                newsComment.UserId,
                NewsCommentTime = ParamHelper.TalkTimeConvert(newsComment.CommentTime),
                newsComment.Point,
                newsComment.Commenter,
                StockAge = "股龄" + commenter.StockAge,
                commenter.ImageUrl
            });
            
        }
        #endregion

        #region 删除新闻评论
        [HttpPost]
        public HttpResponseMessage DeleteNewsComment(long NewsCommentId,long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            NewsComment newsComment = db.NewsComments.FirstOrDefault(s => s.Id == NewsCommentId && s.UserId==UserId);
            string NewsComment_Key = "NewsId=" + newsComment.NewsId.ToString() + "&CommentNumber";
            string NewsComment_Praise_Key = "NewsCommentId=" + newsComment.Id.ToString() + "&PraiseNumber";
            try
            {
                redisDatabase.KeyDelete(NewsComment_Key);
                redisDatabase.KeyDelete(NewsComment_Praise_Key);
                db.NewsComments.Remove(newsComment);
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok("删除成功");

        }
        #endregion

        #region 新闻点赞
        [HttpPost]
        public HttpResponseMessage SendNewsPraise(long NewsId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string News_User_Key = "NewsId=" + NewsId.ToString() + "&UserId=" + UserId.ToString();
            string NewsPraise_Key = "NewsId=" + NewsId + "&Praise";
            if (redisDatabase.KeyExists(News_User_Key))
                return ApiResponse.BadRequest("您已经点过赞了！");
            redisDatabase.StringIncrement(News_User_Key, 1);
            redisDatabase.StringIncrement(NewsPraise_Key, 1);
            return ApiResponse.Ok("点赞成功！");
        }
        #endregion

        #region 新闻取消点赞
        [HttpPost]
        public HttpResponseMessage CancelPraise(long NewsId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string News_User_Key = "NewsId=" + NewsId.ToString() + "&UserId=" + UserId.ToString();
            string NewsPraise_Key = "NewsId=" + NewsId + "&Praise";
            try
            {
                redisDatabase.KeyDelete(News_User_Key);
                redisDatabase.StringDecrement(NewsPraise_Key, 1);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("取消失败！");
            }
            return ApiResponse.Ok("取消成功！");
        }
        #endregion

        #region 新闻评论点赞
        [HttpPost]
        public HttpResponseMessage SendNewsCommentPraise(long NewsCommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string NewsComment_User_Key = "NewsCommentId=" + NewsCommentId.ToString() + "&UserId=" + UserId.ToString();
            string NewsComment_Praise_Key = "NewsCommentId=" + NewsCommentId.ToString() + "&PraiseNumber";
            if (redisDatabase.KeyExists(NewsComment_User_Key))
                return ApiResponse.BadRequest("您已经点过赞了！");
            redisDatabase.StringSet(NewsComment_User_Key, 1);
            redisDatabase.StringIncrement(NewsComment_Praise_Key, 1);
            return ApiResponse.Ok("点赞成功!");
        }
        #endregion

        #region 新闻评论取消点赞
        [HttpPost]
        public HttpResponseMessage CancelNewsCommentPraise(long NewsCommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string NewsComment_User_Key = "NewsCommentId=" + NewsCommentId.ToString() + "&UserId=" + UserId.ToString();
            string NewsComment_Praise_Key = "NewsCommentId=" + NewsCommentId.ToString() + "&PraiseNumber";
            if(redisDatabase.KeyExists(NewsComment_User_Key))
            {
                redisDatabase.KeyDelete(NewsComment_User_Key);
                redisDatabase.StringDecrement(NewsComment_Praise_Key, 1);
                return ApiResponse.Ok("取消点赞成功!");
            }
            return ApiResponse.Ok("您已取消过了!");

        }
        #endregion

    }
}