namespace PipOverlay;

// One initial attempt and at most two delayed retries for explicitly transient failures.
internal sealed class AutoConnect
{
    readonly Func<long> clock;
    internal AutoConnect(Func<long>? now=null){clock=now??(()=>Environment.TickCount64);}
    internal bool Enabled {get;private set;}=true;
    string? attempted;
    int retries;long retryAt=long.MaxValue;
    internal void Pause(){Enabled=false;retryAt=long.MaxValue;}
    internal void Resume(){Enabled=true;Reset();}
    internal void Reset(){attempted=null;retries=0;retryAt=long.MaxValue;}
    internal void RetryTransient(){if(Enabled&&attempted!=null&&retries<2&&retryAt==long.MaxValue)retryAt=clock()+(retries==0?3000:10000);}
    internal bool TakeAttempt(string? source,int? target,bool busy)
    {
        if(!Enabled||busy)return false;
        if(source==null||target==null){Reset();return false;}
        string pair=$"{source}/{target}";
        if(pair==attempted){if(clock()<retryAt||retries>=2)return false;retries++;retryAt=long.MaxValue;return true;}
        Reset();attempted=pair;return true;
    }
}
