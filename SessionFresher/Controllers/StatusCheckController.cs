using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SessionFresher.Services;

namespace SessionFresher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusCheckController(IStatusService statusService) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(statusService.IsHealthy());
        }
    }
}
