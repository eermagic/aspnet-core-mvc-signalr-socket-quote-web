using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using TeachQuoteWeb.Hubs;
using TeachQuoteWeb.Models;

namespace TeachQuoteWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IHubContext<QuoteHub> quoteHubContext)
        {
            _logger = logger;
            QuoteUtil.hubContext = quoteHubContext;
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

        /// <summary>
        /// 訂閱報價
        /// </summary>
        /// <param name="inModel"></param>
        /// <returns></returns>
        public IActionResult RequestQuote(Dictionary<string, string> inModel)
        {
            Dictionary<string, string> outModel = new Dictionary<string, string>();
            outModel["ErrMsg"] = "";

            // 訂閱報價需求
            string msg = QuoteUtil.GetRequestSymbol(inModel["HubConnId"], inModel["Symbol"]);
            if (msg != "")
            {
                outModel["ErrMsg"] = msg;
            }

            return Json(outModel);
        }
    }
}