namespace TaskTrackerAPI.Models
{
    public class Task
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public bool completed { get; set; }
    }
}
