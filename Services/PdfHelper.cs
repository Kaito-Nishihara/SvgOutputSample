
using SvgOutputSample.Models;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;

namespace SvgOutputSample.Services
{
    public class SvgTextAreaPoint
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public interface ISvgProcessingService
    {
        SvgPreviewModel ReplacePlaceholders<TModel>(string fileName, TModel replacementValues, 帳票種別 docType);
    }

        public class SvgProcessingService(IWebHostEnvironment hostingEnvironment) : ISvgProcessingService
        {
            private Dictionary<帳票種別, SvgPreviewModel> docTypeSettings = new Dictionary<帳票種別, SvgPreviewModel>
        {
            { 帳票種別.Sample,new SvgPreviewModel{Orientation = PDF向き.縦,PageSize = PDFサイズ.A4,FileName = "test"} },
        };
            /// <summary>
            /// SVGで設定しているPlaceholderを置換する
            /// </summary>
            /// <typeparam name="TModel"></typeparam>
            /// <param name="fileName"></param>
            /// <param name="replacementValues"></param>
            /// <returns></returns>
            public SvgPreviewModel ReplacePlaceholders<TModel>(string fileName, TModel replacementValues, 帳票種別 docType)
            {
                // TODO：実際はAzureStorageから取得
                var inputFilePath = Path.Combine(hostingEnvironment.WebRootPath, "svg", fileName);
                var svgContent = File.ReadAllText(inputFilePath);
                var svgDoc = XDocument.Parse(svgContent);
                var ns = svgDoc.Root?.GetDefaultNamespace();

                foreach (PropertyInfo property in typeof(TModel).GetProperties())
                {
                    var propertyValue = property.GetValue(replacementValues, null);
                    if (propertyValue is System.Collections.IEnumerable enumerable && !(propertyValue is string))
                    {
                        // Listや他のコレクションの場合
                        ReplaceEnumerablePlaceholders(svgDoc, enumerable, ns, property);
                    }
                    else
                    {
                        // 単一プロパティの場合
                        ReplaceSinglePlaceholder(svgDoc, property, propertyValue, ns!);
                    }
                }
                //帳票種別から帳票の詳細情報を取得
                var viewModel = GetDocTypeSettings(docType);
                viewModel.SvgContent = svgDoc.ToString();
                // 置換後のSVGの内容を返す
                return viewModel;
            }

            /// <summary>
            /// 帳票タイプから帳票のサイズ、向き、名前を取得
            /// </summary>
            /// <param name="docType"></param>
            /// <returns></returns>
            private SvgPreviewModel GetDocTypeSettings(帳票種別 docType)
            {
                docTypeSettings.TryGetValue(docType, out var viewModel);
                return viewModel!;
            }

