using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordOAuthWpf;

public sealed record TweakDefinition(string Id, string Name, string Command, bool RequiresAdmin = true);

public sealed class WindowsTweaksService
{
    public IReadOnlyList<TweakDefinition> AvailableTweaks { get; } =
    [
        new("visual", "Disable Animations & Visual Effects", "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f && reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 100 /f", false),
        new("speedmenu", "Speed Up Menus and Windows", "reg add \"HKCU\\Control Panel\\Desktop\" /v ForegroundFlashCount /t REG_DWORD /d 1 /f"),
        new("tips", "Disable Windows Tips & Suggestions", "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f", false),
        new("transparency", "Disable Transparency & Shadows", "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f && reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v ListviewShadow /t REG_DWORD /d 0 /f", false),
        new("cortana", "Disable Cortana", "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 0 /f"),
        new("onedrive", "Disable OneDrive", "reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f && taskkill /f /im OneDrive.exe"),
        new("indexing", "Disable Windows Search Indexing", "sc stop WSearch && sc config WSearch start= disabled"),
        new("sysmain", "Disable Superfetch / SysMain", "sc stop SysMain && sc config SysMain start= disabled"),
        new("power", "Enable Ultimate Performance Power Plan", "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 && powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61"),
        new("network", "Network Tweaks (TCP Optimizations)", "netsh interface tcp set global autotuninglevel=normal && netsh interface tcp set global chimney=enabled && netsh interface tcp set global rss=enabled"),
        new("lock", "Disable Lock Screen", "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization\" /v NoLockScreen /t REG_DWORD /d 1 /f"),
        new("explorer", "Speed Up Explorer & Taskbar", "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f && reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v AnimateMinMax /t REG_DWORD /d 0 /f", false),
        new("ssd", "Apply SSD Optimizations", "powercfg -h off && fsutil behavior set DisableDeleteNotify 0"),
        new("clean", "Clean Temp Files", "del /q/f/s %TEMP%\\* && del /q/f/s C:\\Windows\\Temp\\*"),
        new("xbox", "Disable Xbox Services", "sc stop XboxGipSvc && sc stop XboxNetApiSvc && sc config XboxGipSvc start= disabled && sc config XboxNetApiSvc start= disabled"),
        new("feedback", "Disable Windows Feedback", "reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f && reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v PeriodInNanoSeconds /t REG_DWORD /d 0 /f", false),
        new("hibernation", "Disable Hibernation", "powercfg -h off")
    ];

    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async Task<int> ApplyTweaksAsync(IEnumerable<TweakDefinition> tweaks, Action<string> log, CancellationToken ct)
    {
        var selected = tweaks.ToList();
        if (selected.Count == 0)
        {
            return 0;
        }

        var applied = 0;

        foreach (var tweak in selected)
        {
            ct.ThrowIfCancellationRequested();
            log($"Applying: {tweak.Name}");

            var exitCode = await ExecuteCommandAsync(tweak.Command, ct);
            if (exitCode == 0)
            {
                applied++;
                log($"✔ {tweak.Name}");
            }
            else
            {
                log($"✖ {tweak.Name} (exit {exitCode})");
            }
        }

        return applied;
    }

    private static async Task<int> ExecuteCommandAsync(string command, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}
