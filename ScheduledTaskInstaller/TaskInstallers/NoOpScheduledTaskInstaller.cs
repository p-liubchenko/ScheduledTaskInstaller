namespace ScheduledTaskInstaller.TaskInstallers;
public class NoOpScheduledTaskInstaller : IScheduledTaskInstaller
{
    private readonly string _reason;

    public NoOpScheduledTaskInstaller(string reason)
    {
        _reason = reason;
    }

    public void Install(string name, string command, TimeSpan interval)
    {
        Console.WriteLine($"[NoOp] Install called for {name}, but platform is not supported.");
    }

    public void Uninstall(string name)
    {
        Console.WriteLine($"[NoOp] Uninstall called for {name}, but platform is not supported.");
    }

    public bool IsInstalled(string name)
    {
        Console.WriteLine($"[NoOp] IsInstalled called for {name}, returning false.");
        return false;
    }
}