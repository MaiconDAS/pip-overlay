using System.Diagnostics;
using System.Text.Json;

namespace PipOverlay;

internal static class Diagnostics
{
    internal static void Inspect(string path)
    {
        var foreground=Native.GetForegroundWindow();var list=new List<object>();int order=0;
        Native.EnumWindows((hwnd,_)=> {
            int index=order++;
            try {
                using var p=Process.GetProcessById((int)Native.ProcessId(hwnd));
                if (p.ProcessName is not ("PipOverlay" or "DigimonMasters" or "msedge" or "chrome")) return true;
                if (!Native.IsWindowVisible(hwnd) && p.ProcessName!="PipOverlay") return true;
                Native.GetWindowRect(hwnd,out var rect);
                list.Add(new { order=index,hwnd=hwnd.ToInt64(),process=p.ProcessName,foreground=foreground==hwnd,
                    visible=Native.IsWindowVisible(hwnd),minimized=Native.IsIconic(hwnd),
                    topmost=Native.IsTopmost(hwnd),exStyle=Native.GetWindowLongPtr(hwnd,-20).ToInt64().ToString("X"),
                    style=Native.GetWindowLongPtr(hwnd,-16).ToInt64().ToString("X"),
                    owner=Native.GetWindow(hwnd,4).ToInt64(),bounds=new {rect.Left,rect.Top,rect.Right,rect.Bottom} });
            } catch(ArgumentException e) { list.Add(new {hwnd=hwnd.ToInt64(),pid=Native.ProcessId(hwnd),error=e.Message}); }
            return true;
        },0);
        File.WriteAllText(path,JsonSerializer.Serialize(list,new JsonSerializerOptions {WriteIndented=true}));
    }
}
