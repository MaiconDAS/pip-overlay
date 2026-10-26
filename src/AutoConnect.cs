namespace PipOverlay;

// One attempt per source/game pair. A failed connection never loops in the background.
internal sealed class AutoConnect
{
    internal bool Enabled {get;private set;}=true;
    string? attempted;
    internal void Pause(){Enabled=false;}
    internal void Resume(){Enabled=true;attempted=null;}
    internal void Reset(){attempted=null;}
    internal bool TakeAttempt(string? source,int? target,bool busy)
    {
        if(!Enabled||busy)return false;
        if(source==null||target==null){attempted=null;return false;}
        string pair=$"{source}/{target}";
        if(pair==attempted)return false;
        attempted=pair;return true;
    }
}
