using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mtapiclient.classes;

namespace mtapiclient.Controllers;

[ApiController]
[Route("webhook/v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> logger;
    private readonly AppSettings config;

    public TestController(AppSettings config, ILogger<TestController> logger)
    {
        this.config = config;
        this.logger = logger;
    }
    [HttpGet(Name = "Health")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]

    public IActionResult Health()
    {
        return Ok(new { message = "API is Online"});
    }
}