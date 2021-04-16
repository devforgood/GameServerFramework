using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login.Controllers
{
    public class LoginController : Controller
    {
        //[HttpPost()]
        public async Task<IActionResult> Index()
        {
            await Task.Delay(1);

            return Json(new { Id = 1, Name = "Lee" });
        }
    }
}