            /// <summary>
            /// Listやコレクションのプレースホルダを置換
            /// </summary>
            private void ReplaceEnumerablePlaceholders(XDocument svgDoc, System.Collections.IEnumerable enumerable, XNamespace? ns, PropertyInfo property)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    foreach (PropertyInfo itemProperty in item.GetType().GetProperties())
                    {
                        var displayName = GetDisplayName(itemProperty);
                        var listPlaceholder = $"{displayName}_{index}";
                        var itemValue = itemProperty.GetValue(item)?.ToString() ?? string.Empty;
                        var svgTextAnchorAttr = itemProperty.GetCustomAttribute<SvgTextAnchorAttribute>();

                        SetSvgTextAttributes(listPlaceholder, svgTextAnchorAttr, svgDoc, ns!);
                        UpdateTextElement(svgDoc, listPlaceholder, itemValue, ns!);
                    }
                    index++;
                }
            }

            /// <summary>
            /// 単一のプロパティのプレースホルダを置換
            /// </summary>
            private void ReplaceSinglePlaceholder(XDocument svgDoc, PropertyInfo property, object? propertyValue, XNamespace ns)
            {
                var displayName = GetDisplayName(property);
                var replacementValue = propertyValue?.ToString() ?? string.Empty;
                var isTextArea = property.Name.Contains("TextArea");
                if (isTextArea)
                {
                    UpdateTextElement(svgDoc, displayName, replacementValue, ns, isTextArea);
                }
                else
                {
                    var svgTextAnchorAttr = property.GetCustomAttribute<SvgTextAnchorAttribute>();
                    SetSvgTextAttributes(displayName, svgTextAnchorAttr, svgDoc, ns);
                    UpdateTextElement(svgDoc, displayName, replacementValue, ns);
                }
            }

            /// <summary>
            /// SVGのText要素に属性を付与する
            /// </summary>
            private void SetSvgTextAttributes(string id, SvgTextAnchorAttribute? svgTextAnchorAttr, XDocument svgDoc, XNamespace ns)
            {
                var textElement = svgDoc.Descendants(ns + "text")
                                        .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Text");
                if (textElement == null) return;
                if (svgTextAnchorAttr != null)
                {
                    textElement.SetAttributeValue("text-anchor", svgTextAnchorAttr.TextAnchor);
                }
                var point = GetTextAreaPoint(id, svgDoc, ns);
                if (point != null)
                {
                    textElement.SetAttributeValue("textLength", point.Width);
                }
            }

            /// <summary>
            /// SVG要素のテキストを更新
            /// </summary>
            private void UpdateTextElement(XDocument svgDoc, string placeholder, string replacementValue, XNamespace ns, bool isTextArea = false)
            {
                var elementType = isTextArea ? "TextArea" : "Text";
                var textElement = svgDoc.Descendants(ns + "text")
                                        .FirstOrDefault(e => (string)e.Attribute("id") == $"{placeholder}_{elementType}");

                if (textElement != null)
                {
                    // 既存の <tspan> 要素を探す
                    var tspanElement = textElement.Element(ns + "tspan");

                    if (tspanElement != null)
                    {
                        // <tspan> の内容を置換
                        tspanElement.Value = replacementValue;
                    }
                    else
                    {
                        // <tspan> が存在しない場合、新しく追加
                        var tspan = new XElement(ns + "tspan", replacementValue);

                        // 必要に応じて座標などの属性を設定
                        tspan.SetAttributeValue("x", textElement.Attribute("x")?.Value ?? "0");
                        tspan.SetAttributeValue("y", textElement.Attribute("y")?.Value ?? "0");

                        // 新しい <tspan> を <text> に追加
                        textElement.Add(tspan);
                    }
                }
            }


                /// <summary>
                /// 表示範囲の幅と高さを取得
                /// </summary>
                private SvgTextAreaPoint? GetTextAreaPoint(string id, XDocument svgDoc, XNamespace ns)
            {
                return GetRectDimensions(id, svgDoc, ns)
                    ?? GetPathDimensions(id, svgDoc, ns)
                    ?? GetGroupElementDimensions(id, svgDoc, ns)
                    ?? GetPathDimensionsG(id, svgDoc, ns);
            }

            /// <summary>
            /// rect要素から幅と高さを取得
            /// </summary>
            /// <param name="id"></param>
            /// <param name="svgDoc"></param>
            /// <param name="ns"></param>
            /// <returns></returns>
            private SvgTextAreaPoint? GetRectDimensions(string id, XDocument svgDoc, XNamespace ns)
            {
                var rectElement = svgDoc.Descendants(ns + "rect")
                                        .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");
                return rectElement != null
                    ? new SvgTextAreaPoint
                    {
                        Width = float.Parse(rectElement.Attribute("width")?.Value ?? "0"),
                        Height = float.Parse(rectElement.Attribute("height")?.Value ?? "0")
                    }
                    : null;
            }

            /// <summary>
            /// path要素から幅と高さを取得
            /// </summary>
            /// <param name="id"></param>
            /// <param name="svgDoc"></param>
            /// <param name="ns"></param>
            /// <returns></returns>
            private SvgTextAreaPoint? GetPathDimensions(string id, XDocument svgDoc, XNamespace ns)
            {
                var pathElement = svgDoc.Descendants(ns + "path")
                                        .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");

                if (pathElement != null)
                {
                    var pathData = pathElement.Attribute("d")?.Value;
                    return !string.IsNullOrEmpty(pathData) ? ParsePathData(pathData) : null;
                }

                return null;
            }

            private SvgTextAreaPoint? GetPathDimensionsG(string id, XDocument svgDoc, XNamespace ns)
            {
                // 指定した id を持つ <g> 要素を取得
                var groupElement = svgDoc.Descendants(ns + "g")
                                         .FirstOrDefault(e => (string)e.Attribute("id") == $"{id}_Area");

                if (groupElement != null)
                {
                    // <g> 要素内の最初の <path> 要素を取得
                    var pathElement = groupElement.Descendants(ns + "path").FirstOrDefault();

                    if (pathElement != null)
                    {
                        // d 属性の値を取得
                        var pathData = pathElement.Attribute("d")?.Value;

                        // pathData が存在する場合、解析して結果を返す
                        return !string.IsNullOrEmpty(pathData) ? ParsePathData(pathData) : null;
                    }
                }

                // <path> 要素または d 属性が存在しない場合は null を返す
                return null;
            }


            /// <summary>
            /// g要素から幅と高さを取得
            /// </summary>
            /// <param name="id"></param>
            /// <param name="svgDoc"></param>
            /// <param name="ns"></param>
            /// <returns></returns>
            private SvgTextAreaPoint? GetGroupElementDimensions(string id, XDocument svgDoc, XNamespace? ns)
            {
                var groupElement = svgDoc.Descendants(ns! + "g")
                                         .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");

                if (groupElement == null) return null;

                var rectElement = groupElement.Descendants(ns! + "rect").FirstOrDefault();
                if (rectElement != null)
                {
                    return new SvgTextAreaPoint
                    {
                        Width = float.Parse(rectElement.Attribute("width")?.Value ?? "0"),
                        Height = float.Parse(rectElement.Attribute("height")?.Value ?? "0")
                    };
                }

                var pathElement = groupElement.Descendants(ns! + "path").FirstOrDefault();
                return pathElement != null ? ParsePathData(pathElement.Attribute("d")?.Value!) : null;
            }
            /// <summary>
            /// パスデータを解析し、長方形の頂点を取得する
            /// </summary>
            private SvgTextAreaPoint? ParsePathData(string pathData)
            {
                try
                {
                    // パスデータを解析して座標値を取得
                    var segments = pathData.Replace("M", "")
                                           .Replace("H", " ")
                                           .Replace("V", " ")
                                           .Replace("Z", "")
                                           .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(float.Parse)
                                           .ToArray();

                    if (segments.Length < 4)
                    {
                        // 座標が足りない場合はnullを返す
                        return null;
                    }

                    // X座標とY座標を分離
                    var xCoords = segments.Where((_, index) => index % 2 == 0).ToArray(); // 偶数インデックス
                    var yCoords = segments.Where((_, index) => index % 2 != 0).ToArray(); // 奇数インデックス

                    if (xCoords.Length == 0 || yCoords.Length == 0)
                    {
                        return null;
                    }

                    // 幅と高さを計算
                    float width = xCoords.Max() - xCoords.Min();
                    float height = yCoords.Max() - yCoords.Min();

                    return new SvgTextAreaPoint
                    {
                        Width = width,
                        Height = height
                    };
                }
                catch
                {
                    // 解析失敗時はnullを返す
                    return null;
                }
            }

            public static string GetDisplayName(PropertyInfo property)
            {
                var displayNameAttribute = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                                   .Cast<DisplayNameAttribute>()
                                                   .FirstOrDefault();
                return displayNameAttribute != null ? displayNameAttribute.DisplayName : property.Name;
            }
        }

    }
}