using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Dashboard.Application.Helpers;

/// <summary>
/// Converts AI-generated Markdown to safe HTML for display.
/// Handles the specific conventions the AI advisor uses:
///   - Numbered section headers (1. Title → h6)
///   - ## / ### headings
///   - Bullet items with any bullet char (-, *, •, ●, ▪, ◦), at any indent level
///   - Inline links [text](url) rendered with a favicon hint (data-favicon)
///   - Bold **text**
///   - Paragraphs
/// </summary>
public static class MarkdownHelper
{
    private static readonly Regex SectionRegex  = new(@"^(\d+)\.\s+(.+)$",                         RegexOptions.Compiled);
    private static readonly Regex BulletRegex   = new(@"^\s*[-*•●▪◦]\s+(.+)$",                     RegexOptions.Compiled);
    private static readonly Regex HrRegex       = new(@"^\s*[-*_]{3,}\s*$",                         RegexOptions.Compiled);
    private static readonly Regex LinkRegex     = new(@"\[([^\]]*)\]\((https?://[^\)\s]+)\)",       RegexOptions.Compiled);
    private static readonly Regex BoldRegex     = new(@"\*\*(.+?)\*\*",                             RegexOptions.Compiled);

    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var lines = markdown.Split('\n');
        var html  = new StringBuilder();
        bool inList      = false;
        bool inParagraph = false;

        void CloseList()      { if (inList)      { html.Append("</ul>"); inList      = false; } }
        void CloseParagraph() { if (inParagraph) { html.Append("</p>"); inParagraph = false; } }

        foreach (var rawLine in lines)
        {
            var line    = rawLine.TrimEnd();
            var trimmed = line.TrimStart();

            // Numbered section header — only when not indented (top-level AI section)
            if (line == trimmed)
            {
                var m = SectionRegex.Match(trimmed);
                if (m.Success)
                {
                    CloseParagraph();
                    CloseList();
                    html.Append($"<h6 class=\"fw-semibold mt-3 mb-1\">{m.Groups[1].Value}. {FormatInline(m.Groups[2].Value)}</h6>");
                    continue;
                }
            }

            // ### heading
            if (trimmed.StartsWith("### "))
            {
                CloseParagraph(); CloseList();
                html.Append($"<h6 class=\"fw-semibold mt-3 mb-1\">{FormatInline(trimmed[4..])}</h6>");
                continue;
            }

            // ## heading
            if (trimmed.StartsWith("## "))
            {
                CloseParagraph(); CloseList();
                html.Append($"<h5 class=\"fw-semibold mt-3 mb-2\">{FormatInline(trimmed[3..])}</h5>");
                continue;
            }

            // Horizontal rule — ---, ***, ___
            if (HrRegex.IsMatch(line))
            {
                CloseParagraph();
                CloseList();
                html.Append("<hr class=\"my-3\">");
                continue;
            }

            // Bullet item — any indentation, any bullet char
            var bullet = BulletRegex.Match(line);
            if (bullet.Success)
            {
                CloseParagraph();
                if (!inList) { html.Append("<ul>"); inList = true; }
                html.Append($"<li>{FormatInline(bullet.Groups[1].Value)}</li>");
                continue;
            }

            // Empty line — close open blocks
            if (string.IsNullOrWhiteSpace(line))
            {
                CloseParagraph();
                CloseList();
                continue;
            }

            // Regular text — append to current paragraph
            CloseList();
            if (!inParagraph) { html.Append("<p>"); inParagraph = true; }
            else html.Append(' ');
            html.Append(FormatInline(trimmed));
        }

        CloseParagraph();
        CloseList();
        return html.ToString();
    }

    /// <summary>Applies inline formatting: links with favicon hints, then bold.</summary>
    private static string FormatInline(string text)
    {
        var sb  = new StringBuilder();
        int pos = 0;

        foreach (Match m in LinkRegex.Matches(text))
        {
            if (m.Index > pos)
                sb.Append(ApplyBold(WebUtility.HtmlEncode(text[pos..m.Index])));

            var linkText   = WebUtility.HtmlEncode(m.Groups[1].Value);
            var rawUrl     = m.Groups[2].Value;
            var host       = Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ? uri.Host : rawUrl;
            var encodedUrl = WebUtility.HtmlEncode(rawUrl);
            var favicon    = $"https://www.google.com/s2/favicons?sz=16&amp;domain={WebUtility.HtmlEncode(host)}";

            sb.Append($"<a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" data-favicon=\"{favicon}\">{linkText}</a>");
            pos = m.Index + m.Length;
        }

        if (pos < text.Length)
            sb.Append(ApplyBold(WebUtility.HtmlEncode(text[pos..])));

        return sb.ToString();
    }

    private static string ApplyBold(string html)
        => BoldRegex.Replace(html, "<strong>$1</strong>");
}
