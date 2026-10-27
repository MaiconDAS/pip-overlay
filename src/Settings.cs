using System.Text.Json;

namespace PipOverlay;

internal sealed record Settings
{
    public string Monitor { get; set; } = "";
    public bool AutomaticEnabled {get;set;} = true;
    public int StartShortcut {get;set;}
    public int StopShortcut {get;set;}
    public int X { get; set; } = 24;
    public int Y { get; set; } = 24;
    public int Width { get; set; } = 420;
    public int Height { get; set; } = 236;
    public int Opacity { get; set; } = 100;
    public int FramesPerSecond { get; set; } = 60;
    public string Quality {get;set;} = "maximum";
    public string GameExecutable {get;set;} = "";
    public int CropLeft { get; set; }
    public int CropTop { get; set; }
    public int CropRight { get; set; }
    public int CropBottom { get; set; }

    internal Settings Validated() => this with {
        X=Math.Clamp(X,0,32000), Y=Math.Clamp(Y,0,32000),FramesPerSecond=Quality=="economy"?15:Quality=="balanced"?30:60,
        Quality=Quality is "balanced" or "economy"?Quality:"maximum",
        Width=Math.Clamp(Width,120,7680),Height=Math.Clamp(Height,68,4320),Opacity=Math.Clamp(Opacity,10,100),
        CropLeft=Math.Clamp(CropLeft,0,16000),CropTop=Math.Clamp(CropTop,0,16000),
        CropRight=Math.Clamp(CropRight,0,16000),CropBottom=Math.Clamp(CropBottom,0,16000)
    };
    internal Rectangle Bounds(Rectangle screen)
    {
        var s=Validated();
        var w=Math.Min(s.Width,screen.Width); var h=Math.Min(s.Height,screen.Height);
        return new(screen.X+Math.Clamp(s.X,0,screen.Width-w),screen.Y+Math.Clamp(s.Y,0,screen.Height-h),w,h);
    }
    internal Rectangle Crop(Size source)
    {
        var s=Validated();
        int left=Math.Min(s.CropLeft,Math.Max(0,source.Width-1));
        int top=Math.Min(s.CropTop,Math.Max(0,source.Height-1));
        int right=Math.Clamp(source.Width-s.CropRight,left+1,source.Width);
        int bottom=Math.Clamp(source.Height-s.CropBottom,top+1,source.Height);
        return Rectangle.FromLTRB(left,top,right,bottom);
    }
    internal static Rectangle Fit(Size source,Rectangle area)
    {
        var factor=Math.Min((double)area.Width/source.Width,(double)area.Height/source.Height);
        var width=Math.Max(1,(int)Math.Round(source.Width*factor));
        var height=Math.Max(1,(int)Math.Round(source.Height*factor));
        return new(area.X+(area.Width-width)/2,area.Y+(area.Height-height)/2,width,height);
    }
    internal static Settings Load(string path)
    {
        try { return (JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new()).Validated(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    internal void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(Validated(),new JsonSerializerOptions { WriteIndented=true }));
        File.Move(path+".tmp",path,true);
    }
}
