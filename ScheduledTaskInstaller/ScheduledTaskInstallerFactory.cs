using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskInstaller;
public static class ScheduledTaskInstallerFactory
{
    public static IScheduledTaskInstaller Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsScheduledTaskInstaller();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new CronScheduledTaskInstaller(); // or Systemd
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new LaunchdScheduledTaskInstaller(); // Optional

        throw new PlatformNotSupportedException();
    }
}
