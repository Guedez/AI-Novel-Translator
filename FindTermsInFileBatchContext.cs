using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace YourNamespace;

public class FindTermsInFileBatchContext : ICommand { //FindTermsInFileBatchContext 20 C:\Translations\Will I End Up As a Hero or a Demon King\Raws\164.txt
    private static readonly HttpClient client = new();
    public string ShortHelp => "Find specific names and terms in the selected text";

    public async Task Execute(Program Main, string Params) {
        string[] P = Params.Split(" ", 2);
        if (!int.TryParse(P[0], out int BatchSize) || !File.Exists(P[1])) {
            Console.WriteLine($"Invalid params, ensure to write a batch size and that {P[1]} exists");
            return;
        }
        string Text = File.ReadAllText(P[1]);
        string directory = Path.GetDirectoryName(P[1]);

        if (File.Exists($"{directory}/TERMS.PROMPT")) {
            Main.PromptTemplate = File.ReadAllText($"{directory}/TERMS.PROMPT");
        }
        string[] lines = Text.Split("\n");
        List<string> Paragraphs = new List<string>();
        StringBuilder ParagraphMaker = new StringBuilder();
        string ParagraphDeliminator = "　";

        for (int i = 0; i < lines.Length; i++) {
            if (!lines[i].StartsWith(ParagraphDeliminator)) {
                if (ParagraphMaker.Length > 0) {
                    Paragraphs.Add(ParagraphMaker.ToString());
                    ParagraphMaker.Clear();
                }
            }
            if (ParagraphMaker.Length > 0) ParagraphMaker.Append("\n");
            ParagraphMaker.Append(lines[i]);
        }
        Paragraphs.Add(ParagraphMaker.ToString());

        List<string> WorkBatches = new List<string>();

        ParagraphMaker.Clear();
        for (int i = 0; i < Paragraphs.Count; i++) {
            if (ParagraphMaker.ToString().Count(T => T == '\n') > BatchSize) {
                if (ParagraphMaker.Length > 0) {
                    WorkBatches.Add(ParagraphMaker.ToString());
                    ParagraphMaker.Clear();
                }
            }
            if (ParagraphMaker.Length > 0) ParagraphMaker.Append("\n");
            ParagraphMaker.Append(Paragraphs[i]);
        }
        WorkBatches.Add(ParagraphMaker.ToString());

        // for (int i = 0; i < lines.Length; i++) {
        //     lines[i] = ReplaceSpecialCharacters(lines[i]).Replace("\"", "\\\"").Replace("\\\\", "\\");
        // }
        string[] OutputLines = new string[WorkBatches.Count];
        List<Task> Lines = new List<Task>();
        SemaphoreSlim Semaphore = new SemaphoreSlim(2);
        // for (int i = 0; i < 5; i++) {// For short debug, switch the lines
        for (int i = 0; i < WorkBatches.Count; i++) {
            Lines.Add(TranslateLine(Semaphore, Main, WorkBatches, OutputLines, i));
        }

        await Main.LoadingBarFor(Lines);
        Dictionary<string, HashSet<string>> Terms = new Dictionary<string, HashSet<string>>();
        foreach (string line in OutputLines) {
            ParseTranslations(Terms, line);
        }
        string T = SerializeTranslations(Terms);
        File.WriteAllText(AddTermsToFileName(P[1]), T);
    }

    public static string SerializeTranslations(Dictionary<string, HashSet<string>> translations) {
        var sb = new StringBuilder();

        foreach (var entry in translations) {
            foreach (var translation in entry.Value) {
                sb.AppendLine($"{entry.Key} | {translation}");
            }
        }

        return sb.ToString().TrimEnd(); // Remove any trailing newline
    }

    public static void DeserializeTranslations(Dictionary<string, HashSet<string>> translations, string serialized) {
        var lines = serialized.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines) {
            var parts = line.Split('|');
            if (parts.Length != 2) continue;

            string japanese = parts[0].Trim();
            string translation = parts[1].Trim();

            if (!translations.ContainsKey(japanese)) {
                translations[japanese] = new HashSet<string>();
            }
            translations[japanese].Add(translation);
        }
    }

    private void ParseTranslations(Dictionary<string, HashSet<string>> Terms, string input) {
        // Split input by lines
        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines) {
            // Split each line by '|'
            var parts = line.Split('|');
            if (parts.Length != 2) continue; // Ignore malformed lines

            string japanese = parts[0].Trim();
            string translation = parts[1].Trim();

            // Add to dictionary
            if (!Terms.ContainsKey(japanese)) {
                Terms[japanese] = new HashSet<string>();
            }
            Terms[japanese].Add(translation);
        }
    }

    public async Task TranslateLine(SemaphoreSlim Semaphore, Program Main, List<string> WorkBatches, string[] Output, int LineIndex) {
        await Semaphore.WaitAsync();
        try {
            string Json = CreateJson(Main, WorkBatches[LineIndex], Main.Model);
            Json = Json.ReplaceLineEndings("\\n");
            var httpContent = new StringContent(Json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"http://{Main.Address}/v1/chat/completions/", httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var contentStartIndex = responseContent.IndexOf("content\": ", StringComparison.Ordinal) + 11;
            var extractedContent = responseContent[(contentStartIndex)..].Split("\"\n")[0];
            Output[LineIndex] = ReplaceSpecialCharacters(extractedContent).Replace("\\n", "\n");
            // }
        } finally {
            Semaphore.Release();
        }
    }

    private static string ReplaceSpecialCharacters(string extractedContent) {
        return extractedContent
            .Replace("\r", "").Replace("###", "").Replace("\\n", "\n").Replace("\\\"", "\"") + "\n";
    }

    public string CreateJson(Program Main, string Batch, string Model) {
        string Text = Main.PromptTemplate;
        Text = Text.Replace("<<TEXT>>", Batch);
        return "{\"messages\":[{\"role\":\"user\",\"content\":\"" + Text + "\"}],\"model\":\"" + Model + "\"}";
    }

    static string AddTermsToFileName(string filePath) {
        // Get the directory, file name without extension, and extension
        string directory = Path.GetDirectoryName(filePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        // Create the new filename
        string newFileName = $"{fileNameWithoutExtension}-Terms{extension}";

        // Combine with the original directory
        return Path.Combine(directory, newFileName);
    }
}