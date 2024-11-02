using System.Diagnostics;
using System.Text;

namespace YourNamespace;

public class TranslateFileContext : ICommand { //TranslateFileContext C:\Translations\Will I End Up As a Hero or a Demon King\Episode 122.txt
    private static readonly HttpClient client = new();
    public string ShortHelp => "Attempts to translate a text file line by line using the previous lines as context, with character metadata";

    public async Task Execute(Program Main, string Params) {
        if (Params.Length < 1) {
            Console.WriteLine("This commands requires a file as the first parameter");
            return;
        }
        if (!File.Exists(Params)) {
            Console.WriteLine($"File {Params} does not exists");
            return;
        }
        string directory = Path.GetDirectoryName(Params);
        if (File.Exists($"{directory}/.KNOWLEDGE")) {
            Main.Knowledge = File.ReadAllText($"{directory}/.KNOWLEDGE");
        }
        if (File.Exists($"{directory}/.PROMPT")) {
            Main.PromptTemplate = File.ReadAllText($"{directory}/.PROMPT");
        }
        string[] lines = File.ReadAllLines(Params);
        for (int i = 0; i < lines.Length; i++) {
            lines[i] = ReplaceSpecialCharacters(lines[i]).Replace("\"", "\\\"").Replace("\\\\", "\\");
        }
        string[] OutputLines = new string[lines.Length];
        List<Task> Lines = new List<Task>();
        SemaphoreSlim Semaphore = new SemaphoreSlim(5);
        // for (int i = 0; i < 5; i++) {// For short debug, switch the lines
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].Trim().Length > 0) {
                Lines.Add(TranslateLine(Semaphore, Main, lines, OutputLines, i));
            } else {
                OutputLines[i] = lines[i];
            }
        }

        await Main.LoadingBarFor(Lines);

        File.WriteAllLines(AddTranslatedToFileName(Params), OutputLines);
    }

    public async Task TranslateLine(SemaphoreSlim Semaphore, Program Main, string[] Lines, string[] Output, int LineIndex) {
        await Semaphore.WaitAsync();
        try {
            string Json = CreateJson(Main, Lines, LineIndex, Main.Model);
            Json = Json.ReplaceLineEndings("\\n");
            var httpContent = new StringContent(Json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"http://{Main.Address}/v1/chat/completions/", httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var contentStartIndex = responseContent.IndexOf("content\": ", StringComparison.Ordinal) + 11;
            var extractedContent = responseContent[(contentStartIndex)..].Split("\"\n")[0];
            if (extractedContent.Contains("\\n")) {
                Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                Console.WriteLine($"Line {LineIndex} did not properly translate. Using original text: {Lines[LineIndex]}.");
                Console.WriteLine($"Failed text: {ReplaceSpecialCharacters(extractedContent)}.");
                Output[LineIndex] = Lines[LineIndex];
            } else {
                Output[LineIndex] = ReplaceSpecialCharacters(extractedContent);
            }
        } finally {
            Semaphore.Release();
        }
    }

    private static string ReplaceSpecialCharacters(string extractedContent) {
        return extractedContent
            .Replace("」", "\"").Replace("「", "\"")
            .Replace("』", "\"").Replace("『", "\"")
            .Replace("（", "(").Replace("）", ")")
            .Replace("【", "[").Replace("】", "]")
            .Replace("―", "-").Replace("ー", "-")
            .Replace("\r", "").Replace("\\\"", "\"") + "\n";
    }

    public string CreateJson(Program Main, string[] lines, int LineIndex, string Model) {
        string Text = Main.PromptTemplate;
        Text = Text.Replace("<<KNOWLEDGE>>", Main.Knowledge);
        if (Text.Contains("<<HISTORY>>")) {
            string History = "";
            int startIndex = Math.Max(0, LineIndex - Main.HistorySize);
            for (int i = startIndex; i < LineIndex; i++) {
                History += "\n" + lines[i];
            }

            Text = Text.Replace("<<HISTORY>>", History);
        }
        Text = Text.Replace("<<TEXT>>", lines[LineIndex]);
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