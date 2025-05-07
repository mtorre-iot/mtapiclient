using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mtapiclient.classes;
using Newtonsoft.Json.Linq;

namespace mtapiclient.Controllers;

[ApiController]
[Route("webhook/v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly Serilog.ILogger logger;
    private readonly AppSettings config;
    private readonly JObject vars;

    public TestController(JObject vars, AppSettings config, Serilog.ILogger logger)
    {
        this.vars = vars;
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