using Microsoft.AspNetCore.Mvc;

namespace PointerStar.Server.Controllers;

[ApiController]
[Route("api/client-config")]
public class ClientConfigController(IConfiguration configuration) : ControllerBase
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    [HttpGet]
    public IActionResult Get()
        => Ok(
            new
            {
                ApplicationInsightsConnectionString = _configuration["ApplicationInsights:ConnectionString"],
                AppVersion = typeof(Program).Assembly.GetName().Version?.ToString(3)
            });
}
