using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LassieME.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security;
using System.Text;
using GiantBomb.Api;
using KiteBotCore;
using KiteBotCore.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExtendedGiantBombRestClient = ExtendedGiantBombClient.ExtendedGiantBombRestClient;


namespace LassieME.Controllers
{
    public class GBotController : Controller
    {
        private readonly KiteBotDbContext _context;

        public GBotController(KiteBotDbContext context)
        {
            _context = context;
        }

        // GET: GBot/LinkAccounts/{sessionId}
        //[Authorize]
        [RequireHttps]
        public async Task<IActionResult> LinkAccounts()
        {
            if ((await HttpContext.AuthenticateAsync().ConfigureAwait(true)).Succeeded)
                return Redirect("/GBot/Submit");
            else
                await HttpContext.ChallengeAsync("Discord").ConfigureAwait(true);
            return View();
        }
        // GET: GBot/Submit/
        [Authorize,RequireHttps]
        public IActionResult Submit()
        {
            var model = new SubmitViewModel();
            
            return View(model);
        }

        static readonly TimeSpanSemaphore appRateLimit = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(1));
        // GET: GBot/Result
        [Authorize,RequireHttps,ValidateAntiForgeryToken]
        public async Task<IActionResult> Result(SubmitViewModel model)
        {
            //First fetch the actual api key from https://www.giantbomb.com/app/GBOT/get-result?regCode={CODE}&format=json
            ResultViewModel resultViewModel = new ResultViewModel{SuccessString = "Success! Please wait a couple minutes for it to take effect"};
            string s = "";
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("User-Agent",
                    "KiteBotCore 1.1 GB Unofficial Discord Bot by GB user LassieME");
                
                var jsonString = await appRateLimit.RunAsync(async () => await client.GetStringAsync($"https://www.giantbomb.com/app/GBOT/get-result?regCode={model.GBkey}&format=json"));

                var definition = new {Status = "", CreationTime = "", RegToken = "", CustomerId = ""};
                var jsonObject = JsonConvert.DeserializeAnonymousType(jsonString, definition);
                if (jsonObject.Status == "success")
                {
                    s = jsonObject.RegToken;
                }
                else
                {
                    resultViewModel.SuccessString = "Invalid GB code, please try again";
                    return View(resultViewModel);
                }
            }
            bool isPremium = true;
            //Then test it against a premium video to see if it throws an error or if it succeeds
            using (ExtendedGiantBombRestClient gbClient = new ExtendedGiantBombRestClient(s))
            {
                try
                {
                    var video = await gbClient.GetVideoAsync(3267, new[] {"url", "name", "id"});
                }
                catch (GiantBombApiException)
                {
                    isPremium = false;
                    resultViewModel.SuccessString = "Code is valid, but account is not a premium account";
                }
            }
            if (model.StoreKey)
            {
                //When that succeeds or fails, encrypt the apitoken with RSA using a public key, and add it to the database along with Premium status
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlStringCustom(System.IO.File.ReadAllText("public-rsa-key.xml"));
                    var encryptedTokenRawData = rsa.Encrypt(Convert.FromBase64String(s), false);
                    var encryptedToken = Convert.ToBase64String(encryptedTokenRawData);
                    var userId = ulong.Parse(HttpContext.User.Identities.First(x => x.AuthenticationType == "Discord")
                        .Claims.First().Value);
                    var user = await _context.Users.FindAsync((long) userId);
                    user.Premium = isPremium;
                    user.RegToken = encryptedToken;
                    user.PremiumLastCheckedAt = null;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var userId = ulong.Parse(HttpContext.User.Identities.First(x => x.AuthenticationType == "Discord")
                    .Claims.First().Value);
                var user = await _context.Users.FindAsync((long)userId);
                user.Premium = isPremium;
                user.PremiumLastCheckedAt = DateTimeOffset.UtcNow;
                user.RegToken = null;
                await _context.SaveChangesAsync();
            }

            return View(resultViewModel);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public static class HelperExtensions
    {
        public static void FromXmlStringCustom(this RSACryptoServiceProvider rsa, string xmlString)
        {
            var parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }
    }
}