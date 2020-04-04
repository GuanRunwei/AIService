using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class Picture
    {
        #region 导航属性
        public virtual Talk Talk { get; set; }
        #endregion

        public long Id { get; set; } //说说图片Id
        public long TalkId { get; set; } //关联的说说Id
        public double FileSize { get; set; } //图片大小
        public string FileType { get; set; } //图片类型
        public string FileUrl { get; set; } //图片地址
        
    }
}
