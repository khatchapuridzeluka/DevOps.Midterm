using DevOps.WebAPI.Models;
using DevOps.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController : ControllerBase
{
    private readonly IPersonService _personService;

    public PersonController(IPersonService personService)
    {
        _personService = personService;
    }

    // Input form endpoint: POST /api/person
    [HttpPost("categorize")]
    public IActionResult Categorize([FromBody] PersonRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = _personService.Categorize(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}