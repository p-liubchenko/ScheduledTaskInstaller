using System.Diagnostics;

namespace ScheduledTaskInstaller.TaskInstallers;
public class CronScheduledTaskInstaller : IScheduledTaskInstaller
{
    public void Install(string name, string command, TimeSpan interval)
    {
        int minutes = (int)Math.Max(interval.TotalMinutes, 1);
        string cronLine = $"*/{minutes} * * * * {command} # {name}";

        var crontab = Run("crontab", "-l", captureOutput: true) ?? string.Empty;
        if (crontab.Contains(name)) return;

        string newCrontab = crontab + Environment.NewLine + cronLine + Environment.NewLine;
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, newCrontab);
        Run("crontab", tmp);
        File.Delete(tmp);
    }

    public void Uninstall(string name)
    {
        var crontab = Run("crontab", "-l", captureOutput: true) ?? string.Empty;
        var newCrontab = string.Join("\n", crontab.Split('\n').Where(l => !l.Contains(name))) + "\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, newCrontab);
        Run("crontab", tmp);
        File.Delete(tmp);
    }

    public bool IsInstalled(string name)
    {
        var crontab = Run("crontab", "-l", captureOutput: true) ?? string.Empty;
        return crontab.Contains(name);
    }

    private string? Run(string file, string args, bool captureOutput = false)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        proc!.WaitForExit();
        return captureOutput ? proc.StandardOutput.ReadToEnd() : null;
    }
}
