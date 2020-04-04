using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class FollowRecord
    {
        public long Id { get; set; } //关注记录Id
        public long FollowingId { get; set; } //关注执行用户的Id
        public long FollowedId { get; set; } //被关注的用户的Id
        public DateTime FollowTime { get; set; } //关注时间
    }
}
