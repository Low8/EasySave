namespace EasySave.ConsoleApp;

public class CommandLineParser
{
    public IEnumerable<int> Parse(string[] args)
    {
        var input = string.Join("", args);
        var result = new List<int>();

        if (input.Contains("-"))
        {
            var p = input.Split('-');
            for (int i = int.Parse(p[0]) - 1; i <= int.Parse(p[1]) - 1; i++)
                result.Add(i);
        }
        else if (input.Contains(";"))
        {
            foreach (var x in input.Split(';'))
                result.Add(int.Parse(x) - 1);
        }
        else
        {
            result.Add(int.Parse(input) - 1);
        }

        return result;
    }
}