using System.Text;

namespace ReadersRealm.Api.Import;

public static class CsvReader
{
    public static List<string[]> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Parse(reader.ReadToEnd());
    }

    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldWasQuoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    fieldWasQuoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    fieldWasQuoted = false;
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    fieldWasQuoted = false;
                    rows.Add([.. row]);
                    row.Clear();
                    break;
                case '\r':
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || fieldWasQuoted || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add([.. row]);
        }

        return rows;
    }
}
