using System.ComponentModel.DataAnnotations;

namespace DevOps.WebAPI.Models;

public class PersonRequest
{
    [Required, MinLength(2)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0, 150)]
    public int Age { get; set; }
}