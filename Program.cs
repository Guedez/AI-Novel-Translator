// Program.cs

using System;
using System.Reflection;

#pragma warning disable CS8600

namespace YourNamespace {
    public class Program {
        public static Dictionary<string, T> GetImplementations<T>() where T : class {
            var implementations = new Dictionary<string, T>();
            var type = typeof(T);

            // Get all types that implement the interface T
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => type.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            // Instantiate each type and add to dictionary
            foreach (var implType in types) {
                if (Activator.CreateInstance(implType) is T instance) {
                    implementations.Add(implType.Name, instance);
                }
            }

            return implementations;
        }

        public class Close : ICommand {
            public string ShortHelp => "Closes the application";

            public async Task Execute(Program Main, string Params) {
                Environment.Exit(0);
            }
        }

        static void Main(string[] args) {
            new Program().DoMain(args);
        }

        public Dictionary<string, ICommand> Commands;
        public string Model = "rombos-llm-v2.6-qwen-14b";
        public string Address = "localhost:1234";
        public string PromptTemplate;
        public int HistorySize = 10;
        public string Knowledge = "";


        public void DoMain(string[] args) {
            Commands = GetImplementations<ICommand>();
            if (args.Length > 0) {
                RunCommand(args);
            } else {
                while (true) {
                    args = Console.ReadLine().Split(" ");
                    if (args.Length > 0) {
                        RunCommand(args);
                    }
                }
            }
        }

        public Task RunCommand(string[] args) {
            Console.WriteLine("");
            if (Commands.TryGetValue(args[0], out ICommand Command)) {
                string Params = string.Join(" ", args.Skip(1).ToArray());
                Console.WriteLine($"Executing [{args[0]}] with params [{string.Join(", ", Params)}]");
                return Command.Execute(this, Params);
            } else {
                Console.WriteLine("Error, command not found: " + args[0]);
                return null;
            }
        }

        public async Task LoadingBarFor(List<Task> tasks) {
            int totalTasks = tasks.Count;
            int completedTasks = 0;

            // Continuously update progress until all tasks are completed
            while (completedTasks < totalTasks) {
                completedTasks = tasks.Count(t => t.IsCompleted);
                DisplayProgressBar(completedTasks, totalTasks);
                await Task.Delay(100); // Update every 100ms
            }

            // Ensure final display shows 100%
            DisplayProgressBar(totalTasks, totalTasks);
            Console.WriteLine("\nAll tasks completed.");
        }

        private void DisplayProgressBar(int completed, int total) {
            int width = 50; // Width of the progress bar
            int progress = (int)((double)completed / total * width);

            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('#', progress));
            Console.Write(new string('-', width - progress));
            Console.Write($"] {completed}/{total} Tasks Completed");
        }
    }
}