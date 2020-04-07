using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class ASharePlate
    {
        
        public int Id { get; set; }
        public int ParentId { get; set; }  //父级Id
        public string PlateName { get; set; }  //板块名称
    }
}
