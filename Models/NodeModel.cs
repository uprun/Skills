using System.Collections.Generic;

namespace Skills.Models
{
    public class NodeModel
    {
        public long id {get; set;}
        public List<TagModel> tags {get; set;}

        public bool IsGeneric 
        {
            get {
                return tags?.Exists(t => t.tag.StartsWith("template:reference:") && string.IsNullOrEmpty(t.value)) ?? false;
            }
        }

        
    }
}