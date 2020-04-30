using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using AIService.Enums;

namespace AIService.Controllers
{
    [Route("api/user/[action]")]
    [ApiController]
    public class UserController : Controller
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

        #region 登录
        [HttpPost]
        public HttpResponseMessage Login(string Phonenumber, string Password)
        {
            User loginUser = db.Users.Where(s => s.Phonenumber == Phonenumber).FirstOrDefault();
            
            IDatabase redisDatabase = RedisHelper.Value.Database;
            
            if (loginUser == null)
                return ApiResponse.Invalid("Phonenumber", "帐号不存在");
            if (SecurityHelper.MD5Hash(Password) != loginUser.Password)
                return ApiResponse.Invalid("Password", "密码错误");
            else
            {
                StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == loginUser.Id);
                //double money = 0;
                //if(stockAccount != null)
                //{
                    
                //    List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
                //    if (simulationStocks == null)
                //        money = stockAccount.ValidMoney;
                //    if(simulationStocks!=null)
                //    {
                //        money = stockAccount.ValidMoney;
                //        for(int i=0;i<simulationStocks.Count;i++)
                //        {
                //            money += simulationStocks[i].NowPrice * simulationStocks[i].StockNumber;
                //        }
                //    }
                //}
                string TalkNumber_Key = "UserId=" + loginUser.Id.ToString() + "&TalkNumber";
                return ApiResponse.Ok(new
                {
                    loginUser.Id,
                    loginUser.Username,
                    loginUser.Phonenumber,
                    loginUser.CreateTime,
                    loginUser.ImageUrl,
                    Gender = loginUser.Gender.ToString(),
                    StockAge = loginUser.StockAge,
                    loginUser.FansNumber,
                    loginUser.FollowNumber,
                    TalkNumber = redisDatabase.StringGet(TalkNumber_Key),
                    loginUser.Remark,
                    loginUser.CoinNumber,
                    SumMoney = stockAccount==null? "0":ParamHelper.ConvertNumber(stockAccount.SumMoney)
                });

            }             
        }
        #endregion

        #region 注册
        [HttpPost]
        public HttpResponseMessage Regist(string Phonenumber, string Password, string Username, int Gender)
        {
            if (Phonenumber.Length != 11)
                return ApiResponse.Invalid("Phonenumber", "手机号不正确!");
            if (!Regex.IsMatch(Phonenumber, "^(13[0-9]|14[579]|15[0-3,5-9]|16[6]|17[0135678]|18[0-9]|19[89])\\d{8}$"))
                return ApiResponse.Invalid("Phonenumber", "手机号不正确!");
            if (Password.Count() < 6)
                return ApiResponse.Invalid("Password", "密码长度须大于等于6位！");
            if (db.Users.Where(s => s.Phonenumber == Phonenumber).Count() > 0)
                return ApiResponse.Invalid("Phonenumber", "手机号已存在！");
            if (db.Users.Where(s => s.Username == Username).Count() > 0)
                return ApiResponse.Invalid("Username", "用户名已存在！");
            User user = new User
            {
                Username = Username,
                Phonenumber = Phonenumber,
                Password = SecurityHelper.MD5Hash(Password),
                CreateTime = DateTime.Now,
                Gender = (Enums.Gender)Gender,
                ImageUrl = "http://119.23.221.142/Files/man_icon_white.png"
            };
            db.Users.Add(user);
            db.SaveChanges();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string TalkNumber_Key = "UserId=" + user.Id.ToString() + "&TalkNumber";
            redisDatabase.StringSet(TalkNumber_Key, 0);
            return ApiResponse.Ok(new
            {
                user.Id,
                user.Username,
                user.Password,
                user.Phonenumber,
                user.CreateTime,
                Gender = user.Gender.ToString(),
                user.FollowNumber,
                user.FansNumber,
                TalkNumber = redisDatabase.StringGet(TalkNumber_Key),
                StockAge = user.StockAge,
                user.Remark
            });
        }
        #endregion

        #region 上传头像
        [HttpPost]
        public HttpResponseMessage UploadImage(long UserId, string ImageUrl)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            try
            {
                user.ImageUrl = ImageUrl;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("网炸了，重新上传一下吧");
            }
            return ApiResponse.Ok(new 
            {
                ImageUrl = ImageUrl,
                message = "上传成功"
            });
        }
        #endregion

        #region 修改头像
        [HttpPost]
        public HttpResponseMessage UpdateImageUrl(long UserId, string ImageUrl)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            user.ImageUrl = ImageUrl;
            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch
            {
                db.Entry(user).State = EntityState.Unchanged;
                db.SaveChanges();
                return ApiResponse.Ok(new
                {
                    message = "头像修改失败"
                });
            }
            return ApiResponse.Ok(new 
            {
                ImageUrl = ImageUrl,
                message = "头像修改成功"
            });
        }
        #endregion

        #region 个人主页
        [HttpGet]
        public IActionResult GetHomePage(long UserId, long MyId)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string TalkNumber_Key = "UserId=" + UserId.ToString() + "&TalkNumber";
            ParamHelper paramHelper = new ParamHelper();
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == user.Id);
            if(MyId==UserId)
            {
                return Json(new
                {
                    user.Id,
                    user.Username,
                    user.Remark,
                    Gender = user.Gender.ToString(),
                    StockAge = user.StockAge,
                    user.ImageUrl,
                    user.FansNumber,
                    user.FollowNumber,
                    user.CoinNumber,
                    TalkNumber = redisDatabase.StringGet(TalkNumber_Key),
                    SumMoney = stockAccount == null ? "0" : ParamHelper.ConvertNumber(stockAccount.SumMoney)
                });
            }
            else
            {
                FollowRecord followRecord = db.FollowRecords.FirstOrDefault(s => s.FollowingId == MyId && s.FollowedId == UserId);
                if(followRecord==null)
                {
                    return Json(new
                    {
                        user.Id,
                        user.Username,
                        user.Remark,
                        Gender = user.Gender.ToString(),
                        StockAge = user.StockAge,
                        user.ImageUrl,
                        user.FansNumber,
                        user.FollowNumber,
                        user.CoinNumber,
                        TalkNumber = redisDatabase.StringGet(TalkNumber_Key),
                        SumMoney = stockAccount == null ? "0" : ParamHelper.ConvertNumber(stockAccount.SumMoney),
                        If_Follow = "false"
                    });
                }
                else
                {
                    return Json(new
                    {
                        user.Id,
                        user.Username,
                        user.Remark,
                        Gender = user.Gender.ToString(),
                        StockAge = user.StockAge,
                        user.ImageUrl,
                        user.FansNumber,
                        user.FollowNumber,
                        user.CoinNumber,
                        TalkNumber = redisDatabase.StringGet(TalkNumber_Key),
                        SumMoney = stockAccount == null ? "0" : ParamHelper.ConvertNumber(stockAccount.SumMoney),
                        If_Follow = "true"
                    });
                }
            }
            
        }
        #endregion

        #region 发布个性签名
        [HttpPost]
        public HttpResponseMessage SendQianMing(long UserId, string Remark)
        {
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            sensitiveWordInterceptor.SourctText = Remark;
            if (sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            try
            {
                user.Remark = Remark;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest(Message.EditFailure);
            }
            return ApiResponse.Ok(new 
            {
                user.Remark
            });
        }
        #endregion

        #region 用户反馈
        [HttpPost]
        public HttpResponseMessage SendFeedback(long UserId, string Content)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            try
            {
                Feedback feedback = new Feedback
                {
                    User = user,
                    UserId = UserId,
                    Content = Content,
                    FeedbackTime = DateTime.Now
                };
                db.Feedbacks.Add(feedback);
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest(Message.EditFailure);
            }
            return ApiResponse.Ok("反馈成功！");
        }
        #endregion

        #region 关注用户列表
        [HttpGet]
        public IActionResult GetAllFollowUsers(long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            IList<FollowRecord> followRecords = db.FollowRecords.Where(s => s.FollowingId == UserId).ToList();
            IList<Comment> comments = db.Comments.ToList();
            if (followRecords == null || followRecords.Count == 0)
                return Json(new 
                {
                    data = new { }
                });
            Dictionary<Talk, KeyValuePair<User, string[]>> followTalks = new Dictionary<Talk, KeyValuePair<User, string[]>>();
            for(int i=0;i<followRecords.Count;i++)
            {
                User tempUser = db.Users.FirstOrDefault(s => s.Id == followRecords[i].FollowedId);
                List<Talk> tempTalks = db.Talks.Where(s => s.UserId == tempUser.Id).ToList();
                if (tempTalks.Count == 0)
                    continue;
                else
                {
                    for (int j = 0; j < tempTalks.Count; j++)
                    {
                        string TalkPraise_Key = "TalkId=" + tempTalks[j].Id.ToString() + "&PraiseNumber";//格式例如： TalkId=1&Praise
                        string TalkTransmit_Key = "TalkId=" + tempTalks[j].Id.ToString() + "&TransmitNumber";
                        string TalkComment_Key = "TalkId=" + tempTalks[j].Id.ToString() + "&CommentNumber";
                        string TalkRead_Key = "TalkId=" + tempTalks[j].Id.ToString() + "&ReadNumber";
                        string Talk_User_Praise_Key = "TalkId=" + tempTalks[j].Id.ToString() + "&UserId=" + UserId.ToString();
                        string TalkPraise_Value = redisDatabase.StringGet(TalkPraise_Key);
                        string TalkTransmit_Value = redisDatabase.StringGet(TalkTransmit_Key);
                        string TalkComment_Value = redisDatabase.StringGet(TalkComment_Key);
                        string TalkRead_Value = (int.Parse(TalkPraise_Value) + int.Parse(TalkComment_Value)).ToString();
                        string Talk_User_Praise_Value = redisDatabase.KeyExists(Talk_User_Praise_Key).ToString();
                        followTalks.Add(tempTalks[j], new KeyValuePair<User,string[]>(tempUser,new string[] { TalkPraise_Value, TalkTransmit_Value , TalkComment_Value, TalkRead_Value, Talk_User_Praise_Value }));
                    }
                }
            }
            return Json(new
            {
                data = followTalks.OrderByDescending(s => s.Key.TalkTime).Select(s => new
                {
                    TalkId = s.Key.Id,
                    Content = s.Key.Content,
                    TalkTime = ParamHelper.TalkTimeConvert(s.Key.TalkTime),
                    PraiseNumber = s.Value.Value[0],
                    TransmitNumber = s.Value.Value[1],
                    CommentNumber = s.Value.Value[2],
                    ReadNumber = s.Value.Value[3],
                    If_Priase = s.Value.Value[4],
                    UserId = s.Value.Key.Id,
                    Username = s.Value.Key.Username,
                    ImageUrl = s.Value.Key.ImageUrl,
                    Pictures = db.Pictures.Where(p => p.TalkId == s.Key.Id).Select(p => p.FileUrl).ToList(),
                    CommentData = s.Key.Comments.Where(c => c.TalkId == s.Key.Id).Select(c => new
                    {
                        c.Id,
                        c.Point,
                        c.UserId,
                        Username = db.Users.FirstOrDefault(u => u.Id == c.UserId).Username,
                        ImageUrl = db.Users.FirstOrDefault(t => t.Id == c.UserId).ImageUrl,
                        CommentTime = ParamHelper.TalkTimeConvert(c.CommentTime),
                        PraiseNumber = redisDatabase.StringGet("CommentId=" + c.Id.ToString() + "&Praise"),
                        Comment_If_Praise = redisDatabase.KeyExists("CommentId=" + c.Id.ToString() + "&UserId=" + UserId.ToString()).ToString()
                    })
                })
            });
        }
        #endregion

        #region 用户添加关注
        [HttpPost]
        public HttpResponseMessage AddFollow(long FollowingId, long FollowedId)
        {
            FollowRecord checkRecord = db.FollowRecords.FirstOrDefault(s => s.FollowedId == FollowedId && s.FollowingId == FollowingId);
            if (checkRecord != null)
                return ApiResponse.BadRequest("您已关注过此用户");
            FollowRecord followRecord = new FollowRecord
            {
                FollowedId = FollowedId,
                FollowingId = FollowingId,
                FollowTime=DateTime.Now
            };
            User followedUser = db.Users.FirstOrDefault(s => s.Id == FollowedId);
            followedUser.FansNumber += 1;
            User followingUser = db.Users.FirstOrDefault(s => s.Id == FollowingId);
            followingUser.FollowNumber += 1;
            try
            {              
                db.FollowRecords.Add(followRecord);
                db.Entry(followedUser).State = EntityState.Modified;
                db.Entry(followingUser).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            return ApiResponse.Ok(new
            {
                followRecord.FollowTime,
                Message="用户 "+followingUser.Username+" 关注用户 "+followedUser.Username+" 成功！"
            });
        }
        #endregion

        #region 所有用户信息
        //[Route("Users")]
        public IActionResult GetUsers()
        {
            List<User> users = db.Users.ToList();
            return Json(new
            {
                data = users.Select(s => new
                {
                    s.Id,
                    s.Username,
                    s.Phonenumber,
                    s.Password,
                    s.CreateTime,
                    s.ImageUrl
                })
            });
        }
        #endregion

        #region 发送聊天消息
        [HttpPost]
        public HttpResponseMessage SendMessage(long UserId1,long UserId2,string Content)
        {
            ChatRecord chatRecord = new ChatRecord()
            {
                UserId1=UserId1,
                UserId2=UserId2,
                ChatContent=Content,
                ChatTime=DateTime.Now
            };
            try
            {
                db.ChatRecords.Add(chatRecord);
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("发送失败！");
            }
            return ApiResponse.Ok(new
            {
                chatRecord.Id,
                chatRecord.UserId1,
                chatRecord.UserId2,
                chatRecord.ChatContent,
                chatRecord.ChatTime
            });
        }
        #endregion

        #region 获取聊天列表
        [HttpGet]
        public IActionResult GetChatList(long UserId)
        {
            List<ChatRecord> allChatRecords = db.ChatRecords.Where(s => s.UserId1 == UserId || s.UserId2 == UserId).OrderByDescending(s => s.ChatTime).ToList();
            Dictionary<long, string[]> result = new Dictionary<long, string[]>(); //key为对方用户的Id即UserId2
            for(int i=0;i<allChatRecords.Count;i++)
            {
                ChatRecord temp = allChatRecords[i];
                long UserId2 = temp.UserId1 == UserId ? temp.UserId2 : temp.UserId1;
                if (result.Keys.Contains(UserId2))
                    continue;
                result.Add(UserId2, new string[] { db.Users.FirstOrDefault(s => s.Id == UserId2).Username, temp.ChatContent, temp.ChatTime.Date == DateTime.Today ? "今天 " + temp.ChatTime.ToString("HH:mm") : temp.ChatTime.ToString("yyyy-MM-dd hh:mm") });
            }
            return Json(new
            {
                data=result.Select(s=>new 
                {
                    UserId = s.Key,
                    Username = s.Value[0],
                    LatestChatContent = s.Value[1],
                    LatestDatetime = s.Value[2],
                })
            });
        }
        #endregion

        #region 获取当前聊天
        [HttpGet]
        public IActionResult GetChatDetail(long UserId1,long UserId2)
        {
            List<ChatRecord> chatRecords = db.ChatRecords.Where(s => (s.UserId1 == UserId1 && s.UserId2 == UserId2) || (s.UserId1 == UserId2 && s.UserId2 == UserId1)).OrderBy(s => s.ChatTime).ToList();
            User me = db.Users.FirstOrDefault(s => s.Id == UserId1);
            User opposite = db.Users.FirstOrDefault(s => s.Id == UserId2);
            return Json(new 
            {
                Me = new 
                {
                    me.Id,
                    me.Username,
                    me.ImageUrl
                },
                Opposite = new 
                {
                    opposite.Id,
                    opposite.Username,
                    opposite.ImageUrl
                },
                ChatData = chatRecords.Select(s=>new 
                {
                    From_UserId = s.UserId1,
                    s.ChatContent,
                    s.ChatTime
                })
            });
        }
        #endregion

        #region 每浏览1分钟 增加1枚金币
        [HttpPost]
        public HttpResponseMessage AddGoldCoin(long UserId)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            try
            {
                user.CoinNumber += 1;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("增加失败");
            }
            return ApiResponse.Ok("恭喜你获得1金币");
        }
        #endregion

        #region 修改个人信息
        [HttpPost]
        public HttpResponseMessage ChangePersonalInfo(long UserId, string ImageUrl, string Username, int Gender, string Remark)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            user.ImageUrl = ImageUrl;
            user.Username = Username;
            user.Gender = (Enums.Gender)(Gender);
            user.Remark = Remark;
            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("修改失败");
            }
            return ApiResponse.Ok(new 
            {
                user.Id,
                user.Username,
                user.ImageUrl,
                user.Gender,
                user.Remark
            });

        }
        #endregion

        #region 修改密码
        [HttpPost]
        public HttpResponseMessage ChangePassword(long UserId, string OldPassword, string NewPassword)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            if (SecurityHelper.MD5Hash(OldPassword) != user.Password)
                return ApiResponse.BadRequest("原密码不正确");
            user.Password = SecurityHelper.MD5Hash(NewPassword);
            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("网络异常");
            }
            return ApiResponse.Ok("修改成功");

        }
        #endregion

        #region 问答

        #region 提问
        [HttpPost]
        public HttpResponseMessage SendQuestion(long UserId, string QuestionContent)
        {
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            if (user.CoinNumber < 2)
                return ApiResponse.BadRequest("金币不够两个哟");
            sensitiveWordInterceptor.SourctText = QuestionContent;
            if (sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            if (QuestionContent.Length < 15)
                return ApiResponse.BadRequest("提问内容须大于等于15个字！");
            Question question = new Question()
            {
                UserId = UserId,
                QuestionContent = QuestionContent,
                QuestionTime = DateTime.Now,
                User = user
            };
            user.CoinNumber -= 2;
            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.Questions.Add(question);
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(user).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("提问失败");
            }
            return ApiResponse.Ok(new
            {
                question.Id,
                question.QuestionContent,
                QuestionTime = question.QuestionTime.ToString(),

            });

        }
        #endregion

        #region 回答
        [HttpPost]
        public HttpResponseMessage SendAnswer(int QuestionId, long UserId, string AnswerContent)
        {
            User answerUser = db.Users.FirstOrDefault(s => s.Id == UserId);
            if (DateTime.Today.Subtract(answerUser.CreateTime.Date).Days < 60)
                return ApiResponse.BadRequest("股龄大于60天才能使用此功能");
            Question question = db.Questions.FirstOrDefault(s => s.Id == QuestionId);
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            if (question.UserId == UserId)
                return ApiResponse.BadRequest("自己不能回答自己的问题哟");
            sensitiveWordInterceptor.SourctText = AnswerContent;
            if(sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            if (AnswerContent.Length < 15)
                return ApiResponse.BadRequest("回答内容须大于等于15个字！");
            Answer answer = new Answer()
            {
                AnswerContent = AnswerContent,
                AnswerTime = DateTime.Now,
                UserId = UserId,
                User = db.Users.FirstOrDefault(s => s.Id == UserId)
            };
            try
            {
                db.Answers.Add(answer);
                db.SaveChanges();
                question.AnswerId = answer.Id;
                db.Entry(question).State = EntityState.Modified;
                db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(question).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("回答失败");
            }
            return ApiResponse.Ok(new 
            {
                answer.Id,
                answer.AnswerContent,
                AnswerTime = answer.AnswerTime.ToString()
            });

        }
        #endregion

        #region 问答List
        [HttpGet]
        public HttpResponseMessage GetQAList()
        {
            List<Question> questionList = db.Questions.ToList();
            Dictionary<Question, User> result = new Dictionary<Question, User>();
            for(int i=0;i<questionList.Count;i++)
            {
                Answer tempAnswer = db.Answers.FirstOrDefault(s => s.Id == questionList[i].AnswerId);
                if(tempAnswer!=null)
                {
                    User tempUser = db.Users.FirstOrDefault(s => s.Id == tempAnswer.UserId);
                    result.Add(questionList[i], tempUser);
                }
                else
                {
                    result.Add(questionList[i], null);
                }
            }
            result.OrderByDescending(s => s.Key.QuestionTime);
            return ApiResponse.Ok(new 
            {
                data = result.Select(s=>new 
                {
                    s.Key.Id,
                    s.Key.QuestionContent,
                    QuestionTime = s.Key.QuestionTime.ToString(),
                    Answer_Username = s.Value == null ? "" :  s.Value.Username,
                    Answer_ImageUrl = s.Value == null ? "" : s.Value.ImageUrl,
                })
            });
            
        }
        #endregion

        #region 我的问答
        [HttpGet]
        public HttpResponseMessage GetMyQAList(long UserId)
        {
            List<Question> questionList = db.Questions.Where(s=>s.UserId==UserId).ToList();
            Dictionary<Question, User> result = new Dictionary<Question, User>();
            for (int i = 0; i < questionList.Count; i++)
            {
                Answer tempAnswer = db.Answers.FirstOrDefault(s => s.Id == questionList[i].AnswerId);
                if (tempAnswer != null)
                {
                    User tempUser = db.Users.FirstOrDefault(s => s.Id == tempAnswer.UserId);
                    result.Add(questionList[i], tempUser);
                }
                else
                {
                    result.Add(questionList[i], null);
                }
            }
            result.OrderByDescending(s => s.Key.QuestionTime);
            if(result.Count==0)
            {
                return ApiResponse.BadRequest("您还没有提问过");
            }
            return ApiResponse.Ok(
                result.Select(s => new
                {
                    s.Key.Id,
                    s.Key.QuestionContent,
                    QuestionTime = s.Key.QuestionTime.ToString(),
                    Answer_Username = s.Value == null ? "" : s.Value.Username,
                    Answer_ImageUrl = s.Value == null ? "" : s.Value.ImageUrl,
                })
            );
        }
        #endregion

        #region 回答Detail
        [HttpGet]
        public HttpResponseMessage GetQADetail(int QuestionId)
        {
            Question question = db.Questions.FirstOrDefault(s => s.Id == QuestionId);
            if (question.AnswerId == 0)
                return ApiResponse.Ok(new 
                {
                    question.Id,
                    question.QuestionContent,
                    QuestionTime = question.QuestionTime,
                    Ask_UserId = question.UserId,
                    Ask_Username = db.Users.FirstOrDefault(s=>s.Id==question.UserId).Username,
                    Ask_ImageUrl = db.Users.FirstOrDefault(s=>s.Id==question.UserId).ImageUrl
                });
            else
            {
                Answer answer = db.Answers.FirstOrDefault(s => s.Id == question.AnswerId);
                return ApiResponse.Ok(new
                {
                    QuestionId = question.Id,
                    question.QuestionContent,
                    QuestionTime = question.QuestionTime.ToString(),
                    Ask_UserId = question.UserId,
                    Ask_Username = db.Users.FirstOrDefault(s => s.Id == question.UserId).Username,
                    Ask_ImageUrl = db.Users.FirstOrDefault(s => s.Id == question.UserId).ImageUrl,
                    AnswerId = answer.Id,
                    answer.AnswerContent,
                    AnswerTime = answer.AnswerTime.ToString(),
                    Answer_UserId = answer.UserId,
                    Answer_Username = db.Users.FirstOrDefault(s=>s.Id==answer.UserId).Username,
                    Answer_ImageUrl = db.Users.FirstOrDefault(s => s.Id == answer.UserId).ImageUrl

                });
            }
        }
        #endregion

        #endregion

        #region 粉丝列表
        [HttpGet]
        public IActionResult GetFansList(long UserId)
        {
            List<User> fans = new List<User>();
            long[] fans_Id = db.FollowRecords.Where(s => s.FollowedId == UserId).Select(s=>s.FollowingId).ToArray();
            foreach(long id in fans_Id)
            {
                fans.Add(db.Users.FirstOrDefault(s => s.Id == id));
            }
            return Json(new 
            {
                code = 200,
                data = fans.Select(s=>new 
                {
                    s.Id,
                    s.Username,
                    StockAge = "股龄" + s.StockAge,
                    s.ImageUrl
                })
            });
        }
        #endregion

        #region 关注列表
        [HttpGet]
        public IActionResult GetAttentionList(long UserId)
        {
            List<User> fans = new List<User>();
            long[] fans_Id = db.FollowRecords.Where(s => s.FollowingId == UserId).Select(s => s.FollowedId).ToArray();
            foreach (long id in fans_Id)
            {
                fans.Add(db.Users.FirstOrDefault(s => s.Id == id));
            }
            return Json(new
            {
                code = 200,
                data = fans.Select(s => new
                {
                    s.Id,
                    s.Username,
                    StockAge = "股龄" + s.StockAge,
                    s.ImageUrl
                })
            });
        }
        #endregion

        #region 搜索用户
        [HttpGet]
        public IActionResult SearchUsers(string Username)
        {
            List<User> resultUsers = db.Users.Where(s => s.Username.ToLower() == Username.ToLower() || s.Username.ToLower().Contains(Username.ToLower()) || GetSimilarity(s.Username, Username) >= 0.6).ToList();
            return Json(new 
            {
                code = 200,
                data = resultUsers.Select(s=>new 
                {
                    s.Id,
                    s.ImageUrl,
                    s.Username
                })
            });
        }
        #endregion

        #region 搜索用户初始化界面
        [HttpGet]
        public IActionResult GetStartSearchUsers()
        {
            List<User> users = db.Users.OrderByDescending(s => s.FansNumber).Take(3).ToList();
            return Json(new 
            {
                code = 200,
                data = users.Select(s=>new 
                {
                    s.Id,
                    s.Username,
                    s.ImageUrl,
                    FansNumber = s.FansNumber + "人关注" + (s.Gender.Equals(Enums.Gender.男) ? "他":"她")
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

        //        #region 上传头像
        //        [HttpPost]
        //        [DisableRequestSizeLimit]
        //        public HttpResponseMessage UploadImage()
        //        {
        //            long UserId = long.Parse(Request.Form["UserId"]);
        //            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
        //            string headString = "http://119.23.221.142/";
        //            var userImage = Request.Form.Files;
        //            if (userImage.Count > 1)
        //                return ApiResponse.BadRequest("头像只能上传一个哟");
        //            if (userImage[0].Length > 6000000)
        //                return ApiResponse.BadRequest("图片大小不能大于6M哟");
        //            PictureInterceptor pictureInterceptor = new PictureInterceptor();
        //            if (!pictureInterceptor.JudgePictures(userImage))
        //            {
        //                return ApiResponse.BadRequest("上传图片类型错误！");
        //            }
        //            try
        //            {
        //                string shortTime = DateTime.Now.ToString("yyyyMMdd") + "/";
        //                string filePhysicalPath = "UploadFiles/" + shortTime;  //文件路径  可以通过注入 IHostingEnvironment 服务对象来取得Web根目录和内容根目录的物理路径
        //                if (!Directory.Exists(filePhysicalPath)) //判断上传文件夹是否存在，若不存在，则创建
        //                {
        //                    Directory.CreateDirectory(filePhysicalPath); //创建文件夹
        //                }
        //                var fileName = System.Guid.NewGuid().ToString() + Path.GetExtension(userImage[0].FileName);//文件名+文件后缀名
        //                using (var stream = new FileStream(filePhysicalPath + fileName, FileMode.Create))
        //                {
        //                    userImage[0].CopyTo(stream);
        //                    user.ImageUrl = headString + filePhysicalPath + fileName;
        //                    db.Entry(user).State = EntityState.Modified;
        //                    db.SaveChanges();
        //                }
        //            }
        //#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
        //            catch(Exception ex)
        //#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
        //            {
        //                db.Entry(user).State = EntityState.Unchanged;
        //                return ApiResponse.BadRequest(Message.EditFailure);
        //            }
        //            return ApiResponse.Ok(new
        //            {
        //                user.ImageUrl
        //            });

        //        }
        //        #endregion

    }
}