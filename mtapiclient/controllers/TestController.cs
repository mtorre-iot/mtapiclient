using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mtapiclient.classes;
using mtapiclient.common;
using Newtonsoft.Json.Linq;

namespace mtapiclient.Controllers;

[ApiController]
[Route("webhook/v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly CycleTimer cycleTimer;
    private readonly ConcurrentQueue<List<Record>> webhookQueue;
    private readonly AppSettings config;
    private readonly JObject vars;

    public TestController(CycleTimer cycleTimer, ConcurrentQueue<List<Record>> webhookQueue, JObject vars, AppSettings config)
    {
        this.cycleTimer = cycleTimer;
        this.webhookQueue = webhookQueue;
        this.vars = vars;
        this.config = config;
    }
    [HttpGet(Name = "Health")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]

    public IActionResult Health()
    {
        return Ok(new { message = "API is Online"});
    }
}