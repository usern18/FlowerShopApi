
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FlowerShopApi.Controllers;

[ApiController]
[Route("api/reliability")]
public class ReliabilityController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReliabilityController> _logger;

    public ReliabilityController(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment,
        ILogger<ReliabilityController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("dependency")]
    public async Task<IActionResult> Dependency(
        [FromQuery] string mode = "ok",
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        _logger.LogInformation(
            "Dependency request, mode={Mode}", mode);

        switch (mode.ToLowerInvariant())
        {
            case "ok":
                return Ok(new
                {
                    status = "healthy"
                });

            case "fail":
                return StatusCode(503, new
                {
                    error = "Simulated service unavailable"
                });

            case "slow":
                await Task.Delay(
                    TimeSpan.FromSeconds(3),
                    cancellationToken);

                return Ok(new
                {
                    status = "slow response"
                });

            default:
                return BadRequest(new
                {
                    error = "Unknown mode"
                });
        }
    }

    [HttpGet("check")]
    public async Task<IActionResult> Check(
        [FromQuery] string mode = "ok",
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        if (mode is not ("ok" or "fail" or "slow"))
            return BadRequest(new { error = "Unknown mode" });

        var client = _httpClientFactory.CreateClient(
            "ReliabilityDemo");

        try
        {
            using var response = await client.GetAsync(
                $"/api/reliability/dependency?mode={mode}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(503, new
                {
                    status = "dependency_failed",
                    dependencyStatus = (int)response.StatusCode
                });
            }

            return Ok(new
            {
                status = "success",
                dependencyStatus = (int)response.StatusCode
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit Breaker is OPEN");

            return StatusCode(503, new
            {
                status = "circuit_open"
            });
        }
        catch (TimeoutRejectedException)
        {
            _logger.LogWarning("Dependency timeout");

            return StatusCode(504, new
            {
                status = "timeout"
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex, "Dependency request failed");

            return StatusCode(503, new
            {
                status = "dependency_unavailable"
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(408, new
            {
                status = "request_cancelled"
            });
        }
    }
}
