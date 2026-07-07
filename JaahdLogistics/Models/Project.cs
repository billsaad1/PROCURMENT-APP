namespace JaahdLogistics.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? ProjectManager { get; set; }
        public string? ProjectOfficer { get; set; }
    }
}
