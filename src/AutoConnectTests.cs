namespace PipOverlay;
internal static class AutoConnectTests
{
    internal static int Run(string path){var results=new List<string>();int failed=0;void Check(string name,bool ok){results.Add($"{(ok?"PASS":"FAIL")} {name}");if(!ok)failed++;}
        var a=new AutoConnect();
        Check("Aguarda vídeo",!a.TakeAttempt(null,1,false));
        Check("Aguarda jogo",!a.TakeAttempt("video",null,false));
        Check("Conecta quando ambos chegam",a.TakeAttempt("video",1,false));
        Check("Erro não causa tentativas infinitas",!a.TakeAttempt("video",1,false));
        Check("Nova instância do jogo reconecta",a.TakeAttempt("video",2,false));
        a.Pause();Check("Pausa bloqueia novo vídeo",!a.TakeAttempt("outro",2,false));
        Check("Pausa bloqueia novo jogo",!a.TakeAttempt("outro",3,false));
        a.Resume();Check("Retomar permite conectar",a.TakeAttempt("outro",3,false));
        Check("Sessão ativa não duplica conexão",!a.TakeAttempt("novo",4,true));
        Check("Ausência reseta tentativa",!a.TakeAttempt(null,3,false));
        Check("Vídeo reaberto reconecta",a.TakeAttempt("outro",3,false));
        a.Reset();Check("Repetir manual permite tentativa",a.TakeAttempt("outro",3,false));
        var settings=new Settings{AutomaticEnabled=false};var saved=System.Text.Json.JsonSerializer.Serialize(settings);Check("Pausa persiste nas preferências",!System.Text.Json.JsonSerializer.Deserialize<Settings>(saved)!.AutomaticEnabled);
        Check("Preferências antigas habilitam automático",System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!.AutomaticEnabled);
        File.WriteAllLines(path,results);return failed==0?0:1;
    }
}
