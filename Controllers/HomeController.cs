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
            string inputFilePath = "���Ϗ��i���z����j.svg";

            // �u������l�����I�u�W�F�N�g���쐬
            var replacementValues = new Estimate();

            // SvgProcessor���g���Ēu�����������s

            string svgContent = _processor.ReplacePlaceholdersInSvg(inputFilePath, replacementValues);

            // SVG�R���e���c��HTTP���X�|���X�Ƃ��ĕԂ�
            return View((object)svgContent);
        }
    }
}
