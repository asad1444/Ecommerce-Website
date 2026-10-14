namespace loginlogout.Models
{
    public class AboutVM
    {
        public int AboutId { get; set; }
        public IFormFile? UpperPhoto { get; set; }
        public string Uppertitle { get; set; } = string.Empty;
        public string uppercontent { get; set; } = string.Empty;

    }
}
