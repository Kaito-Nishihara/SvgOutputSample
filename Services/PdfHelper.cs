using System;
using System.ComponentModel;

using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using SvgOutputSample.Models;

namespace SvgOutputSample.Services
{
    public class SvgTextAreaPoint
    {
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class SvgProcessor
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public SvgProcessor(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public string ReplacePlaceholdersInSvg<TModel>(string fileName, TModel replacementValues)
        {
            // SVGファイルが格納されているディレクトリのパスを取得
            var inputFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "svg", fileName);

            // SVGファイルを読み込む
            var svgContent = File.ReadAllText(inputFilePath);


            // オブジェクトのプロパティを取得して置換
            foreach (PropertyInfo property in typeof(TModel).GetProperties())
            {
                // DisplayName属性をチェック
                var displayNameAttribute = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                                   .Cast<DisplayNameAttribute>()
                                                   .FirstOrDefault();
                // DisplayName属性があればその値を使用、なければプロパティ名を使用
                string placeholderName = displayNameAttribute != null ? displayNameAttribute.DisplayName : property.Name;
                string placeholder = $"%{placeholderName}%";
                var propertyValue = property.GetValue(replacementValues, null);
                
                if (propertyValue is System.Collections.IEnumerable enumerable && !(propertyValue is string))
                {
                    // Listや他のコレクションの場合
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        foreach (PropertyInfo itemProperty in item.GetType().GetProperties())
                        {
                            var itemDisplayNameAttribute = itemProperty.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                                                       .Cast<DisplayNameAttribute>()
                                                                       .FirstOrDefault();
                            string itemPlaceholderName = itemDisplayNameAttribute != null ? itemDisplayNameAttribute.DisplayName : itemProperty.Name;
                            string listPlaceholder = $"%{itemPlaceholderName}[{index}]%";
                            string itemValue = itemProperty.GetValue(item)?.ToString() ?? string.Empty;
                            var svgTextAnchorAttr = itemProperty.GetCustomAttribute<SvgTextAnchorAttribute>();
                            svgContent = SetSvgTextAttributes($"{itemPlaceholderName}[{index}]", svgContent, svgTextAnchorAttr);                            
                            svgContent = svgContent.Replace(listPlaceholder, itemValue);
                        }
                        index++;
                    }
                }
                else
                {
                    // 通常のオブジェクトの場合
                    var replacementValue = propertyValue?.ToString() ?? string.Empty;
                    if (property.Name.Contains("Text"))
                    {
                        //改行あり
                        svgContent = AdjustTextInSvg(placeholderName, svgContent, replacementValue, placeholder);
                    }
                    else
                    {
                        //属性を付与
                        var svgTextAnchorAttr = property.GetCustomAttribute<SvgTextAnchorAttribute>();
                        svgContent = SetSvgTextAttributes(placeholderName, svgContent,svgTextAnchorAttr);
                        svgContent = svgContent.Replace(placeholder, replacementValue);
                    }
                    
                }
            }

            // 置換後のSVGの内容を返す
            return svgContent;
        }

        public string SetSvgTextAttributes(string textId, string svgContent,  SvgTextAnchorAttribute? svgTextAnchorAttr)
        {
            // SVGデータを解析
            var svgDoc = XDocument.Parse(svgContent);
            var ns = svgDoc!.Root!.GetDefaultNamespace();

            // text要素をIDで取得
            var textElement = svgDoc.Descendants(ns + "text")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{textId}_Text");
            if(textElement is null)
            {
                return svgDoc.ToString();
            }
            if (svgTextAnchorAttr is not null)
            {
                textElement.SetAttributeValue("text-anchor", svgTextAnchorAttr.TextAnchor);
            }

            // SvgLengthがある場合、その属性を付与
            var point = GetDimensionsFromSvg(textId, svgContent, svgDoc, ns);
            if (point != null)
            {
                textElement.SetAttributeValue("textLength", point?.Width);
            }
            return svgDoc.ToString();
        }

        public string AdjustTextInSvg(string textId, string svgContent, string value,string placeholder)
        {
            // SVGデータを解析
            var svgDoc = XDocument.Parse(svgContent);
            var ns = svgDoc!.Root!.GetDefaultNamespace();
            // text要素をIDで取得
            var textElement = svgDoc.Descendants(ns + "text")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"_{textId}_");
            // テキストエリアの幅と高さを取得
            var point = GetDimensionsFromSvg(textId, svgContent, svgDoc, ns);
            if (textElement is null || point is null)
            {
                Console.WriteLine($"指定されたID _{textId}_ のテキスト要素が見つかりませんでした。");
                //要素がないもしくはテキスト範囲が取得できないのであればそのまま置換（改行なし）
                return svgContent.Replace(placeholder, value);
            }

            // テキスト内容とフォントサイズを取得
            var fontSize = float.Parse(textElement.Attribute("font-size")?.Value ?? "30");

            // フォントサイズを基に文字サイズを仮定
            var charSize = fontSize;

            // 横方向の最大文字数と縦方向の最大行数を計算
            var maxCharsPerLine = (int)(point.Width / charSize);
            var maxLines = (int)(point.Height / charSize);

            // 自動改行処理
            var lines = WrapText(value, maxCharsPerLine);

            // 行数がエリアに収まるようにフォントサイズを調整
            while (lines.Count > maxLines)
            {
                fontSize *= 0.95f;  // フォントサイズを縮小
                charSize = fontSize;
                maxCharsPerLine = (int)(point.Width / charSize );
                maxLines = (int)(point.Height / charSize);

                // 再度改行を計算
                lines = WrapText(value, maxCharsPerLine);
            }
            textElement.SetAttributeValue("font-size", fontSize.ToString());
            // 既存のテキストノードを削除
            textElement.RemoveNodes();

