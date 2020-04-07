
namespace AIService.Models
{
    using AIService.Enums;
    using AIService.Helper;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class User
    {   
        public User()
        {
            
        }
        public long Id { get; set; }//用户Id
        public string Username { get; set; }//用户名
        public string Phonenumber { get; set; }//手机号码
        public string Password { get; set; }//密码
        public DateTime CreateTime { get; set; }//用户创建时间
        public string Remark { get; set; }//用户个人签名
        public string ImageUrl { get; set; }//用户头像链接
        public Gender Gender { get; set; }//用户性别（0是女，1是男）
        public int FansNumber { get; set; }//用户粉丝数量
        public int FollowNumber { get; set; }//用户关注的用户数
        public UserType UserType { get; set; } //用户类型（0是个人用户，1是公司用户）
        //说说数量TalkNumber用Redis存储
        public string StockAge { get { return StockAgeCalculation.Age(Id); } }  //股龄
        [DefaultValue(0)]
        public int CoinNumber { get; set; }  //金币数量

    }
}
