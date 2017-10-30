namespace Skills.Models
{
    public class LinkDTO
    {
        

        public LinkDTO(LinkModel urlAdded)
        {
            this.Url = urlAdded.Url;
        }

        public string Url {get; set;}
    }
}