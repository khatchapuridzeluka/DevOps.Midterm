using DevOps.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalculatorController : ControllerBase
{
    private readonly ICalculatorService _calculator;

    public CalculatorController(ICalculatorService calculator)
    {
        _calculator = calculator;
    }

    [HttpGet("{operation}/{a:double}/{b:double}")]
    public IActionResult Calculate(string operation, double a, double b)
    {
        try
        {
            var result = Execute(operation, a, b);
            return Ok(new { operation, a, b, result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DivideByZeroException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    private double Execute(string operation, double a, double b) => operation.ToLower() switch
    {
        "add" => _calculator.Add(a, b),
        "subtract" => _calculator.Subtract(a, b),
        "multiply" => _calculator.Multiply(a, b),
        "divide" => _calculator.Divide(a, b),
        _ => throw new ArgumentException($"Unsupported operation: {operation}")
    };
}