using System;
using System.Collections.Generic;

namespace Skills.Models
{
    public class NodeDTO
    {
        public long NodeId {get; set;}

        //public List<(string, List<string>)> tagsCombined {get; set;}
        public List<(string, string)> tagsCombined {get; set;}

    }
}