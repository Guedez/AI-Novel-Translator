namespace YourNamespace;

public interface ICommand {
    string ShortHelp { get; }
    Task Execute(Program Main, string Params);

    public virtual void GetHelp() {
        Console.WriteLine(ShortHelp);
    }
}