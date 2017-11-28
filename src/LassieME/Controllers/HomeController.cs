using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord.OAuth2;
using Microsoft.AspNetCore.Mvc;
using LassieME.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Internal;

namespace LassieME.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //[Route("/signin-discord")]
        //public async Task<IActionResult> SignInDiscord(string state, string code)
        //{
        //    try
        //    {
        //        var userResult = await HttpContext.AuthenticateAsync().ConfigureAwait(false);
        //        var user = userResult.Principal;
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return Redirect("/GBot/Submit");
        //}

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
