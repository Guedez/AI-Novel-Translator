using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace YourNamespace;

public class TranslateFileBatchContext : ICommand { //TranslateFileBatchContext 20 C:\Translations\Will I End Up As a Hero or a Demon King\Raws\164.txt
    private static readonly HttpClient client = new();
    public string ShortHelp => "Attempts to translate a text batching multiple lines using the previous lines as context, with character metadata";

    public async Task Execute(Program Main, string Params) {
        string[] P = Params.Split(" ", 2);
        if (!int.TryParse(P[0], out int BatchSize) || !File.Exists(P[1])) {
            Console.WriteLine($"Invalid params, ensure to write a batch size and that {P[1]} exists");
            return;
        }
        string Text = File.ReadAllText(P[1]);
        string directory = Path.GetDirectoryName(P[1]);
        if (File.Exists($"{directory}/.KNOWLEDGE")) {
            List<(string, string)> KeyDictionary = new List<(string, string)>();
            Main.Knowledge = File.ReadAllText($"{directory}/.KNOWLEDGE");
            string[] _Terms = Main.Knowledge.Split("\n");
            for (int i = 0; i < _Terms.Length; i++) {
                string[] KV = _Terms[i].Trim().Split(" ", 2);
                KeyDictionary.Add((KV[0], KV[1]));
            }
            (string, string)[] Terms = KeyDictionary.OrderByDescending(T => T.Item1.Length).ToArray();

            foreach (var term in Terms) {
                Text = Regex.Replace(Text, Regex.Escape(term.Item1), term.Item2);
            }
        }
        if (File.Exists($"{directory}/.PROMPT")) {
            Main.PromptTemplate = File.ReadAllText($"{directory}/.PROMPT");
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

        File.WriteAllLines(AddTranslatedToFileName(P[1]), OutputLines);
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
            // if (extractedContent.Contains("\\n")) {
            //     Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
            //     Console.WriteLine($"Line {LineIndex} did not properly translate. Using original text: {Lines[LineIndex]}.");
            //     Console.WriteLine($"Failed text: {ReplaceSpecialCharacters(extractedContent)}.");
            //     Output[LineIndex] = Lines[LineIndex];
            // } else {
            Output[LineIndex] = ReplaceSpecialCharacters(extractedContent).Replace("\\n", "\n");
            // }
        } finally {
            Semaphore.Release();
        }
    }

    private static string ReplaceSpecialCharacters(string extractedContent) {
        return extractedContent
            // .Replace("」", "\"").Replace("「", "\"")
            // .Replace("』", "\"").Replace("『", "\"")
            // .Replace("（", "(").Replace("）", ")")
            // .Replace("【", "[").Replace("】", "]")
            // .Replace("―", "-").Replace("ー", "-")
            .Replace("\r", "").Replace("###", "").Replace("\\n", "\n").Replace("\\\"", "\"") + "\n";
    }

    public string CreateJson(Program Main, string Batch, string Model) {
        string Text = Main.PromptTemplate;
        // Text = Text.Replace("<<KNOWLEDGE>>", Main.Knowledge);
        // if (Text.Contains("<<HISTORY>>")) {
        //     string History = "";
        //     int startIndex = Math.Max(0, LineIndex - Main.HistorySize);
        //     for (int i = startIndex; i < LineIndex; i++) {
        //         History += lines[i];
        //     }
        //
        //     Text = Text.Replace("<<HISTORY>>", History);
        // }
        Text = Text.Replace("<<TEXT>>", Batch);
        //Text = Text.Replace("\r", "");
        //Text = Text.Replace("\"", "\\\"");
        return "{\"messages\":[{\"role\":\"user\",\"content\":\"" + Text + "\"}],\"model\":\"" + Model + "\"}";
    }

    static string AddTranslatedToFileName(string filePath) {
        // Get the directory, file name without extension, and extension
        string directory = Path.GetDirectoryName(filePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        // Create the new filename
        string newFileName = $"{fileNameWithoutExtension}-Translated{extension}";

        // Combine with the original directory
        return Path.Combine(directory, newFileName);
    }
}