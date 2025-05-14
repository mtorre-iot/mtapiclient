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
public class PushController : ControllerBase
{
    private readonly CycleTimer cycleTimer;
    private readonly ConcurrentQueue<List<Record>> webhookQueue;
    private readonly JObject vars;
    private readonly AppSettings config;
    
    public PushController(CycleTimer cycleTimer, ConcurrentQueue<List<Record>> webhookQueue, JObject vars, AppSettings config)
    {
        this.cycleTimer = cycleTimer;
        this.webhookQueue = webhookQueue;
        this.vars = vars;
        this.config = config;
    }

    [HttpPost(Name = "Push")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Push (List<Record> records)
    {
        foreach (Record record in records)
        {
            // Check if it is a valid and subscribed topic
            if (Vars.ContainsTopic(vars, record.topic) == false)
            {
                return BadRequest(new {message = $"Topic {record.topic} not found. This and rest of records will be skipped."});            
            }
        }
        
        if (cycleTimer.isOn == true)
        {        
            webhookQueue.Enqueue(records); 
        }
        return Ok (new {status = "OK"});
    }
}
