namespace DevOps.WebAPI.Models;

public class PersonCategoryResult
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
}