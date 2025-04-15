namespace ScheduledTaskInstaller;

public interface IScheduledTaskInstaller
{
    void Install(string name, string command, TimeSpan interval);
    void Uninstall(string name);
    bool IsInstalled(string name);
}
