using System.Text;
using UglyToad.PdfPig;

namespace ReadAloud;

/// <summary>Pulls readable text out of a PDF, one block per page.</summary>
public static class PdfText
{
    public static int PageCount(string path)
    {
        using var document = PdfDocument.Open(path);
        return document.NumberOfPages;
    }

    /// <summary>Text of pages <paramref name="from"/> to <paramref name="to"/> (1-based, clamped to the document).</summary>
    public static string Extract(string path, int from, int to)
    {
        using var document = PdfDocument.Open(path);
        from = Math.Clamp(from, 1, document.NumberOfPages);
        to = Math.Clamp(to, from, document.NumberOfPages);

        var text = new StringBuilder();
        for (var number = from; number <= to; number++)
        {
            var words = document.GetPage(number).GetWords().Select(word => word.Text);
            text.AppendLine($"Page {number}.")
                .AppendLine(string.Join(' ', words))
                .AppendLine();
        }
        return text.ToString();
    }
}
