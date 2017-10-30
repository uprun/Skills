using System;
using System.Collections.Generic;
namespace Skills.Models
{
    public class SkillModel: BaseModel
    {
        public int MinutesSpent {get; set;}

        public string SkillName {get; set;}
        
        public List<LinkModel> ToProcess {get; set;}
    }
}