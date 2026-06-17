using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Benchmark;
using BattleTanks_Backend.Application.Services;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BenchmarkController : ControllerBase
{
    private readonly QueryBenchmarkService _benchmarkService;

    public BenchmarkController(QueryBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    [HttpGet("run")]
    [ProducesResponseType(typeof(BenchmarkResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenchmarkResultDto>> RunBenchmarks()
    {
        try
        {
            var results = await _benchmarkService.RunAllBenchmarksAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error al ejecutar los benchmarks",
                error = ex.Message
            });
        }
    }

    [HttpGet("redis")]
    [ProducesResponseType(typeof(BenchmarkResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenchmarkResultDto>> RunRedisBenchmarks()
    {
        try
        {
            var results = await _benchmarkService.RunRedisBenchmarksAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error al ejecutar los benchmarks de Redis",
                error = ex.Message
            });
        }
    }
}
