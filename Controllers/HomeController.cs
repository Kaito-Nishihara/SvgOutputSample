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

            // 置換する値を持つオブジェクトを作成
            var replacementValues = new TestViewModel();

            // SvgProcessorを使って置換処理を実行

            var svgContent = _processor.ReplacePlaceholders(inputFilePath, replacementValues,帳票種別.Sample);

            // SVGコンテンツをHTTPレスポンスとして返す
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
            Orientation = Orientation.Portrait, // 縦方向
            PaperSize = DinkToPdf.PaperKind.A4,           // A4サイズのPDF
            
        },
                Objects = {
                new ObjectSettings
                {
                    HtmlContent = svgContent, // SVGを含むHTMLを変換
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            // PDFに変換
            byte[] pdf = _converter.Convert(pdfDoc);

            // PDFをクライアントにダウンロードさせる
            return File(pdf, "application/pdf", "document.pdf");
        }
        
    }
    public class SvgModel
    {
        public string SvgContent { get; set; }
    }
}
