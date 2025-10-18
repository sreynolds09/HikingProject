using Microsoft.AspNetCore.Mvc;

namespace HikingProject.Controllers.MVC
{
    [ApiExplorerSettings(IgnoreApi = true)] // hides from Swagger
    public class HomeController : Controller
    {

        // About Me page
        [Route("About")]
        public IActionResult About()
        {
            return View(); // /Views/Home/About.cshtml
        }
    }
}
