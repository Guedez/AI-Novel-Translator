// using System.Diagnostics;
// using System.Text;
// using System.Text.RegularExpressions;
//
// namespace YourNamespace;
//
// public class TranslateFullFile : ICommand { //TranslateFullFile C:\Translations\Will I End Up As a Hero or a Demon King\152.txt
//     private static readonly HttpClient client = new();
//     public string ShortHelp => "Attempts to translate a text batching multiple lines using the previous lines as context, with character metadata";
//     public static (int, int, string, string) SplitParams(string paramString)
//     {
//         // Regular expression to match two integers followed by two quoted strings (file paths)
//         string pattern = @"(\d+)\s+(\d+)\s+""([^""]+)""\s+""([^""]+)""";
//
//         Match match = Regex.Match(paramString, pattern);
//         if (match.Success)
//         {
//             // Extracting the integers and file paths
//             int int1 = int.Parse(match.Groups[1].Value);
//             int int2 = int.Parse(match.Groups[2].Value);
//             string filepath1 = match.Groups[3].Value;
//             string filepath2 = match.Groups[4].Value;
//
//             return (int1, int2, filepath1, filepath2);
//         }
//         else
//         {
//             Console.WriteLine($"Input string format is incorrect");
//             return (0, 0, null, null);
//         }
//     }
//     public async Task Execute(Program Main, string Params) {
//         var (From, To, Raws, File) = SplitParams(Params);
//         if (To < From) {
//             Console.WriteLine($"");
//             return;
//         }
//         if (P.Length == 4 && int.TryParse(P[1], out int StartChapter) && int.TryParse(P[2], out int EndChapter) && EndChapter >= StartChapter) {
//             string Novel = P[0];
//             string SaveDirectory = P[3];
//         if (!File.Exists(Params)) {
//             Console.WriteLine($"Invalid params, that {Params} exists");
//             return;
//         }
//         string Text = File.ReadAllText(Params);
//         string directory = Path.GetDirectoryName(Params);
//         if (File.Exists($"{directory}/.KNOWLEDGE")) {
//             List<(string, string)> KeyDictionary = new List<(string, string)>();
//             Main.Knowledge = File.ReadAllText($"{directory}/.KNOWLEDGE");
//             string[] _Terms = Main.Knowledge.Split("\n");
//             for (int i = 0; i < _Terms.Length; i++) {
//                 string[] KV = _Terms[i].Trim().Split(" ", 2);
//                 KeyDictionary.Add((KV[0], KV[1]));
//             }
//             (string, string)[] Terms = KeyDictionary.OrderByDescending(T => T.Item1.Length).ToArray();
//
//             foreach (var term in Terms) {
//                 Text = Regex.Replace(Text, Regex.Escape(term.Item1), term.Item2);
//             }
//         }
//
//         if (File.Exists($"{directory}/.PROMPT")) {
//             Main.PromptTemplate = File.ReadAllText($"{directory}/.PROMPT");
//         }
//
//         string Json = CreateJson(Main, Text, Main.Model);
//         Json = Json.ReplaceLineEndings("\\n");
//         var httpContent = new StringContent(Json, Encoding.UTF8, "application/json");
//         var response = await client.PostAsync($"http://{Main.Address}/v1/chat/completions/", httpContent);
//         var responseContent = await response.Content.ReadAsStringAsync();
//
//         var contentStartIndex = responseContent.IndexOf("content\": ", StringComparison.Ordinal) + 11;
//         var extractedContent = responseContent[(contentStartIndex)..].Split("\"\n")[0];
//         string Value = ReplaceSpecialCharacters(extractedContent).Replace("\\n", "\n");
//
//         await File.WriteAllTextAsync(AddTranslatedToFileName(Params), Value);
//     }
//
//     private static string ReplaceSpecialCharacters(string extractedContent) {
//         return extractedContent
//             .Replace("\r", "").Replace("\\n", "\n").Replace("\\\"", "\"") + "\n";
//     }
//
//     public string CreateJson(Program Main, string ToTranslate, string Model) {
//         string Text = Main.PromptTemplate;
//         Text = Text.Replace("<<TEXT>>", ToTranslate);
//         //Text = Text.Replace("\r", "");
//         //Text = Text.Replace("\"", "\\\"");
//         return "{\"messages\":[{\"role\":\"user\",\"content\":\"" + Text + "\"}],\"model\":\"" + Model + "\"}";
//     }
//
//     static string AddTranslatedToFileName(string filePath) {
//         // Get the directory, file name without extension, and extension
//         string directory = Path.GetDirectoryName(filePath);
//         string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
//         string extension = Path.GetExtension(filePath);
//
//         // Create the new filename
//         string newFileName = $"{fileNameWithoutExtension}-Translated{extension}";
//
//         // Combine with the original directory
//         return Path.Combine(directory, newFileName);
//     }
// }