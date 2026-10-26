namespace PipOverlay;
internal static class HotkeyTests
{
    internal static int Run(string path,bool native){var results=new List<string>();int failed=0;void Check(string name,bool value){results.Add($"{(value?"PASS":"FAIL")} {name}");if(!value)failed++;}
        var a=new Shortcut((int)(Keys.Control|Keys.Alt|Keys.F23));var b=new Shortcut((int)(Keys.Control|Keys.Alt|Keys.F24));var c=new Shortcut((int)(Keys.Control|Keys.Shift|Keys.F22));
        Check("Combinação válida",a.Valid);Check("Tecla isolada rejeitada",!new Shortcut((int)Keys.A).Valid);Check("F12 rejeitada",!new Shortcut((int)(Keys.Control|Keys.F12)).Valid);
        var registrations=new Dictionary<int,Shortcut>();bool refuse=false;
        using(var manager=new Hotkeys(0,(id,key)=>{if(refuse&&key==c)return false;registrations[id]=key;return true;},id=>registrations.Remove(id))){
            Check("Registrar dois",manager.Apply(a,b,out _)&&registrations.Count==2);
            int idA=registrations.Single(x=>x.Value==a).Key;
            Check("Roteamento iniciar",manager.ActionFor(idA)==0);
            Check("Duplicados rejeitados sem perder registros",!manager.Apply(a,a,out _)&&registrations.Count==2);
            refuse=true;Check("Conflito preserva anteriores",!manager.Apply(a,c,out _)&&registrations.Values.Contains(b));
            Check("Trocar ações sem novo registro",manager.Apply(b,a,out _)&&manager.ActionFor(idA)==1&&registrations.Count==2);
            Check("Limpar atalhos",manager.Apply(new(0),new(0),out _)&&registrations.Count==0);
            refuse=false;manager.Apply(a,b,out _);
        }
        Check("Dispose libera registros",registrations.Count==0);
        var temp=path+".settings.json";try{new Settings{StartShortcut=a.Value,StopShortcut=b.Value}.Save(temp);var saved=Settings.Load(temp);Check("Persistência",saved.StartShortcut==a.Value&&saved.StopShortcut==b.Value);}finally{if(File.Exists(temp))File.Delete(temp);}
        if(native){using var first=new Form();using var second=new Form();using var one=new Hotkeys(first.Handle);using var two=new Hotkeys(second.Handle);bool ok=one.Apply(a,b,out var error);Check("Registro Windows em janelas temporárias: "+error,ok);if(ok){Check("Conflito Windows detectado",!two.Apply(a,new(0),out _));one.Dispose();Check("Atalho disponível após liberação",two.Apply(a,new(0),out _));}}
        results.Add($"Falhas: {failed}");File.WriteAllLines(path,results);return failed==0?0:1;
    }
}
