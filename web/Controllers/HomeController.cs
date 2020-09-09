using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using web.Models;

namespace web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ApiController : Controller
    {
		private readonly ILogger<ApiController> logger;

		public ApiController(ILogger<ApiController> logger)
        {
			this.logger = logger;
		}

        [Route("/api/data/{id}")]
        [HttpGet]
        public IActionResult GetData(string id)
        {
            return Json(new {
                id,
                foo = "bar",
                DateTime.Now
            });
        }
    }
}
