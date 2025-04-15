using System.Diagnostics;

namespace ScheduledTaskInstaller.TaskInstallers;
public class WindowsScheduledTaskInstaller : IScheduledTaskInstaller
{
    public void Install(string name, string command, TimeSpan interval)
    {
        int minutes = (int)Math.Max(interval.TotalMinutes, 1);
        string args = $"/Create /SC MINUTE /MO {minutes} /TN \"{name}\" /TR \"{command}\" /F";
        Run("schtasks", args);
    }

    public void Uninstall(string name)
    {
        Run("schtasks", $"/Delete /TN \"{name}\" /F");
    }

    public bool IsInstalled(string name)
    {
        var result = Run("schtasks", $"/Query /TN \"{name}\"", captureOutput: true);
        return result?.Contains(name) == true;
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
