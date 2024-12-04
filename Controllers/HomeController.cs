using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using SvgOutputSample.Models;
using SvgOutputSample.Services;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Reflection.Metadata;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.StyledXmlParser.Jsoup;
using iText.Html2pdf;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Layout.Element;
using iText.Svg.Converter;
using static SvgOutputSample.Services.SvgTextAreaPoint;

namespace SvgOutputSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISvgProcessingService _processor;
        private readonly IConverter _converter;
        readonly IWebHostEnvironment _hostingEnvironment;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment, IConverter converter)
        {
            _processor = new SvgProcessingService(hostingEnvironment);
            _converter = converter;
            _hostingEnvironment = hostingEnvironment;
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
            string inputFilePath = "test.svg";

            // �u������l�����I�u�W�F�N�g���쐬
            var replacementValues = new TestViewModel();

            // SvgProcessor���g���Ēu�����������s

            var svgContent = _processor.ReplacePlaceholders(inputFilePath, replacementValues,���[���.Sample);

            // SVG�R���e���c��HTTP���X�|���X�Ƃ��ĕԂ�
            return View(svgContent);
        }

        [HttpPost]
        public IActionResult DownloadPdf([FromBody] SvgModel model)
        {
            var inputFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "svg", "a4_size_svg.svg");
            var svgContent = System.IO.File.ReadAllText(inputFilePath);
            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(Path.Combine(Directory.GetCurrentDirectory(), "libwkhtmltox.dll"));
            //string styledSvgContent = model.SvgContent.Replace("<svg", "<svg style='width: 270mm; height: 380mm; '");
            var pdfDoc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
            Orientation = Orientation.Portrait, // �c����
            PaperSize = DinkToPdf.PaperKind.A4,           // A4�T�C�Y��PDF
            
        },
                Objects = {
                new ObjectSettings
                {
                    HtmlContent = svgContent, // SVG���܂�HTML��ϊ�
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            // PDF�ɕϊ�
            byte[] pdf = _converter.Convert(pdfDoc);

            // PDF���N���C�A���g�Ƀ_�E�����[�h������
            return File(pdf, "application/pdf", "document.pdf");
        }
        
    }
    public class SvgModel
    {
        public string SvgContent { get; set; }
    }
}
