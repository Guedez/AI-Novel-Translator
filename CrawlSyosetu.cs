using System.Text.RegularExpressions;

namespace YourNamespace;

public class CrawlSyosetu : ICommand {
    public string ShortHelp => "A crawler for https://ncode.syosetu.com/";

    public void GetHelp() {
        Console.WriteLine(ShortHelp);
        Console.WriteLine("The first parameter is the novel code, example: n4214hb");
        Console.WriteLine("The second parameter is the start chapter");
        Console.WriteLine("The second parameter is the end chapter");
        Console.WriteLine("The last parameter is the save directory");
        Console.WriteLine(@"Example: CrawlSyosetu n4214hb 142 142 C:\Translations\Will I End Up As a Hero or a Demon King\Raws");
    }

    public async Task Execute(Program Main, string Params) {
        string[] P = Params.Split(" ", 4);
        for (int i = 0; i < P.Length; i++) {
            Console.WriteLine(P[i]);
        }
        if (P.Length == 4 && int.TryParse(P[1], out int StartChapter) && int.TryParse(P[2], out int EndChapter) && EndChapter >= StartChapter) {
            string Novel = P[0];
            string SaveDirectory = P[3];

            if (!Directory.Exists(SaveDirectory)) {
                Directory.CreateDirectory(SaveDirectory);
            }
            if (Directory.Exists(SaveDirectory)) {
                Console.WriteLine($"Directory '{SaveDirectory}' is not valid");
            }

            SemaphoreSlim Semaphore = new SemaphoreSlim(1);
            List<Task> Lines = new List<Task>();
            for (int C = StartChapter; C < EndChapter + 1; C++) {
                Task downloadOne = DownloadOne(Semaphore, Novel, C, SaveDirectory);
                Lines.Add(downloadOne);
            }

            // string filePath = $"{SaveDirectory}\\{StartChapter} to {EndChapter}.txt";
            // Task concatenateTask = Task.WhenAll(Lines_S).ContinueWith(async completedTasks => {
            //     var results = await Task.WhenAll(Lines_S); // Waits for all Lines_S to complete and collects the results
            //     string NewLines = Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine;
            //     NewLines = NewLines + NewLines;
            //     await File.WriteAllTextAsync(filePath, string.Join(NewLines, results));
            // }).Unwrap(); // Unwrap to treat as a Task instead of Task<Task>
            // Lines.Add(concatenateTask);

            await Main.LoadingBarFor(Lines);
            // Console.WriteLine($"All chapters saved to {filePath}");
        } else {
            Console.WriteLine("Invalid parameters, see 'Help CrawlSyosetu'");
        }
    }

    public async Task DownloadOne(SemaphoreSlim Semaphore, string Novel, int Chapter, string SaveDirectory) {
        using (HttpClient client = new HttpClient()) {
            await Semaphore.WaitAsync();
            try {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");

                string pageContent = await client.GetStringAsync($"http://ncode.syosetu.com/{Novel}/{Chapter}/");

                string pattern = $"<h1 class=\"p-novel__title p-novel__title--rensai\">(.*?)</h1>";

                Match match = Regex.Match(pageContent, pattern, RegexOptions.Singleline);
                string Title = "";
                if (match.Success) {
                    Title = match.Groups[1].Value;
                }

                string start_string = "<div class=\"js-novel-text p-novel__text\">";
                int startIndex = pageContent.IndexOf(start_string) + start_string.Length + 1;
                pageContent = pageContent.Substring(startIndex);
                string end_string = "</div>";
                pageContent = pageContent.Substring(0, pageContent.IndexOf(end_string));

                pageContent = Title + "\n\n" + Regex.Replace(pageContent, @"<[^>]+>", "").Replace("\n\n", "\n").Trim();
                //await File.WriteAllTextAsync(filePath, pageContent);
                File.WriteAllText($"{SaveDirectory}\\{Chapter}.txt", pageContent);

                await Task.Delay(5000);
            } catch (Exception ex) {
                await Task.Delay(5000);
                Console.WriteLine($"Error downloading the page: {ex.Message}");
            } finally {
                Semaphore.Release();
            }
        }
    }
}