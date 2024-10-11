using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
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

        /// <summary>
        /// SVGで設定しているPlaceholderを置換する
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="replacementValues"></param>
        /// <returns></returns>
        public string ReplacePlaceholdersInSvg<TModel>(string fileName, TModel replacementValues)
        {
            // TODO：実際はAzureStorageから取得
            var inputFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "svg", fileName);
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
                    ReplaceSinglePlaceholder(svgDoc, property, propertyValue, ns);
                }
            }

            // 置換後のSVGの内容を返す
            return svgDoc.ToString();
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

                    SetSvgTextAttributes(listPlaceholder, svgTextAnchorAttr, svgDoc, ns);
                    UpdateTextElement(svgDoc, listPlaceholder, itemValue, ns);
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

            if (property.Name.Contains("TextArea"))
            {
                UpdateTextAreaElement(svgDoc, displayName, replacementValue, ns);                
            }
            else
            {
                var svgTextAnchorAttr = property.GetCustomAttribute<SvgTextAnchorAttribute>();
                SetSvgTextAttributes(displayName, svgTextAnchorAttr, svgDoc, ns);
                UpdateTextElement(svgDoc, displayName, replacementValue, ns);
            }
        }

        /// <summary>
        /// DisplayName属性が設定されている場合はその値を、設定されていない場合はプロパティ名を取得
        /// </summary>
        private string GetDisplayName(PropertyInfo property)
        {
            var displayNameAttribute = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                               .Cast<DisplayNameAttribute>()
                                               .FirstOrDefault();
            return displayNameAttribute != null ? displayNameAttribute.DisplayName : property.Name;
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
        /// テキストエリアの改行処理
        /// </summary>
        private void AdjustTextArea(string displayName, string value, XDocument svgDoc, XNamespace ns)
        {
            var textElement = svgDoc.Descendants(ns + "text")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{displayName}_Text");
            var point = GetTextAreaPoint(displayName, svgDoc, ns);

            if (textElement == null || point == null) return;

            var fontSize = float.Parse(textElement.Attribute("font-size")?.Value ?? "30");
            var charSize = fontSize;
            var maxCharsPerLine = (int)(point.Width / charSize);
            var maxLines = (int)(point.Height / charSize);
            var lines = WrapText(value, maxCharsPerLine);

            while (lines.Count > maxLines)
            {
                fontSize *= 0.95f;
                charSize = fontSize;
                maxCharsPerLine = (int)(point.Width / charSize);
                maxLines = (int)(point.Height / charSize);
                lines = WrapText(value, maxCharsPerLine);
            }

            textElement.SetAttributeValue("font-size", fontSize.ToString());
            textElement.RemoveNodes();

            float y = float.Parse(textElement.Attribute("y")?.Value ?? "0");
            for (int i = 0; i < lines.Count; i++)
            {
                textElement.Add(new XElement(ns + "tspan",
                    new XAttribute("x", 0),
                    new XAttribute("y", y + i * fontSize),
                    lines[i]));
            }
        }

        /// <summary>
        /// 指定された文字列を最大文字数に基づいて改行し、複数行のリストとして返します。
        /// </summary>
        private List<string> WrapText(string value, int maxCharsPerLine)
        {
            var lines = new List<string>();
            var currentLine = string.Empty;

            foreach (char c in value)
            {
                if (currentLine.Length + 1 > maxCharsPerLine)
                {
                    lines.Add(currentLine);
                    currentLine = string.Empty;
                }
                currentLine += c;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        /// <summary>
        /// SVG要素のテキストを更新
        /// </summary>
        private void UpdateTextElement(XDocument svgDoc, string placeholder, string replacementValue, XNamespace ns)
        {
            var textElement = svgDoc.Descendants(ns + "text")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{placeholder}_Text");
            if (textElement != null)
            {
                textElement.Value = replacementValue;
            }
        }

        private void UpdateTextAreaElement(XDocument svgDoc, string placeholder, string replacementValue, XNamespace ns)
        {
            var textElement = svgDoc.Descendants(ns + "text")
                                    .FirstOrDefault(e => (string)e.Attribute("id")! == $"{placeholder}_TextArea");
            if (textElement != null)
            {
                textElement.Value = replacementValue;
            }
        }

        /// <summary>
        /// 表示範囲の幅と高さを取得
        /// </summary>
        private SvgTextAreaPoint? GetTextAreaPoint(string id, XDocument svgDoc, XNamespace ns)
        {
            return GetRectDimensions(id, svgDoc, ns)
                ?? GetPathDimensions(id, svgDoc, ns)
                ?? GetGroupElementDimensions(id, svgDoc, ns);
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

        /// <summary>
        /// g要素から幅と高さを取得
        /// </summary>
        /// <param name="id"></param>
        /// <param name="svgDoc"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        private SvgTextAreaPoint? GetGroupElementDimensions(string id, XDocument svgDoc, XNamespace? ns)
        {
            var groupElement = svgDoc.Descendants(ns + "g")
                                     .FirstOrDefault(e => (string)e.Attribute("id")! == $"{id}_Area");

            if (groupElement == null) return null;

            var rectElement = groupElement.Descendants(ns + "rect").FirstOrDefault();
            if (rectElement != null)
            {
                return new SvgTextAreaPoint
                {
                    Width = float.Parse(rectElement.Attribute("width")?.Value ?? "0"),
                    Height = float.Parse(rectElement.Attribute("height")?.Value ?? "0")
                };
            }

            var pathElement = groupElement.Descendants(ns + "path").FirstOrDefault();
            return pathElement != null ? ParsePathData(pathElement.Attribute("d")?.Value!) : null;
        }

        /// <summary>
        /// パスデータを解析し、長方形の頂点を取得する
        /// </summary>
        private SvgTextAreaPoint? ParsePathData(string pathData)
        {
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
                    float x1 = segments[0], y1 = segments[1];
                    float x2 = segments[2], y2 = segments[3];

                    return new SvgTextAreaPoint
                    {
                        Width = Math.Abs(x1 - x2),
                        Height = Math.Abs(y1 - y2)
                    };
                }
            }
            catch
            {
                // 解析失敗時はnullを返す
                return null;
            }

            return null;
        }
    }
}
