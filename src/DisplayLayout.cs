namespace PipOverlay;

internal sealed record DisplayLayout(int ScreenWidth,int ScreenHeight,int X,int Y,int Width,int Height)
{
    internal static DisplayLayout Capture(Settings s,Size screen)=>new(screen.Width,screen.Height,s.X,s.Y,s.Width,s.Height);
    internal Rectangle Fit(Size screen){
        if(screen.Width<=0||screen.Height<=0)throw new ArgumentOutOfRangeException(nameof(screen));
        double scale=screen.Width/(double)Math.Max(1,ScreenWidth);
        int w=(int)Math.Clamp(Width*scale,Math.Min(120,screen.Width),screen.Width);
        double aspect=Math.Clamp(Width/(double)Math.Max(1,Height),.05,20);
        int h=Math.Max(1,(int)Math.Round(w/aspect));if(h>screen.Height){h=screen.Height;w=Math.Max(1,(int)Math.Round(h*aspect));}
        double x=Math.Clamp(X/(double)Math.Max(1,ScreenWidth-Width),0,1),y=Math.Clamp(Y/(double)Math.Max(1,ScreenHeight-Height),0,1);
        return new((int)Math.Round(x*(screen.Width-w)),(int)Math.Round(y*(screen.Height-h)),w,h);
    }
    internal Settings Apply(Settings s,Size screen){var r=Fit(screen);return s with{X=r.X,Y=r.Y,Width=r.Width,Height=r.Height};}
}
