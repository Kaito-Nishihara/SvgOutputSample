namespace SvgOutputSample.Models
{
    public enum PDF向き
    {
        縦,
        横
    }
    public enum PDFサイズ
    {
        A4 =0,
    }
    public enum 帳票種別
    {
        Sample,
    }
    public class SvgPreviewModel
    {
        public string SvgContent { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public PDF向き Orientation { get; set; }
        public PDFサイズ PageSize { get; set; }
    }
}
