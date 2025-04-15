using System.Diagnostics;

namespace ScheduledTaskInstaller.TaskInstallers;
public class SystemdScheduledTaskInstaller : IScheduledTaskInstaller
{
    private const string SystemdPath = "~/.config/systemd/user"; // User-level systemd services

    public void Install(string name, string command, TimeSpan interval)
    {
        // Convert ~ to absolute path
        var fullPath = Environment.ExpandEnvironmentVariables(SystemdPath);
        Directory.CreateDirectory(fullPath);

        string serviceName = $"{name}.service";
        string timerName = $"{name}.timer";
        string servicePath = Path.Combine(fullPath, serviceName);
        string timerPath = Path.Combine(fullPath, timerName);

        string serviceContent = $"""
[Unit]
Description=Run {name} command

[Service]
Type=oneshot
ExecStart={command}
""";

        int minutes = (int)Math.Max(interval.TotalMinutes, 1);
        string timerContent = $"""
[Unit]
Description=Timer for {name}

[Timer]
OnBootSec=1min
OnUnitActiveSec={minutes}min
Unit={serviceName}

[Install]
WantedBy=timers.target
""";

        File.WriteAllText(servicePath, serviceContent);
        File.WriteAllText(timerPath, timerContent);

        // Reload and start the timer
        Run("systemctl", $"--user daemon-reload");
        Run("systemctl", $"--user enable --now {timerName}");
    }

    public void Uninstall(string name)
    {
        string serviceName = $"{name}.service";
        string timerName = $"{name}.timer";
        var fullPath = Environment.ExpandEnvironmentVariables(SystemdPath);

        Run("systemctl", $"--user disable --now {timerName}");
        Run("systemctl", $"--user disable {serviceName}");

        File.Delete(Path.Combine(fullPath, serviceName));
        File.Delete(Path.Combine(fullPath, timerName));

        Run("systemctl", "--user daemon-reload");
    }

    public bool IsInstalled(string name)
    {
        string timerName = $"{name}.timer";
        var result = Run("systemctl", $"--user list-timers", captureOutput: true);
        return result?.Contains(timerName) == true;
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