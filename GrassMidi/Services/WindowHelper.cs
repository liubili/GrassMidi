using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace GrassMidi.Services;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Gets the OBS-compatible window string for the current foreground window.
    /// Format: Title:Class:Executable.exe
    /// </summary>
    public static string? GetForegroundWindowInfo()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            // 1. Get Title
            const int nChars = 256;
            var titleBuilder = new StringBuilder(nChars);
            if (GetWindowText(hWnd, titleBuilder, nChars) == 0) return null; // Or empty? OBS might need non-empty
            string title = titleBuilder.ToString();

            // 2. Get Class
            var classBuilder = new StringBuilder(nChars);
            GetClassName(hWnd, classBuilder, nChars);
            string className = classBuilder.ToString();

            // 3. Get Executable Name
            GetWindowThreadProcessId(hWnd, out uint pid);
            string exeName = "";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                exeName = proc.MainModule?.ModuleName ?? proc.ProcessName + ".exe";
            }
            catch
            {
                // Fallback or permission issues
                return null;
            }

            // Replace colons in parts to avoid parsing issues if any? 
            // OBS usually expects "Title:Class:Executable"
            // If title contains colon, does it break? 
            // Based on obs-studio source (window-helpers-win.cpp), it finds the window by looking for exact matches first.
            // But the setting format string is specific. Windows titles can change.
            // Let's stick to the standard format.
            
            return $"{title}:{className}:{exeName}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting window info: {ex.Message}");
            return null;
        }
    }
}
