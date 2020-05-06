using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Helper.BaiduAPIHelper;
using AIService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                    UserId = s.UserId,
                    Commenter = db.Users.FirstOrDefault(u=>u.Id==s.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(u => u.Id == s.UserId).ImageUrl,
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

        #region 新闻评论按热度排序
        [HttpGet]
        public IActionResult GetHotNewsComments(long NewsId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            List<NewsComment> newsComments = db.NewsComments.Where(s => s.NewsId == NewsId).ToList();
            Dictionary<NewsComment, string> result = new Dictionary<NewsComment, string>();
            foreach(var item in newsComments)
            {
                string Comment_PraiseNumber = redisDatabase.StringGet("NewsCommentId=" + item.Id.ToString() + "&PraiseNumber");
                result.Add(item, Comment_PraiseNumber);
            }
            return Json(new 
            {
                code = 200,
                data = result.OrderByDescending(s=>s.Value).OrderByDescending(s=>s.Key.CommentTime).Select(s=>new 
                { 
                    s.Key.Id,
                    s.Key.UserId,
                    Commenter = db.Users.FirstOrDefault(u => u.Id == s.Key.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(u => u.Id == s.Key.UserId).ImageUrl,
                    StockAge = "股龄" + db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).StockAge,
                    CommentTime = ParamHelper.TalkTimeConvert(s.Key.CommentTime),
                    s.Key.Point,
                    Comment_If_Praise = redisDatabase.KeyExists("NewsCommentId=" + s.Key.Id.ToString() + "&UserId=" + UserId.ToString()).ToString(),
                    Comment_PraiseNumber = redisDatabase.StringGet("NewsCommentId=" + s.Key.Id.ToString() + "&PraiseNumber")
                })
            });
        }
        #endregion

        #region 新闻评论按热度排序
        [HttpGet]
        public IActionResult GetTimeNewsComments(long NewsId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            List<NewsComment> newsComments = db.NewsComments.Where(s => s.NewsId == NewsId).ToList();
            Dictionary<NewsComment, string> result = new Dictionary<NewsComment, string>();
            foreach (var item in newsComments)
            {
                string Comment_PraiseNumber = redisDatabase.StringGet("NewsCommentId=" + item.Id.ToString() + "&PraiseNumber");
                result.Add(item, Comment_PraiseNumber);
            }
            return Json(new
            {
                code = 200,
                data = result.OrderByDescending(s => s.Key.CommentTime).Select(s => new
                {
                    s.Key.Id,
                    s.Key.UserId,
                    Commenter = db.Users.FirstOrDefault(u => u.Id == s.Key.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(u => u.Id == s.Key.UserId).ImageUrl,
                    StockAge = "股龄" + db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).StockAge,
                    CommentTime = ParamHelper.TalkTimeConvert(s.Key.CommentTime),
                    s.Key.Point,
                    Comment_If_Praise = redisDatabase.KeyExists("NewsCommentId=" + s.Key.Id.ToString() + "&UserId=" + UserId.ToString()).ToString(),
                    Comment_PraiseNumber = redisDatabase.StringGet("NewsCommentId=" + s.Key.Id.ToString() + "&PraiseNumber")
                })
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

        #region 新闻搜索初始化界面
        [HttpGet]
        public IActionResult GetStartSearchNews()
        {
            int[] number_result = new int[3];
            Random random = new Random();
            IList<News> news = db.News.Where(s => s.NewsType == Enums.NewsType.首页).ToList();
            Dictionary<long, News> result = new Dictionary<long, News>();
            List<News> final_result = new List<News>();
            int count = 0;
            for (int i = 1; i < news.Count; i++)
            {
                result.Add(i, news[i]);
            }
            Console.WriteLine(result.Count);
            while (number_result[2] == 0)
            {
                int i = random.Next(1, result.Count-1);
                if (!number_result.Contains(i))
                {
                    number_result[count] = i;
                    count++;
                }
            }
            for (int i = 0; i < number_result.Length; i++)
            {
                final_result.Add(result[number_result[i]]);
            }
            return Json(new
            {
                code = 200,
                data = final_result.Select(s => new { s.Id, Title = s.Title.Length > 20 ? (s.Title.Substring(0, 20) + "…") : s.Title })
            });
        }
        #endregion

        #region 新闻搜索结果
        [HttpGet]
        public IActionResult SearchNews(string SearchText)
        {
            if (SearchText.Trim().Length == 0||SearchText==null||SearchText==""||SearchText.Equals(""))
                return Json(new
                {
                    code = 200,
                    data = new string[] { }
                });
            List<News> news = new List<News>();
            if(SearchText.Trim().Length < 5)
                news = db.News.Where(s => s.NewsType == Enums.NewsType.首页 && (s.Title.ToLower() == SearchText.ToLower() || s.Title.ToLower().Contains(SearchText) || s.Content.ToLower().Contains(SearchText) || GetSimilarity(s.Title, SearchText) > 0.6)).OrderByDescending(s => s.IssueTime).ToList();
            else
            {
                JObject result = null;
                string cut_sentence;
                string[] words_array;
                try
                {
                    string url = "http://116.62.208.165/api/cut_sentence?Sentence=" + SearchText;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream myResponseStream = response.GetResponseStream();
                    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                    string retString = myStreamReader.ReadToEnd();
                    result = JsonConvert.DeserializeObject<JObject>(retString);
                    cut_sentence = result["data"].ToString().Trim();
                    words_array = cut_sentence.Split(" ").ToArray();
                    foreach(var item in db.News.Where(s=>s.NewsType==Enums.NewsType.首页).ToList())
                    {
                        for(int i=0;i<words_array.Length;i++)
                        {
                            if (item.Title.ToLower().Contains(words_array[i].Trim()) || item.Content.ToLower().Contains(words_array[i].Trim()))
                            {
                                news.Add(item);
                                break;
                            }
                        }
                    }
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    return Json(new
                    {
                        code = 400,
                        data = "糟糕，网络好像出问题了"
                    }
                    );
                }
            }
            
            return Json(new
            {
                code = 200,
                data = news.Select(s => new
                {
                    s.Id,
                    Title = s.Title.Length > 18 ? s.Title.Substring(0, 18) + "…" : s.Title,
                    Content = s.Content.Length > 40 ? s.Content.Substring(0, 40) + "…":s.Content,
                    s.Source,
                    IssueTime = ParamHelper.TalkTimeConvert(s.IssueTime)
                })
            });
        }
        #endregion

        #region 编辑距离算法文本相似度匹配
        public static double GetSimilarity(String doc1, String doc2)
        {
            if (doc1 != null && doc1.Trim().Length > 0 && doc2 != null
                    && doc2.Trim().Length > 0)
            {
                Dictionary<int, int[]> AlgorithmMap = new Dictionary<int, int[]>();
                //将两个字符串中的中文字符以及出现的总数封装到，AlgorithmMap中
                for (int i = 0; i < doc1.Length; i++)
                {
                    char d1 = doc1.ToCharArray()[i];
                    if (IsHanZi(d1))
                    {
                        int charIndex = GetGB2312Id(d1);
                        if (charIndex != -1)
                        {
                            int[] fq = null;
                            try
                            {
                                fq = AlgorithmMap[charIndex];
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                if (fq != null && fq.Length == 2)
                                {
                                    fq[0]++;
                                }
                                else
                                {
                                    fq = new int[2];
                                    fq[0] = 1;
                                    fq[1] = 0;
                                    AlgorithmMap.Add(charIndex, fq);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < doc2.Length; i++)
                {
                    char d2 = doc2.ToCharArray()[i];
                    if (IsHanZi(d2))
                    {
                        int charIndex = GetGB2312Id(d2);
                        if (charIndex != -1)
                        {
                            int[] fq = null;
                            try
                            {
                                fq = AlgorithmMap[charIndex];
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                if (fq != null && fq.Length == 2)
                                {
                                    fq[1]++;
                                }
                                else
                                {
                                    fq = new int[2];
                                    fq[0] = 0;
                                    fq[1] = 1;
                                    AlgorithmMap.Add(charIndex, fq);
                                }
                            }
                        }
                    }
                }
                double sqdoc1 = 0;
                double sqdoc2 = 0;
                double denominator = 0;
                foreach (KeyValuePair<int, int[]> par in AlgorithmMap)
                {
                    int[] c = par.Value;
                    denominator += c[0] * c[1];
                    sqdoc1 += c[0] * c[0];
                    sqdoc2 += c[1] * c[1];
                }
                return denominator / Math.Sqrt(sqdoc1 * sqdoc2);
            }
            else
            {
                throw new Exception();
            }
        }
        public static bool IsHanZi(char ch)
        {
            // 判断是否汉字
            return (ch >= 0x4E00 && ch <= 0x9FA5);
        }
        /**
         * 根据输入的Unicode字符，获取它的GB2312编码或者ascii编码，
         * 
         * @param ch
         *            输入的GB2312中文字符或者ASCII字符(128个)
         * @return ch在GB2312中的位置，-1表示该字符不认识
         */
        public static short GetGB2312Id(char ch)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.GetEncoding("gb2312").GetBytes(ch.ToString());
                if (buffer.Length != 2)
                {
                    // 正常情况下buffer应该是两个字节，否则说明ch不属于GB2312编码，故返回'?'，此时说明不认识该字符
                    return -1;
                }
                int b0 = (int)(buffer[0] & 0x0FF) - 161; // 编码从A1开始，因此减去0xA1=161
                int b1 = (int)(buffer[1] & 0x0FF) - 161; // 第一个字符和最后一个字符没有汉字，因此每个区只收16*6-2=94个汉字
                return (short)(b0 * 94 + b1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return -1;
        }
        #endregion

    }
}