            // 各行に対応する<tspan>要素を作成
            float y = float.Parse(textElement.Attribute("y")?.Value ?? "0");
            for (int i = 0; i < lines.Count; i++)
            {
                textElement.Add(new XElement(ns + "tspan",
                    new XAttribute("x", 0),
                    new XAttribute("y", y + i * fontSize ),  // 行間を調整
                    lines[i]));
            }

            // 変更を反映してSVGを文字列に変換し返す
            return svgDoc.ToString();
        }

        public List<string> WrapText(string text, int maxCharsPerLine)
        {
            var lines = new System.Collections.Generic.List<string>();
            string currentLine = "";

            // 単語単位ではなく、1文字ずつ処理する
            foreach (char c in text)
            {
                // 現在の行が指定された長さを超える場合、新しい行に移動
                if (currentLine.Length + 1 > maxCharsPerLine)
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                // 現在の行に文字を追加
                currentLine += c;
            }

            // 最後の行を追加
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        public SvgTextAreaPoint? GetDimensionsFromSvg(string id, string svgContent, XDocument svgDoc, XNamespace ns)
        {
            // まず <rect> 要素を検索
            var rectDimensions = GetRectDimensions(id, svgDoc, ns);
            if (rectDimensions != null)
            {
                return rectDimensions;
            }

            // <rect> が見つからなかったら <path> を検索して幅と高さを取得
            var pathDimensions = GetPathDimensions(id, svgDoc, ns);
            if (pathDimensions != null)
            {
                return pathDimensions;
            }

            // さらに <g> 要素を検索して、その中の <rect> または <path> 要素を探索
            var groupDimensions = GetGroupElementDimensions(id, svgDoc, ns);
            if (groupDimensions != null)
            {
                return groupDimensions;
            }

            // どの要素も見つからなければ null を返す
            return null;
        }

        // <rect> 要素の幅と高さを取得するヘルパーメソッド
        private SvgTextAreaPoint? GetRectDimensions(string id, XDocument svgDoc, XNamespace ns)
        {
            var rectElement = svgDoc.Descendants(ns + "rect")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");
            if (rectElement != null)
            {
                return new SvgTextAreaPoint()
                {
                    Width = float.Parse(rectElement.Attribute("width")?.Value ?? "0"),
                    Height = float.Parse(rectElement.Attribute("height")?.Value ?? "0")
                };
            }
            return null;
        }

        // <path> 要素のパスデータを解析して幅と高さを取得するヘルパーメソッド
        private SvgTextAreaPoint? GetPathDimensions(string id, XDocument svgDoc, XNamespace ns)
        {
            var pathElement = svgDoc.Descendants(ns + "path")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");

            if (pathElement != null)
            {
                string pathData = pathElement.Attribute("d")?.Value!;

                if (!string.IsNullOrEmpty(pathData))
                {
                    // パスデータを解析して幅と高さを計算
                    return ParsePathData(pathData);
                }
            }
            return null;
        }

        // <g> 要素内の <rect> または <path> 要素を探索して幅と高さを取得するヘルパーメソッド
        private SvgTextAreaPoint? GetGroupElementDimensions(string id, XDocument svgDoc, XNamespace ns)
        {
            var groupElement = svgDoc.Descendants(ns + "g")
                                     .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");

            if (groupElement == null)
            {
                Console.WriteLine($"指定されたID {id} の <g> 要素が見つかりませんでした。");
                return null;
            }

            // <g> 要素内の <rect> を取得
            var rectElement = groupElement.Descendants(ns + "rect").FirstOrDefault();
            if (rectElement != null)
            {
                return new SvgTextAreaPoint()
                {
                    Width = float.Parse(rectElement.Attribute("width")?.Value ?? "0"),
                    Height = float.Parse(rectElement.Attribute("height")?.Value ?? "0")
                };
            }

            // <rect> がない場合、<path> 要素を取得してパスデータを解析
            var pathElement = groupElement.Descendants(ns + "path").FirstOrDefault();
            if (pathElement != null)
            {
                string pathData = pathElement.Attribute("d")?.Value!;

                if (!string.IsNullOrEmpty(pathData))
                {
                    return ParsePathData(pathData);
                }
            }

            return null;
        }


        /// <summary>
        /// パスデータを解析し、長方形の頂点を取得する
        /// </summary>
        /// <param name="pathData">パスデータ</param>
        /// <returns>長方形の頂点座標の配列</returns>
        public SvgTextAreaPoint? ParsePathData(string pathData)
        {
            //下記のような座標データを解析する
            //<path d='M1480 2274H137V2509H1480V2274Z' />
            try
            {
                var segments = pathData.Replace("M", "")
                                       .Replace("H", " ")
                                       .Replace("V", " ")
                                       .Replace("Z", "")
                                       .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(float.Parse)
                                       .ToArray();

                if (segments.Length == 8)
                {
                    // 長方形の4つの頂点を取得
                    float x1 = segments[0], y1 = segments[1];  // 右上
                    float x2 = segments[2], y2 = segments[3];  // 左下

                    // 幅と高さを計算                    
                    return new SvgTextAreaPoint()
                    {
                        Width = Math.Abs(x1 - x2),
                        Height = Math.Abs(y1 - y2)
                    };
                }
            }
            catch
            {
                // 解析に失敗した場合はnullを返す
                return null!;
            }
            return null!;
        }
    }
}
