namespace EasySave.ConsoleApp;

public class CommandLineParser
{
    public IEnumerable<int> Parse(string[] args)
    {
        if (args.Length == 0) yield break;
        var input = string.Join(";", args);
        foreach (var segment in input.Split(';'))
        {
            var trimmed = segment.Trim();
            if (trimmed.Contains('-'))
            {
                var parts = trimmed.Split('-');
                if (parts.Length == 2
                    && int.TryParse(parts[0].Trim(), out int from)
                    && int.TryParse(parts[1].Trim(), out int to))
                {
                    for (int i = from; i <= to; i++) yield return i;
                }
            }
            else if (int.TryParse(trimmed, out int index))
            {
                yield return index;
            }
        }
    }
}
