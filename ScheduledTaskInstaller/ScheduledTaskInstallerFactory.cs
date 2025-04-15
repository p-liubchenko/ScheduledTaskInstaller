using ScheduledTaskInstaller.TaskInstallers;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScheduledTaskInstaller;
public static class ScheduledTaskInstallerFactory
{
    public static IScheduledTaskInstaller Create()
    {
        // Windows platform uses schtasks
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsScheduledTaskInstaller();

        // Linux platform prefers crontab, fallback to systemd or error
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string osInfo = File.Exists("/etc/os-release") ? File.ReadAllText("/etc/os-release") : string.Empty;
            string uname = Run("uname", "-a") ?? string.Empty;

            if (uname.Contains("Synology", StringComparison.OrdinalIgnoreCase))
                return new CronScheduledTaskInstaller(); // Synology uses cron

            if (osInfo.Contains("Alpine", StringComparison.OrdinalIgnoreCase))
                return new CronScheduledTaskInstaller(); // Alpine needs cronie typically

            if (osInfo.Contains("OpenWrt", StringComparison.OrdinalIgnoreCase))
                return new CronScheduledTaskInstaller(); // BusyBox cron
            
            if (osInfo.Contains("Raspbian", StringComparison.OrdinalIgnoreCase) || uname.Contains("raspberrypi", StringComparison.OrdinalIgnoreCase))
                return new CronScheduledTaskInstaller();
            
            if (IsRunningInWSL())
            {
                if (IsCommandAvailable("crontab"))
                    return new CronScheduledTaskInstaller();
                return new NoOpScheduledTaskInstaller("WSL without cron");
            }
            if (IsRunningInDocker())
            {
                if (IsCommandAvailable("crontab"))
                    return new CronScheduledTaskInstaller();
                return new NoOpScheduledTaskInstaller("Docker without cron");
            }
            if (uname.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return new NoOpScheduledTaskInstaller();

            if (IsCommandAvailable("crontab"))
                return new CronScheduledTaskInstaller();

            if (IsCommandAvailable("systemctl") && IsCommandAvailable("systemd-run"))
                return new SystemdScheduledTaskInstaller();

            return new NoOpScheduledTaskInstaller("Distributive does not support cron or systemd");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            if (IsCommandAvailable("crontab"))
                return new CronScheduledTaskInstaller();

            throw new PlatformNotSupportedException(".NET is not officially supported on FreeBSD. Consider using a supported platform or a community build.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new LaunchdScheduledTaskInstaller();

        if (IsTizenOS())
            return new NoOpScheduledTaskInstaller("Tizen supports background services via AppControl in managed apps. No general-purpose scheduler available.");

        if (IsIOS())
            return new NoOpScheduledTaskInstaller("iOS does not allow persistent scheduled background processes. Consider using system APIs like BGTaskScheduler in native code.");

        return new NoOpScheduledTaskInstaller("OS not supported");
    }

    private static string? Run(string file, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc!.WaitForExit();
            return proc.StandardOutput.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }
    private static bool IsTizenOS()
    {
        return File.Exists("/etc/tizen-release") || Directory.Exists("/opt/share/tizen-sdk");
    }

    private static bool IsIOS()
    {
        return RuntimeInformation.OSDescription.Contains("Darwin") && !Directory.Exists("/Applications");
    }
    private static bool IsCommandAvailable(string name)
    {
        try
        {
            var psi = new ProcessStartInfo("which", name)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc!.WaitForExit();

            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    private static bool IsRunningInWSL()
    {
        string? content = File.Exists("/proc/version") ? File.ReadAllText("/proc/version") : null;
        return content != null && content.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRunningInDocker()
    {
        return File.Exists("/.dockerenv") || Directory.Exists("/var/run/secrets/kubernetes.io");
    }
}

