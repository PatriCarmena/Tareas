using System.Text;

namespace OrderDesk.Web.Services;

public static class CsvExport
{
    public static byte[] Build(IEnumerable<IEnumerable<string?>> rows)
    {
        var text = new StringBuilder();
        foreach (var row in rows)
            text.AppendLine(string.Join(';', row.Select(Escape)));
        // Excel reconoce UTF-8 con BOM y separador punto y coma en configuración es-AR.
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static string Escape(string? value)
    {
        var safe = value ?? string.Empty;
        // Evita que un nombre o descripción de usuario se interprete como fórmula al abrir el CSV.
        if (safe.Length > 0 && ("=+-@".Contains(safe.TrimStart().FirstOrDefault()) || safe[0] is '\t' or '\r' or '\n'))
            safe = "'" + safe;
        return '"' + safe.Replace("\"", "\"\"") + '"';
    }
}
