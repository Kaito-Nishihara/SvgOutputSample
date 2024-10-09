using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using SvgOutputSample.Models;
using SvgOutputSample.Services;
using System.Diagnostics;

namespace SvgOutputSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SvgProcessor _processor;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _processor = new SvgProcessor(hostingEnvironment);
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

        public IActionResult GenerateSvg()
        {
            string inputFilePath = "見積書（金額あり）.svg";

            // 置換する値を持つオブジェクトを作成
            var replacementValues = new Estimate();

            // SvgProcessorを使って置換処理を実行

            string svgContent = _processor.ReplacePlaceholdersInSvg(inputFilePath, replacementValues);

            // SVGコンテンツをHTTPレスポンスとして返す
            return View((object)svgContent);
        }
    }
}
