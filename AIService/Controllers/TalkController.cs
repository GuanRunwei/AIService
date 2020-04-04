using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AIService.Controllers
{
    [Route("api/talk/[action]")]
    [ApiController]
    public class TalkController : Controller
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

        #region 发说说（文字图片混合）Redis字段为UserId=?&TalkNumber
        [HttpPost]
        public HttpResponseMessage SendTalk(long UserId, string Content, string PictureUrl)
        {
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            sensitiveWordInterceptor.SourctText = Content;
            if(sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            Talk talk = new Talk();
            Picture picture = new Picture();
            try
            {
                #region 生成并上传文字
                talk.UserId = UserId;
                talk.Content = Content;
                talk.TalkTime = DateTime.Now;
                talk.User = db.Users.FirstOrDefault(s => s.Id == UserId);
                db.Talks.Add(talk);
                db.SaveChanges();
                #endregion

                #region Redis中该用户的说说数量加一，同时该说说的点赞数和转发数设置为0
                IDatabase redisDatabase = RedisHelper.Value.Database;
                string TalkNumber_Key = "UserId=" + UserId.ToString() + "&TalkNumber"; //用户的说说数量key
                redisDatabase.StringIncrement(TalkNumber_Key, 1);
                string TalkPraise_Key = "TalkId=" + talk.Id.ToString() + "&PraiseNumber";//格式例如： TalkId=1&Praise
                redisDatabase.StringSet(TalkPraise_Key, 0);
                string TalkTransmit_Key = "TalkId=" + talk.Id.ToString() + "&TransmitNumber";//格式例如：TalkId=1&Transmit
                redisDatabase.StringSet(TalkTransmit_Key, 0);
                string TalkComment_Key = "TalkId=" + talk.Id.ToString() + "&CommentNumber";
                redisDatabase.StringSet(TalkComment_Key, 0);
                string TalkRead_Key = "TalkId=" + talk.Id.ToString() + "&ReadNumber";
                redisDatabase.StringSet(TalkRead_Key, 0);
                #endregion

                #region 图片关联
                if(PictureUrl!=null)
                {
                    picture.FileUrl = PictureUrl;
                    picture.TalkId = talk.Id;
                    picture.FileSize = 0;
                    db.Pictures.Add(picture);
                    db.SaveChanges();
                }
                
                #endregion
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok(new
            {
                talk.Id,
                talk.UserId,
                talk.User.Username,
                StockAge = talk.User.StockAge,
                talk.Content,
                TalkTime = ParamHelper.TalkTimeConvert(talk.TalkTime),
                PraiseNumber = 0,
                TransmitNumber = 0,
                CommentNumber = 0,
                ReadNumber = 0,
                PictureUrl = picture == null? null : picture.FileUrl
            });
        }
        #endregion

        #region 评论 每评论一次，Redis内的TalkId=?&CommentNumber字段加一
        [HttpPost]
        public HttpResponseMessage SendComment(long UserId, long TalkId, string Point)
        {
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            sensitiveWordInterceptor.SourctText = Point;
            if (sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            Comment comment = new Comment();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            User Commenter = db.Users.FirstOrDefault(s => s.Id == UserId);
            string tempName = Commenter.Username;
            try
            {               
                comment.UserId = UserId;
                comment.TalkId = TalkId;
                comment.Point = Point;
                comment.CommentTime = DateTime.Now;
                comment.Commenter = tempName;
                string TalkComment_Key = "TalkId=" + TalkId + "&CommentNumber";
                redisDatabase.StringIncrement(TalkComment_Key, 1);
                db.Comments.Add(comment);
                db.SaveChanges();
                db.Talks.FirstOrDefault(s => s.Id == TalkId).Comments.Add(comment);
                db.SaveChanges();
                string CommentPraise_Key = "CommentId=" + comment.Id.ToString() + "&Praise";
                redisDatabase.StringSet(CommentPraise_Key, 0);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok(new
            {
                comment.Id,
                comment.TalkId,
                comment.UserId,
                comment.Point,
                CommentTime = ParamHelper.TalkTimeConvert(comment.CommentTime),
                comment.Commenter,
                Commenter.ImageUrl
            });
        }
        #endregion

        #region 说说点赞
        [HttpPost]
        public HttpResponseMessage SendTalkPraise(long TalkId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string Talk_User_Key = "TalkId=" + TalkId.ToString() + "&UserId=" + UserId.ToString();
            string TalkPraise_Key = "TalkId=" + TalkId.ToString() + "&PraiseNumber";
            if (redisDatabase.KeyExists(Talk_User_Key))
                return ApiResponse.BadRequest("您已经点过赞了");
            redisDatabase.StringIncrement(Talk_User_Key, 1);
            redisDatabase.StringIncrement(TalkPraise_Key, 1);
            Console.WriteLine(redisDatabase.StringGet(Talk_User_Key));        
            return ApiResponse.Ok("点赞成功！");
        }
        #endregion

        #region 取消说说点赞
        [HttpPost]
        public HttpResponseMessage CancelTalkPraise(long TalkId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string Talk_User_Key = "TalkId=" + TalkId.ToString() + "&UserId=" + UserId.ToString();
            string TalkPraise_Key = "TalkId=" + TalkId + "&PraiseNumber";
            try
            {
                redisDatabase.KeyDelete(Talk_User_Key);
                redisDatabase.StringDecrement(TalkPraise_Key, 1);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("取消失败！");
            }
            return ApiResponse.Ok("取消成功！");
        }
        #endregion

        #region 评论点赞
        [HttpPost]
        public HttpResponseMessage SendCommentPraise(long CommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string Comment_User_Key = "CommentId=" + CommentId.ToString() + "&UserId=" + UserId.ToString();
            string CommentPraise_Key = "CommentId=" + CommentId.ToString() + "&PraiseNumber";
            if (redisDatabase.KeyExists(Comment_User_Key))
                return ApiResponse.BadRequest("您已经点过赞了");
            redisDatabase.StringIncrement(Comment_User_Key, 1);
            redisDatabase.StringIncrement(CommentPraise_Key, 1);
            return ApiResponse.Ok("点赞成功！");
        }
        #endregion

        #region 取消评论点赞
        [HttpPost]
        public HttpResponseMessage CancelCommentPraise(long CommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string Comment_User_Key = "CommentId=" + CommentId.ToString() + "&UserId=" + UserId.ToString();
            string CommentPraise_Key = "CommentId=" + CommentId.ToString() + "&PraiseNumber";
            try
            {
                redisDatabase.KeyDelete(Comment_User_Key);
                redisDatabase.StringDecrement(CommentPraise_Key, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ApiResponse.BadRequest("取消点赞失败！");
            }
            return ApiResponse.Ok("取消点赞成功！");
        }
        #endregion

        #region 删除说说
        [HttpPost]
        public HttpResponseMessage DeleteTalk(long TalkId, long UserId)
        {
            Talk talk = db.Talks.FirstOrDefault(s => s.Id == TalkId && s.UserId == UserId);
            List<Comment> comments = db.Comments.Where(s => s.TalkId == TalkId).ToList();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            try
            {
                db.Talks.Remove(talk);
                for (int i = 0; i < comments.Count(); i++)
                {
                    string CommentPraise_Key = "CommentId=" + comments[i].Id.ToString() + "&Praise";
                    redisDatabase.KeyDelete(CommentPraise_Key);
                    db.Comments.Remove(comments[i]);
                }
                db.SaveChanges();             
                string TalkNumber_Key = "UserId=" + UserId.ToString() + "&TalkNumber"; //用户的说说数量key
                redisDatabase.StringDecrement(TalkNumber_Key, 1);
                string TalkPraise_Key = "TalkId=" + TalkId.ToString() + "&PraiseNumber";//格式例如： TalkId=1&Praise
                redisDatabase.KeyDelete(TalkPraise_Key);
                string TalkTransmit_Key = "TalkId=" + TalkId.ToString() + "&TransmitNumber";//格式例如：TalkId=1&Transmit
                redisDatabase.KeyDelete(TalkTransmit_Key);
                string TalkComment_Key = "TalkId=" + TalkId.ToString() + "&CommentNumber";//格式例如：TalkId=1&CommentNumber
                redisDatabase.KeyDelete(TalkComment_Key);
                string TalkRead_Key = "TalkId=" + TalkId.ToString() + "&ReadNumber";
                redisDatabase.KeyDelete(TalkRead_Key);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok("删除成功！");
        }
        #endregion

        #region 删除评论
        [HttpPost]
        public HttpResponseMessage DeleteComment(long CommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            Comment comment = db.Comments.FirstOrDefault(s => s.Id == CommentId && s.UserId == UserId);
            try
            {
                string CommentPraise_Key = "CommentId=" + CommentId.ToString() + "&Praise";
                redisDatabase.KeyDelete(CommentPraise_Key);
                db.Comments.Remove(comment);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok("删除成功！");
        }
        #endregion

        #region 获取论坛消息
        [HttpGet]
        public IActionResult GetTalkList(long UserId)
        {
            List<Talk> talks = db.Talks.OrderByDescending(s => s.TalkTime).ToList();
            Dictionary<Talk, string[]> result = new Dictionary<Talk, string[]>();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            for(int i=0;i<talks.Count;i++)
            {
                string TalkPraise_Key = "TalkId=" + talks[i].Id.ToString() + "&PraiseNumber";
                string TalkTransmit_Key = "TalkId=" + talks[i].Id.ToString() + "&TransmitNumber";
                string TalkComment_Key = "TalkId=" + talks[i].Id.ToString() + "&CommentNumber";
                string TalkRead_Key = "TalkId=" + talks[i].Id.ToString() + "&ReadNumber";
                string Talk_User_Praise_Key = "TalkId=" + talks[i].Id.ToString() + "&UserId=" + UserId.ToString();
                string praiseNumber = redisDatabase.StringGet(TalkPraise_Key);
                string transmitNumber = redisDatabase.StringGet(TalkTransmit_Key);
                string commentNumber = redisDatabase.StringGet(TalkComment_Key);
                string readNumber = redisDatabase.StringGet(TalkRead_Key);
                string Talk_User_Praise_Value = redisDatabase.KeyExists(Talk_User_Praise_Key).ToString();
                result.Add(talks[i], new string[5] { praiseNumber, transmitNumber, commentNumber, readNumber, Talk_User_Praise_Value });
                
            }
            return Json(new
            {
                data = result.Select(s => new
                {
                    s.Key.Id,
                    s.Key.Content,
                    s.Key.UserId,
                    TalkTime = ParamHelper.TalkTimeConvert(s.Key.TalkTime),
                    TalkType = s.Key.TalkType.ToString(),
                    Username = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).ImageUrl,
                    StockAge = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).StockAge,
                    PraiseNumber = s.Value[0],
                    TransmitNumber = s.Value[1],
                    CommentNumber = s.Value[2],
                    ReadNumber = s.Value[3],
                    If_Praise = s.Value[4],
                    PictureUrl = db.Pictures.FirstOrDefault(p => p.TalkId == s.Key.Id) == null ? null : db.Pictures.FirstOrDefault(p => p.TalkId == s.Key.Id).FileUrl
                })
            });
        }
        #endregion

        #region 说说详细内容查看
        [HttpGet]
        public IActionResult GetTalkDetail(long TalkId, long UserId)
        {
            Talk talk = db.Talks.FirstOrDefault(s => s.Id == TalkId);
            List<Comment> comments = db.Comments.Where(s => s.TalkId == TalkId).OrderByDescending(s=>s.CommentTime).ToList();
            List<Picture> pictures = db.Pictures.Where(s => s.TalkId == TalkId).ToList();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string TalkPraise_Key = "TalkId=" + TalkId.ToString() + "&PraiseNumber";
            string TalkTransmit_Key = "TalkId=" + TalkId.ToString() + "&TransmitNumber";
            string TalkComment_Key = "TalkId=" + TalkId.ToString() + "&CommentNumber";
            string TalkRead_Key = "TalkId=" + TalkId.ToString() + "&ReadNumber";
            redisDatabase.StringIncrement(TalkRead_Key, 1);
            string praiseNumber = redisDatabase.StringGet(TalkPraise_Key);
            string transmitNumber = redisDatabase.StringGet(TalkTransmit_Key);
            string commentNumber = redisDatabase.StringGet(TalkComment_Key);
            string readNumber = redisDatabase.StringGet(TalkRead_Key);
            return Json(new
            {
                talk.Id,
                talk.Content,
                TalkTime = ParamHelper.TalkTimeConvert(talk.TalkTime),
                TalkType = talk.TalkType.ToString(),
                Username = db.Users.FirstOrDefault(s => s.Id == talk.UserId).Username,
                StockAge = db.Users.FirstOrDefault(s => s.Id == talk.UserId).StockAge,
                ImageUrl = db.Users.FirstOrDefault(s => s.Id == talk.UserId).ImageUrl,
                If_Praise = redisDatabase.KeyExists("TalkId=" + talk.Id.ToString() + "&UserId=" + UserId.ToString()).ToString(),
                PraiseNumber = praiseNumber,
                TransmitNumber = transmitNumber,
                CommentNumber = commentNumber,
                ReadNumber = readNumber,
                PictureUrl = db.Pictures.FirstOrDefault(p => p.TalkId == talk.Id)==null?null: db.Pictures.FirstOrDefault(p => p.TalkId == talk.Id).FileUrl,
                CommentData = comments.Select(s => new
                {
                    s.Id,
                    s.Point,
                    s.UserId,
                    Username = db.Users.FirstOrDefault(u=>u.Id==s.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(t => t.Id == s.UserId).ImageUrl,
                    CommentTime = ParamHelper.TalkTimeConvert(s.CommentTime),
                    PraiseNumber = redisDatabase.StringGet("CommentId=" + s.Id.ToString() + "&PraiseNumber"),
                    Comment_If_Praise = redisDatabase.KeyExists("CommentId=" + s.Id.ToString() + "&UserId=" + UserId.ToString()).ToString()
                })
            });
        }
        #endregion

        #region 个人主页说说列表
        [HttpGet]
        public IActionResult GetMyTalkList(long UserId, long MyId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            List<Talk> talks = db.Talks.Where(s => s.UserId == UserId).ToList();
            Dictionary<Talk, string[]> result = new Dictionary<Talk, string[]>();
            if(UserId==MyId)
            {
                foreach(Talk item in talks)
                {
                    string TalkPraise_Key = "TalkId=" + item.Id.ToString() + "&PraiseNumber";
                    string TalkTransmit_Key = "TalkId=" + item.Id.ToString() + "&TransmitNumber";
                    string TalkComment_Key = "TalkId=" + item.Id.ToString() + "&CommentNumber";
                    string TalkRead_Key = "TalkId=" + item.Id.ToString() + "&ReadNumber";
                    string Talk_User_Praise_Key = "TalkId=" + item.Id.ToString() + "&UserId=" + UserId.ToString();
                    string praiseNumber = redisDatabase.StringGet(TalkPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(TalkTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(TalkComment_Key);
                    string readNumber = redisDatabase.StringGet(TalkRead_Key);
                    string Talk_User_Praise_Value = redisDatabase.KeyExists(Talk_User_Praise_Key).ToString();
                    string If_Mine = "True";
                    result.Add(item, new string[] { praiseNumber, transmitNumber, commentNumber, readNumber, Talk_User_Praise_Value, If_Mine });
                }
            }
            else
            {
                foreach (Talk item in talks)
                {
                    string TalkPraise_Key = "TalkId=" + item.Id.ToString() + "&PraiseNumber";
                    string TalkTransmit_Key = "TalkId=" + item.Id.ToString() + "&TransmitNumber";
                    string TalkComment_Key = "TalkId=" + item.Id.ToString() + "&CommentNumber";
                    string TalkRead_Key = "TalkId=" + item.Id.ToString() + "&ReadNumber";
                    string Talk_User_Praise_Key = "TalkId=" + item.Id.ToString() + "&UserId=" + UserId.ToString();
                    string praiseNumber = redisDatabase.StringGet(TalkPraise_Key);
                    string transmitNumber = redisDatabase.StringGet(TalkTransmit_Key);
                    string commentNumber = redisDatabase.StringGet(TalkComment_Key);
                    string readNumber = redisDatabase.StringGet(TalkRead_Key);
                    string Talk_User_Praise_Value = redisDatabase.KeyExists(Talk_User_Praise_Key).ToString();
                    string If_Mine = "False";
                    result.Add(item, new string[] { praiseNumber, transmitNumber, commentNumber, readNumber, Talk_User_Praise_Value, If_Mine });
                }
            }
            return Json(new 
            {
                code = 200, 
                data = result.Select(s=>new 
                {
                    s.Key.Id,
                    s.Key.Content,
                    s.Key.UserId,
                    TalkTime = ParamHelper.TalkTimeConvert(s.Key.TalkTime),
                    TalkType = s.Key.TalkType.ToString(),
                    Username = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).Username,
                    ImageUrl = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).ImageUrl,
                    StockAge = db.Users.FirstOrDefault(t => t.Id == s.Key.UserId).StockAge,
                    PraiseNumber = s.Value[0],
                    TransmitNumber = s.Value[1],
                    CommentNumber = s.Value[2],
                    ReadNumber = s.Value[3],
                    If_Praise = s.Value[4],
                    If_Mine = s.Value[5],
                    PictureUrl = db.Pictures.FirstOrDefault(p => p.TalkId == s.Key.Id) == null ? null : db.Pictures.FirstOrDefault(p => p.TalkId == s.Key.Id).FileUrl
                })
            });
        }
        #endregion

        #region 多图片上传测试
        [HttpPost]
        [DisableRequestSizeLimit]
        public HttpResponseMessage UploadImages(long TalkId)
        {
            string headString = "http://119.23.221.142/";
            var files = Request.Form.Files;
            long size = files.Sum(f => f.Length);
            PictureInterceptor pictureInterceptor = new PictureInterceptor();
            if(!pictureInterceptor.JudgePictures(files))
            {
                return ApiResponse.BadRequest("上传图片类型错误！");
            }
            if (size > 30000000)
                return ApiResponse.BadRequest("上传图片总量大小不得大于30M！");
            List<Picture> pictures = new List<Picture>();
            string shortTime = DateTime.Now.ToString("yyyyMMdd") + "/";
            string filePhysicalPath = "UploadFiles/" + shortTime;  //文件路径  可以通过注入 IHostingEnvironment 服务对象来取得Web根目录和内容根目录的物理路径
            if (!Directory.Exists(filePhysicalPath)) //判断上传文件夹是否存在，若不存在，则创建
            {
                Directory.CreateDirectory(filePhysicalPath); //创建文件夹
            }
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = System.Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);//文件名+文件后缀名
                    using (var stream = new FileStream(filePhysicalPath + fileName, FileMode.Create))
                    {
                        Picture picture = new Picture();                       
                        file.CopyTo(stream);
                        picture.FileUrl = headString + filePhysicalPath + fileName;
                        picture.FileType = Path.GetExtension(file.FileName).Substring(1);
                        picture.FileSize = Math.Round(stream.Length * 1.0 / (1024 * 1024),3);
                        picture.TalkId = TalkId;
                        Talk talk = db.Talks.FirstOrDefault(s => s.Id == TalkId);
                        picture.Talk = talk;
                        pictures.Add(picture);
                        db.Pictures.Add(picture);
                        db.SaveChanges();
                    }
                }
            }
            return ApiResponse.Ok(new
            {
                FileCount = files.Count,
                AllFilesSize = Math.Round(size * 1.0 / (1024 * 1024), 3),
                Message = files.Count + "张照片上传成功！",
                PictureData=pictures.Select(s=>new
                {
                    s.Id,
                    s.FileType,
                    s.FileSize,
                    s.FileUrl,
                    s.TalkId
                })

            });
        }
        #endregion

        
    }
}