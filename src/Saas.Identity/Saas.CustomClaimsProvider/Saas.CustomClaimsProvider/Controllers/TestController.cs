
using Microsoft.AspNetCore.Mvc;

namespace Saas.CustomClaimsProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : Controller
{
    [HttpGet("permissions")]
    [Produces("application/json")]
    public async Task<IActionResult> Get()
    {
        return new OkObjectResult(new { message = "Hello World!" });
    }
}
