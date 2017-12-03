using System;
using System.Collections.Generic;

namespace Skills.Models
{
    public class SkillDTO
    {
        public long NodeId {get; set;}

        public string SkillName {get; set;}

        public List<LinkDTO> ToProcess {get ;set;}
    }
}