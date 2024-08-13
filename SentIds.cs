namespace LoukosterParser;

public static class SentIds
{
    private const string FilePath = "sent_ids.txt";
    public static HashSet<string> Get()
    {
        var ids = new HashSet<string>();
        if (File.Exists(FilePath))
        {
            var lines = File.ReadAllLines(FilePath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line)) ids.Add(line.Trim());
            }
        }
        return ids;
    }

    public static void Write(HashSet<string> ids)
    {
        using var writer = new StreamWriter(FilePath, append: false);
        foreach (var id in ids) writer.WriteLine(id);
    }
}