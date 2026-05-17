namespace CryptoSoft;

public static class Program
{
    private const string MutexName = "Global\\CryptoSoft_SingleInstance";

    public static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: false, name: MutexName, out bool createdNew);

        if (!createdNew)
        {
            Console.WriteLine("CryptoSoft is already running. Please wait for the current encryption process to finish.");
            Environment.Exit(-1);
        }

        try
        {
            var fileManager = new FileManager(args[0], args[1]);
            int elapsedTime = fileManager.TransformFile();
            Environment.Exit(elapsedTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(-99);
        }
    }
}
