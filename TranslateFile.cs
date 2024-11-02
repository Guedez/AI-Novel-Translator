// using System.Text;
//
// namespace YourNamespace;
//
// public class TranslateFile : ICommand {
//     private static readonly HttpClient client = new HttpClient();
//     public string ShortHelp => "Attempts to translate a text file line by line";
//
//     public async void Execute(Program Main, string Params) {
//         if (Params.Length < 1) {
//             Console.WriteLine("This commands requires a file as the first parameter");
//             return;
//         }
//         if (!File.Exists(Params)) {
//             Console.WriteLine($"File {Params} does not exists");
//             return;
//         }
//         string[] lines = File.ReadAllLines(Params);
//         string[] OutputLines = new string[lines.Length];
//         List<Task> Lines = new List<Task>();
//         SemaphoreSlim Semaphore = new SemaphoreSlim(5);
//         for (int i = 0; i < lines.Length; i++) {
//             Lines.Add(TranslateLine(Semaphore, Main, lines[i], OutputLines, i));
//         }
//
//         await Main.LoadingBarFor(Lines);
//
//         File.WriteAllLines(AddTranslatedToFileName(Params), OutputLines);
//     }
//
//     public async Task TranslateLine(SemaphoreSlim Semaphore, Program Main, string Line, string[] Output, int LineIndex) {
//         await Semaphore.WaitAsync();
//         try
//         {
//             var httpContent = new StringContent(CreateJson(Line, Main.Model), Encoding.UTF8, "application/json");
//             var response = await client.PostAsync($"http://{Main.Address}/v1/chat/completions/", httpContent);
//             var responseContent = await response.Content.ReadAsStringAsync();
//         
//             var contentStartIndex = responseContent.IndexOf("content\": ", StringComparison.Ordinal) + 11;
//             var extractedContent = responseContent[(contentStartIndex)..].Split("\"\n")[0];
//
//             Output[LineIndex] = extractedContent
//                 .Replace("\\n", Environment.NewLine) // Replace literal \n with actual new line
//                 .Replace("\\\"", "\"") + Environment.NewLine;
//         }
//         finally
//         {
//             Semaphore.Release();
//         }
//     }
//
//     public string CreateJson(string CurrentText, string Model) {
//         //厳密には俺だけが泊まるこの宿『ハンファレスト』は4階建ての建物だ。
//         //vntl-llama3-8b-202409
//         return "{\"messages\":[{\"role\":\"user\",\"content\":\"" + CurrentText + "\"}],\"model\":\"" + Model + "\"}";
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