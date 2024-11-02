namespace YourNamespace;

public class Help : ICommand {
    public string ShortHelp => "Shows this text";

    public async Task Execute(Program Main, string Params) {
        string[] Names = Main.Commands.Keys.OrderBy(T => T).ToArray();

        if (Params.Length > 0) {
            if (Main.Commands.TryGetValue(Params, out ICommand C)) {
                Console.WriteLine($"Displaying help for: {Params[0]}");
                C.GetHelp();
            } else {
                Console.WriteLine($"Command {Params[0]} not found");
            }
        } else {
            Console.WriteLine($"Listing Commands: ");
            foreach (string N in Names) {
                if (Main.Commands.TryGetValue(N, out ICommand C)) {
                    Console.WriteLine($"{N}: {C.ShortHelp}");
                }
            }
        }
    }
}