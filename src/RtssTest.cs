using System.IO.MemoryMappedFiles;
using System.Drawing.Imaging;
using System.Text;

namespace PipOverlay;

internal static class RtssTest
{
    internal static int Run(string report)
    {
        var results=new List<string>();int failures=0;
        void Check(string name,Action test) {try {test();results.Add("PASS "+name);} catch(Exception e) {failures++;results.Add("FAIL "+name+": "+e.Message);} }
        void Assert(bool value) {if(!value) throw new Exception("Condição não satisfeita");}
        void Reject(Action action) {try {action();} catch(ArgumentException) {return;} catch(InvalidDataException) {return;} throw new Exception("Entrada inválida aceita");}
        Check("não cria servidor RTSS ausente",()=>{
            try {using var c=new RtssClient("Local\\PipOverlay-missing-"+Guid.NewGuid());}
            catch(FileNotFoundException) {return;}throw new Exception("Mapa inexistente aberto");
        });
        foreach(uint version in new uint[]{0x2000e,0x20014}) {
            using var fixture=new Fixture(version);using var client=new RtssClient(fixture.Name);
            var v=fixture.View;long slot=fixture.Start+fixture.Stride*2;long textOffset=version>=0x20014?266752:512;
            Check($"{version:X}: preserva slot reservado e outro cliente",()=>{
                Assert(client.Publish("frame 1"));Assert(v.ReadByte(fixture.Start)==0x55);
                Assert(v.ReadByte(fixture.Start+fixture.Stride+512)==0x66);
                Assert(v.ReadByte(slot+textOffset)==(byte)'f');Assert(v.ReadUInt32(32)==1);Assert(v.ReadInt32(36)==0);
                if(version>=0x20014) Assert(v.ReadByte(slot+512)==0);
            });
            Check($"{version:X}: texto menor termina sem resíduos",()=>{Assert(client.Publish("x"));Assert(v.ReadByte(slot+textOffset+1)==0);});
            Check($"{version:X}: ignora atualização enquanto renderer está ocupado",()=>{
                uint frame=v.ReadUInt32(32);v.Write(36,1);try {Assert(!client.Publish("busy"));Assert(v.ReadUInt32(32)==frame);Assert(v.ReadByte(slot+textOffset)==(byte)'x');}finally{v.Write(36,0);}
            });
            Check($"{version:X}: rejeita excesso e NUL antes de escrever",()=>{
                uint frame=v.ReadUInt32(32);Reject(()=>client.Publish(new string('x',4096)));Reject(()=>client.Publish("a\0b"));Assert(v.ReadUInt32(32)==frame);
            });
            Check($"{version:X}: liberação afeta apenas nosso slot",()=>{Assert(client.Release());Assert(v.ReadByte(slot+256)==0);Assert(v.ReadByte(slot+512)==0);Assert(v.ReadByte(fixture.Start)==0x55);Assert(v.ReadByte(fixture.Start+fixture.Stride+512)==0x66);});
            Check($"{version:X}: slot cheio libera bloqueio",()=>{
                v.Write(slot+256,(byte)'z');try {try {client.Publish("x");}catch(InvalidOperationException){Assert(v.ReadInt32(36)==0);return;}throw new Exception("Slot alheio sobrescrito");}finally{v.Write(slot+256,(byte)0);}
            });
        }
        Check("rejeita assinatura, versão, tamanho e tabelas sobrepostas",()=>{
            foreach(var change in new (int Offset,uint Value)[]{(0,0xDEAD),(4,0x2000d),(20,4000),(24,8),(28,uint.MaxValue),(12,128),(8,uint.MaxValue)}) {
                using var f=new Fixture(0x20014);f.View.Write(change.Offset,change.Value);
                Reject(()=>{using var c=new RtssClient(f.Name);});
            }
        });
        Check("descarte remove slot próprio",()=>{
            using var f=new Fixture(0x20014);using(var c=new RtssClient(f.Name)) Assert(c.Publish("bye"));Assert(f.View.ReadByte(f.Start+f.Stride*2+256)==0);
        });
        results.Add($"Resultado: {results.Count-failures}/{results.Count} testes aprovados. Somente memória temporária; RTSS real e renderização não testados.");
        File.WriteAllLines(report,results);return failures==0?0:1;
    }
    internal static int Probe(string report)
    {
        // Intentionally bounded experiment, not the production video backend.
        // Two persistent filenames prevent an unbounded texture/file cache.
        var log=new List<string>();
        try {
            using var client=new RtssClient(); // Fail before creating any assets if RTSS is absent.
            string dir=Path.Combine(AppContext.BaseDirectory,"rtss-probe");Directory.CreateDirectory(dir);
            var paths=new[]{Path.Combine(dir,"a.png"),Path.Combine(dir,"b.png")};
            foreach(var path in paths) if(path.IndexOfAny(new[]{'"','<','>'})>=0) throw new InvalidOperationException("Caminho incompatível com tags RTSS.");
            for(int i=0;i<2;i++) {
                using var bitmap=new Bitmap(320,180);using var g=Graphics.FromImage(bitmap);
                g.Clear(i==0?Color.DarkBlue:Color.DarkRed);
                using var font=new Font("Segoe UI",30,FontStyle.Bold);g.DrawString(i==0?"TESTE A":"TESTE B",font,Brushes.White,30,55);bitmap.Save(paths[i],ImageFormat.Png);
            }
            int accepted=0;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(timer.Elapsed.TotalSeconds<10) {
                int index=(int)(timer.ElapsedMilliseconds/500)%2;
                // Syntax verified in Overlay.cpp and HypertextExtension.cpp from the SDK source mirror.
                string text=$"<LI=\"{paths[index]}\"><I=320,180,0,0,320,180>";
                if(client.Publish(text)) accepted++;Thread.Sleep(50);
            }
            bool released=client.Release();
            log.Add($"Atualizações aceitas pela memória: {accepted}. Liberação imediata: {released}.");
            log.Add("Isso NÃO comprova que as imagens foram renderizadas. Conferir visualmente alternância A/B no host de teste RTSS.");
            log.Add("Ainda não testa vídeo, recarga de pixels no mesmo arquivo, fullscreen do jogo ou desempenho.");
            File.WriteAllLines(report,log);return accepted>0?0:1;
        } catch(Exception e) {
            log.Add("Não foi possível executar o teste RTSS: "+e.Message);
            log.Add("Nenhuma instalação, perfil do RTSS ou configuração do Windows foi alterada.");
            File.WriteAllLines(report,log);return 2;
        }
    }
    internal static int VideoProbe(string report)
    {
        try {
            var sources=WindowSource.Find().Where(x=>x.LikelyPip).ToArray();
            if(sources.Length!=1) throw new InvalidOperationException($"Esperado exatamente um PiP; encontrados {sources.Length}.");
            using var output=new RtssFrameOutput();using var capture=new WindowCapture(sources[0].Handle);
            var settings=new Settings {X=24,Y=24,Width=384,Height=216};
            var timer=System.Diagnostics.Stopwatch.StartNew();
            while(timer.Elapsed.TotalSeconds<20) {
                if(capture.Error is string error) throw new InvalidOperationException(error);
                using var frame=capture.TakeFrame();if(frame!=null) output.Publish(frame,settings,true);
                Application.DoEvents();Thread.Sleep(67);
            }
            File.WriteAllText(report,$"Capturados: {capture.Frames}; publicados: {output.PublishedFrames}. Duração: {timer.Elapsed.TotalSeconds:F1}s. Confirmação visual necessária.\n");
            return output.PublishedFrames>0?0:1;
        }catch(Exception e) {File.WriteAllText(report,e.ToString());return 2;}
    }
    sealed class Fixture : IDisposable
    {
        internal readonly string Name="Local\\PipOverlay-test-"+Guid.NewGuid();
        internal readonly int Start=128,Stride;
        readonly MemoryMappedFile map;
        internal readonly MemoryMappedViewAccessor View;
        internal Fixture(uint version) {
            Stride=version>=0x20014?299520:266752;
            map=MemoryMappedFile.CreateNew(Name,Start+Stride*3+128);View=map.CreateViewAccessor();
            View.Write(0,RtssClient.Signature);View.Write(4,version);View.Write(8,128u);View.Write(12,(uint)(Start+Stride*3));View.Write(16,1u);
            View.Write(20,(uint)Stride);View.Write(24,(uint)Start);View.Write(28,3u);
            View.Write(Start,(byte)0x55);View.Write(Start+Stride+256,(byte)'a');View.Write(Start+Stride+512,(byte)0x66);
        }
        public void Dispose(){View.Dispose();map.Dispose();}
    }
}
