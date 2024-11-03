using System.Text;
using System.Text.RegularExpressions;

namespace YourNamespace;

public class BatchBooklet : ICommand { //BatchBooklet 164 212 "C:\Translations\Will I End Up As a Hero or a Demon King\Raws" "C:\Translations\Will I End Up As a Hero or a Demon King\Volume 6.txt"
    public string ShortHelp => "Runs multiple commands to generate a booklet after translating multiple files";

    public static (int, int, string, string) SplitParams(string paramString) {
        // Regular expression to match two integers followed by two quoted strings (file paths)
        string pattern = @"(\d+)\s+(\d+)\s+""([^""]+)""\s+""([^""]+)""";

        Match match = Regex.Match(paramString, pattern);
        if (match.Success) {
            // Extracting the integers and file paths
            int int1 = int.Parse(match.Groups[1].Value);
            int int2 = int.Parse(match.Groups[2].Value);
            string filepath1 = match.Groups[3].Value;
            string filepath2 = match.Groups[4].Value;

            return (int1, int2, filepath1, filepath2);
        } else {
            Console.WriteLine($"Input string format is incorrect");
            return (0, 0, null, null);
        }
    }

    public async Task Execute(Program Main, string Params) {
        var (From, To, Raws, FileOut) = SplitParams(Params);
        if (To < From) {
            Console.WriteLine($"Invalid chapters");
            return;
        }
        if (!Directory.Exists(Raws)) {
            Console.WriteLine($"Raws directory is invalid");
            return;
        }
        string OutDir = Path.GetDirectoryName(FileOut);
        if (!Directory.Exists(OutDir)) {
            Directory.CreateDirectory(OutDir);
        }
        if (!Directory.Exists(OutDir)) {
            Console.WriteLine($"Attempting to save a file to an invalid directory");
            return;
        }
        File.Create(FileOut).Close();
        if (!File.Exists(FileOut)) {
            Console.WriteLine($"Attempting to save an invalid file");
            return;
        }
        StringBuilder Booklet = new StringBuilder();
        for (int Current = From; Current < To + 1; Current++) {
            if (!File.Exists($"{Raws}\\{Current}-Translated.txt")) {
                Console.WriteLine($"Translating file {Raws}\\{Current}.txt");
                await Main.RunCommand(new[] { "TranslateFileBatchContext", $"5 {Raws}\\{Current}.txt" });
            } else {
                Console.WriteLine($"File {Raws}\\{Current}.txt is already translated");
            }
            if (Booklet.Length > 0) {
                Booklet.Append("\n\n\n\n###");
            } else {
                Booklet.Append("###");
            }
            Booklet.Append(File.ReadAllText($"{Raws}\\{Current}-Translated.txt"));
        }

        File.WriteAllText(FileOut, Booklet.ToString());

        Console.WriteLine($"Generated booklet at {FileOut}");
    }
}