using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class HKSharePlate
    {
        public int Id { get; set; }
        public string PlateName { get; set; }  //板块名称
        public string PlateCode { get; set; }  //板块代码
        public string ParentName { get; set; }  //父级名称
    }
}
