using System.Text;
using System.Text.RegularExpressions;

namespace YourNamespace;

public class BatchTerms : ICommand { //BatchTerms 265 303 "C:\Translations\Will I End Up As a Hero or a Demon King\Raws" "C:\Translations\Will I End Up As a Hero or a Demon King\Terminology.txt"
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
        Dictionary<string, HashSet<string>> translations = new Dictionary<string, HashSet<string>>();
        for (int Current = From; Current < To + 1; Current++) {
            if (!File.Exists($"{Raws}\\{Current}-Terms.txt")) {
                Console.WriteLine($"Extracting terms from {Raws}\\{Current}.txt");
                await Main.RunCommand(new[] { "FindTermsInFileBatchContext", $"20 {Raws}\\{Current}.txt" });
            } else {
                Console.WriteLine($"File {Raws}\\{Current}.txt is already extracted");
            }
            FindTermsInFileBatchContext.DeserializeTranslations(translations, File.ReadAllText($"{Raws}\\{Current}-Terms.txt"));
        }
        StringBuilder NewTerms = new StringBuilder();

        string directory = Path.GetDirectoryName(Raws);
        if (File.Exists($"{directory}/.KNOWLEDGE")) {
            HashSet<string> KeyDictionary = new HashSet<string>();
            Main.Knowledge = File.ReadAllText($"{directory}/.KNOWLEDGE");
            string[] _Terms = Main.Knowledge.Split("\n");
            for (int i = 0; i < _Terms.Length; i++) {
                string trim = _Terms[i].Trim();
                if (trim.Length == 0) continue;
                string[] KV = trim.Split(" ", 2);
                KeyDictionary.Add(KV[0]);
            }
            foreach (KeyValuePair<string, HashSet<string>> Proposed in translations) {
                if (!KeyDictionary.Contains(Proposed.Key)) {
                    NewTerms.AppendLine($"{Proposed.Key} | {string.Join(" | ", Proposed.Value)}");
                }
            }
        }

        File.WriteAllText(FileOut, NewTerms.ToString());

        Console.WriteLine($"Generated booklet at {FileOut}");
    }
}