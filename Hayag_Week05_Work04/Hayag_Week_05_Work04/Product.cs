using System.Globalization;

namespace ProductFormApp;

public record Product(
    string Barcode,
    string Name,
    string Description,
    string Category,
    string Origin,
    string Status,
    decimal Cost,
    decimal Price,
    decimal Markup);

/// <summary>Reads and writes products.csv (the same file the Product Form saves to).</summary>
public static class ProductRepository
{
    public static readonly string CsvPath =
        Path.Combine(AppContext.BaseDirectory, "products.csv");

    public static List<Product> Load()
    {
        var list = new List<Product>();
        if (!File.Exists(CsvPath)) return list;

        foreach (var line in File.ReadLines(CsvPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = ParseLine(line);
            if (f.Count < 9) continue;

            if (!TryDec(f[6], out var cost) || !TryDec(f[7], out var price) || !TryDec(f[8], out var markup))
                continue;

            list.Add(new Product(f[0], f[1], f[2], f[3], f[4], f[5], cost, price, markup));
        }
        return list;
    }

    public static void SaveAll(IEnumerable<Product> products)
    {
        using var w = new StreamWriter(CsvPath, append: false);
        w.WriteLine("Barcode,Name,Description,Category,Origin,Status,Cost,Price,Markup");
        foreach (var p in products)
        {
            w.WriteLine(string.Join(",",
                Quote(p.Barcode), Quote(p.Name), Quote(p.Description), Quote(p.Category),
                p.Origin, p.Status,
                p.Cost.ToString("0.00", CultureInfo.InvariantCulture),
                p.Price.ToString("0.00", CultureInfo.InvariantCulture),
                p.Markup.ToString("0.00", CultureInfo.InvariantCulture)));
        }
    }

    private static string Quote(string s) =>
        "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";

    private static bool TryDec(string s, out decimal v) =>
        decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v);

    // Handles quoted fields that contain commas or doubled quotes
    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }
}
