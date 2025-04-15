using System.Diagnostics;

namespace ScheduledTaskInstaller.TaskInstallers;
public class LaunchdScheduledTaskInstaller : IScheduledTaskInstaller
{
    private const string LaunchAgentsPath = "~/Library/LaunchAgents";

    public void Install(string name, string command, TimeSpan interval)
    {
        string label = $"com.custom.{name}";
        string plistPath = Path.Combine(Environment.ExpandEnvironmentVariables(LaunchAgentsPath), $"{label}.plist");

        int minutes = (int)Math.Max(interval.TotalMinutes, 1);

        string plistContent = $"""
<?xml version=\"1.0\" encoding=\"UTF-8\"?>
<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">
<plist version=\"1.0\">
<dict>
    <key>Label</key>
    <string>{label}</string>
    <key>ProgramArguments</key>
    <array>
        <string>/bin/bash</string>
        <string>-c</string>
        <string>{command}</string>
    </array>
    <key>StartInterval</key>
    <integer>{minutes * 60}</integer>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
""";

        Directory.CreateDirectory(Path.GetDirectoryName(plistPath)!);
        File.WriteAllText(plistPath, plistContent);
        Run("launchctl", $"load {plistPath}");
    }

    public void Uninstall(string name)
    {
        string label = $"com.custom.{name}";
        string plistPath = Path.Combine(Environment.ExpandEnvironmentVariables(LaunchAgentsPath), $"{label}.plist");
        Run("launchctl", $"unload {plistPath}");
        if (File.Exists(plistPath)) File.Delete(plistPath);
    }

    public bool IsInstalled(string name)
    {
        string label = $"com.custom.{name}";
        var result = Run("launchctl", "list", captureOutput: true);
        return result?.Contains(label) == true;
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
