namespace PipOverlay;
// Slow recovery avoids oscillation; frame content/rate is deliberately not a load signal.
internal sealed class AdaptiveFrameRate
{
    int slow,healthy,ceiling,floor; bool enabled;
    internal int Current {get;private set;}=60;
    internal void Configure(bool active,int minimum,int maximum){
        minimum=Math.Min(minimum,maximum);
        if(active!=enabled||minimum!=floor||maximum!=ceiling){enabled=active;floor=minimum;ceiling=maximum;Current=maximum;slow=healthy=0;}
    }
    internal int Sample(double processingMs){
        if(!enabled)return Current;
        double budget=1000.0/Current;
        if(processingMs>budget*.75){healthy=0;if(++slow>=3){Current=Math.Max(floor,Current==60?30:15);slow=0;}}
        else if(processingMs<budget*.25){slow=0;if(++healthy>=20){Current=Math.Min(ceiling,Current==15?30:60);healthy=0;}}
        else slow=healthy=0;
        return Current;
    }
}
