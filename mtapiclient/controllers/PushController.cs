using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mtapiclient.classes;
using Newtonsoft.Json.Linq;

namespace mtapiclient.Controllers;

[ApiController]
[Route("webhook/v1/[controller]")]
public class PushController : ControllerBase
{
    private readonly Serilog.ILogger logger;
    private readonly AppSettings config;
        private readonly JObject vars;

    public PushController(JObject vars, AppSettings config, Serilog.ILogger logger)
    {
        this.vars = vars;
        this.config = config;
        this.logger = logger;
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
        foreach (Record record in records)
        {
            logger.Information($"Topic: {record.topic}");
            foreach (PVqts vqts in record.vqts)
            {
                logger.Information($"----vqts");
                logger.Information($"--------tag:  {vqts.tag}");
                logger.Information($"--------type:  {vqts.type}");
                logger.Information($"------------v:  {vqts.vqt.v}");
                logger.Information($"------------q:  {vqts.vqt.q}");
                logger.Information($"------------t:  {vqts.vqt.t}");
                logger.Information("");

            }
        }
        return Ok (new {status = "OK"});
    }
}
