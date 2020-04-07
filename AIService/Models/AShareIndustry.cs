using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class AShareIndustry
    {
        #region 导航属性
        public virtual ASharePlate Parent { get; set; } 
        public int ParentId { get; set; }
        #endregion
        public int Id { get; set; }
        public string IndustryName { get; set; }  //A股行业名称
        public string IndustryCode { get; set; }  //行业代码
        public string ParentPlateName { get; set; }  //父级行业代码
    }
}
