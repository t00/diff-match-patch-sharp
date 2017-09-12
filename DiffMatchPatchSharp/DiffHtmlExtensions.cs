using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public static class DiffHtmlExtensions
    {
        public static string GetHtmlColor(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        public static string GetStyle(XElement element)
        {
            return element.Attribute(XName.Get("style"))?.Value;
        }

        public static void SetStyle(XElement element, IDictionary<string, string> newValues)
        {
            var existingStyle = GetStyle(element);
            var style = SetStyle(existingStyle, newValues);
            element.SetAttributeValue(XName.Get("style"), style);
        }

        public static string SetStyle(string css, IDictionary<string, string> newValues)
        {
            var style = ReadInlineStyle(css);
            foreach (var value in newValues)
            {
                style[value.Key] = value.Value;
            }
            return WriteInlineStyle(style);
        }

        public static IDictionary<string, string> ReadInlineStyle(XElement element)
        {
            return ReadInlineStyle(GetStyle(element));
        }

        public static IDictionary<string, string> ReadInlineStyle(string css)
        {
            var style = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(css))
            {
                return style;
            }
            var label = new StringBuilder();
            var value = new StringBuilder();
            var isLabel = true;
            var isQuote = false;
            var isStarted = false;
            var wasQuoted = false;
            foreach (var c in css)
            {
                if (isLabel)
                {
                    if (c == ':')
                    {
                        isStarted = false;
                        isLabel = false;
                    }
                    else if (c != ';' && (isStarted || !char.IsWhiteSpace(c)))
                    {
                        isStarted = true;
                        label.Append(c);
                    }
                }
                else
                {
                    if (c == '\'' && (!isStarted || isQuote))
                    {
                        isQuote = !isQuote;
                        isStarted = isQuote;
                        wasQuoted = true;
                    }
                    else if (c == ';' && !isQuote)
                    {
                        if (label.Length > 0 && value.Length > 0)
                        {
                            var v = value.ToString();
                            if (!wasQuoted)
                            {
                                v = v.TrimEnd();
                            }
                            style[label.ToString().TrimEnd()] = v;
                            label.Clear();
                            value.Clear();
                        }
                        isLabel = true;
                        isStarted = false;
                        wasQuoted = false;
                    }
                    else if(isStarted || !char.IsWhiteSpace(c))
                    {
                        isStarted = true;
                        value.Append(c);
                    }
                }
            }
            if (label.Length > 0 && value.Length > 0)
            {
                var v = value.ToString();
                if (!wasQuoted)
                {
                    v = v.TrimEnd();
                }
                style[label.ToString()] = v;
            }
            return style;
        }

        public static string WriteInlineStyle(IDictionary<string, string> style)
        {
            var sb = new StringBuilder();
            foreach (var kvp in style.Where(x => x.Value != null))
            {
                sb.Append(kvp.Key);
                sb.Append(':');
                var hasSpaces = kvp.Value.Any(char.IsWhiteSpace);
                if (hasSpaces)
                {
                    sb.Append("'");
                }
                sb.Append(kvp.Value.Replace("'", "\'"));
                if (hasSpaces)
                {
                    sb.Append("'");
                }
                sb.Append(';');
            }
            return sb.ToString();
        }

        public static bool AreStylesEqual(IDictionary<string, string> style1, IDictionary<string, string> style2, ICollection<string> compareOnly = null, StringComparison valueComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (style1 == style2)
            {
                return true;
            }
            if (style1 == null || style2 == null)
            {
                return false;
            }

            var keysTotTest = style1.Keys.Union(style2.Keys).Where(x => compareOnly == null || compareOnly.Contains(x));
            foreach (var key in keysTotTest)
            {
                if(style1.TryGetValue(key, out string value1) != style2.TryGetValue(key, out string value2))
                {
                    return false;
                }
                if (!string.Equals(value1, value2, valueComparison))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
