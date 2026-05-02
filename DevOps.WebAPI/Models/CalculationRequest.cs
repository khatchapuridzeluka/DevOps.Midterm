using System.ComponentModel.DataAnnotations;

namespace DevOps.WebAPI.Models;


public class CalculationRequest
{
    [Required]
    public string Operation { get; set; } = string.Empty;

    [Required]
    public double A { get; set; }

    [Required]
    public double B { get; set; }
}