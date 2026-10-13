using System.Globalization;
using System.Text;
using loginlogout.Helpers;
using loginlogout.Models;

namespace loginlogout.Services;

public static class InvoicePdfGenerator
{
    private const int FirstPageItemLimit = 20;
    private const int ContinuationPageItemLimit = 30;

    public static byte[] Generate(OrderDetailsViewModel model)
    {
        var items = model.Items.ToList();
        var pages = new List<IReadOnlyList<OrderItem>>();
        if (items.Count == 0)
        {
            pages.Add(Array.Empty<OrderItem>());
        }
        else
        {
            pages.Add(items.Take(FirstPageItemLimit).ToList());
            items = items.Skip(FirstPageItemLimit).ToList();
            while (items.Count > 0)
            {
                pages.Add(items.Take(ContinuationPageItemLimit).ToList());
                items = items.Skip(ContinuationPageItemLimit).ToList();
            }
        }

        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Array.Empty<byte>(),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
        };

        var pageObjectIds = new List<int>();
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var pageObjectId = objects.Count + 1;
            var contentObjectId = pageObjectId + 1;
            pageObjectIds.Add(pageObjectId);
            objects.Add(Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectId} 0 R >>"));
            var content = BuildPage(model, pages[pageIndex], pageIndex, pages.Count);
            var contentBytes = Ascii(content);
            objects.Add(Ascii($"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream"));
        }

        var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[1] = Ascii($"<< /Type /Pages /Kids [{kids}] /Count {pageObjectIds.Count} >>");

        using var output = new MemoryStream();
        Write(output, Ascii("%PDF-1.4\n"));
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Write(output, Ascii($"{index + 1} 0 obj\n"));
            Write(output, objects[index]);
            Write(output, Ascii("\nendobj\n"));
        }

        var xrefOffset = output.Position;
        Write(output, Ascii($"xref\n0 {objects.Count + 1}\n"));
        Write(output, Ascii("0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1))
        {
            Write(output, Ascii($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n"));
        }

        Write(output, Ascii($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
        return output.ToArray();
    }

    private static string BuildPage(OrderDetailsViewModel model, IReadOnlyList<OrderItem> items, int pageIndex, int pageCount)
    {
        var content = new StringBuilder();
        var order = model.Order;
        if (pageIndex == 0)
        {
            DrawText(content, "COZA STORE", 48, 748, 18, bold: true);
            DrawText(content, "INVOICE", 48, 710, 26, bold: true);
            DrawText(content, $"Order #{order.OrderId}", 420, 739, 11, bold: true);
            DrawText(content, order.CreatedAt.ToLocalTime().ToString("MMMM d, yyyy", CultureInfo.CurrentCulture), 420, 721);
            DrawText(content, $"Status: {order.Status}", 420, 703);

            DrawText(content, "BILL TO", 48, 660, 9, bold: true);
            DrawText(content, $"{order.FirstName} {order.LastName}", 48, 642, 10, bold: true);
            DrawText(content, order.Email, 48, 626);
            DrawText(content, order.Phone, 48, 610);

            DrawText(content, "SHIP TO", 330, 660, 9, bold: true);
            DrawText(content, order.Address, 330, 642);
            DrawText(content, $"{order.City}, {order.PostalCode}", 330, 626);
            DrawText(content, order.Country, 330, 610);
        }
        else
        {
            DrawText(content, "COZA STORE — INVOICE CONTINUED", 48, 748, 14, bold: true);
            DrawText(content, $"Order #{order.OrderId}", 48, 727, 10);
        }

        var headerY = pageIndex == 0 ? 570 : 690;
        DrawRule(content, 48, headerY + 12, 564);
        DrawText(content, "PRODUCT", 48, headerY, 9, bold: true);
        DrawRight(content, "UNIT PRICE", 390, headerY, 9, bold: true);
        DrawRight(content, "QTY", 460, headerY, 9, bold: true);
        DrawRight(content, "AMOUNT", 564, headerY, 9, bold: true);
        DrawRule(content, 48, headerY - 8, 564);

        var rowY = headerY - 28;
        foreach (var item in items)
        {
            var productName = item.Product?.Name ?? $"Product #{item.ProductId}";
            DrawText(content, Truncate(productName, 52), 48, rowY);
            DrawRight(content, RupeeFormatter.Format(item.Price), 390, rowY);
            DrawRight(content, item.Quantity.ToString(CultureInfo.CurrentCulture), 460, rowY);
            DrawRight(content, RupeeFormatter.Format(item.Price * item.Quantity), 564, rowY);
            DrawRule(content, 48, rowY - 8, 564, light: true);
            rowY -= 18;
        }

        if (pageIndex == pageCount - 1)
        {
            var totalY = Math.Max(rowY - 22, 70);
            DrawText(content, "TOTAL", 440, totalY, 11, bold: true);
            DrawRight(content, RupeeFormatter.Format(order.Total), 564, totalY, 12, bold: true);
        }

        DrawText(content, $"Page {pageIndex + 1} of {pageCount}", 500, 28, 8);
        return content.ToString();
    }

    private static void DrawText(StringBuilder content, string value, float x, float y, int size = 10, bool bold = false)
    {
        content.Append("BT /")
            .Append(bold ? "F2" : "F1")
            .Append(' ')
            .Append(size.ToString(CultureInfo.InvariantCulture))
            .Append(" Tf 1 0 0 1 ")
            .Append(x.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(y.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(" Tm (")
            .Append(EscapeText(value))
            .Append(") Tj ET\n");
    }

    private static void DrawRight(StringBuilder content, string value, float right, float y, int size = 10, bool bold = false)
    {
        var estimatedWidth = value.Length * size * .52f;
        DrawText(content, value, right - estimatedWidth, y, size, bold);
    }

    private static void DrawRule(StringBuilder content, float left, float y, float right, bool light = false)
    {
        var gray = light ? "0.9" : "0.72";
        content.Append("q ")
            .Append(gray)
            .Append(" G 0.6 w ")
            .Append(left.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(y.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(" m ")
            .Append(right.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(y.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(" l S Q\n");
    }

    private static string EscapeText(string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value.Replace('\r', ' ').Replace('\n', ' '));
        var escaped = new StringBuilder(bytes.Length);
        foreach (var valueByte in bytes)
        {
            if (valueByte is >= 32 and <= 126 && valueByte is not (byte)'(' and not (byte)')' and not (byte)'\\')
            {
                escaped.Append((char)valueByte);
            }
            else
            {
                escaped.Append('\\').Append(Convert.ToString(valueByte, 8).PadLeft(3, '0'));
            }
        }

        return escaped.ToString();
    }

    private static string Truncate(string value, int maximumLength)
    {
        value = value.Replace('\r', ' ').Replace('\n', ' ');
        return value.Length <= maximumLength ? value : value[..(maximumLength - 3)] + "...";
    }

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

    private static void Write(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
}
