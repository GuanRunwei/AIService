using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class ChatRecord
    {
        public long Id { get; set; } //聊天Id
        public long UserId1 { get; set; }//此条聊天记录本人Id
        public long UserId2 { get; set; }//此条聊天记录对方Id
        public string ChatContent { get; set; }//聊天内容
        public DateTime ChatTime { get; set; }//本条聊天记录产生时间
    }
}
