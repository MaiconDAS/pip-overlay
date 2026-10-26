using System.Runtime.InteropServices;
namespace PipOverlay;

internal readonly record struct Shortcut(int Value)
{
    internal Keys Key=>(Keys)(Value&(int)Keys.KeyCode);
    internal uint Modifiers=>((Value&(int)Keys.Control)!=0?2u:0)|((Value&(int)Keys.Alt)!=0?1u:0)|((Value&(int)Keys.Shift)!=0?4u:0);
    internal bool Valid=>Value==0||((Value&~((int)Keys.KeyCode|(int)Keys.Control|(int)Keys.Alt|(int)Keys.Shift))==0&&Modifiers!=0&&
        ((Key>=Keys.A&&Key<=Keys.Z)||(Key>=Keys.D0&&Key<=Keys.D9)||(Key>=Keys.NumPad0&&Key<=Keys.NumPad9)||(Key>=Keys.F1&&Key<=Keys.F24&&Key!=Keys.F12)));
    public override string ToString()=>Value==0?"Não definido":$"{((Modifiers&2)!=0?"Ctrl + ":"")}{((Modifiers&1)!=0?"Alt + ":"")}{((Modifiers&4)!=0?"Shift + ":"")}{Key}";
}

internal sealed class Hotkeys : IDisposable
{
    readonly nint window;
    readonly Func<int,Shortcut,bool> register;
    readonly Action<int> unregister;
    readonly Dictionary<int,(int Id,Shortcut Key)> active=new();
    int nextId=1000;
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(nint hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(nint hwnd,int id);
    internal Hotkeys(nint hwnd,Func<int,Shortcut,bool>? add=null,Action<int>? remove=null){window=hwnd;register=add??((id,key)=>RegisterHotKey(window,id,key.Modifiers|0x4000,(uint)key.Key));unregister=remove??(id=>{UnregisterHotKey(window,id);});}
    internal int ActionFor(int id){foreach(var item in active)if(item.Value.Id==id)return item.Key;return -1;}
    internal Shortcut ShortcutFor(int id)=>active.Values.FirstOrDefault(x=>x.Id==id).Key;
    internal bool Apply(Shortcut start,Shortcut stop,out string error){
        error="";
        if(!start.Valid||!stop.Valid){error="Use Ctrl, Alt ou Shift com uma letra, número ou tecla F (exceto F12).";return false;}
        if(start.Value!=0&&start==stop){error="Escolha atalhos diferentes para iniciar e parar.";return false;}
        var next=new Dictionary<int,(int Id,Shortcut Key)>();var created=new List<int>();
        foreach(var pair in new[]{(Action:0,Key:start),(Action:1,Key:stop)}) {
            if(pair.Key.Value==0)continue;
            var existing=active.Values.FirstOrDefault(x=>x.Key==pair.Key);
            if(existing.Id!=0){next[pair.Action]=existing;continue;}
            int id=nextId++;if(nextId>0xBFFF)nextId=1000;
            if(!register(id,pair.Key)){foreach(int allocated in created)unregister(allocated);error=$"{pair.Key} não está disponível. Escolha outra combinação. Seus atalhos anteriores foram mantidos.";return false;}
            created.Add(id);next[pair.Action]=(id,pair.Key);
        }
        foreach(var item in active.Values)if(!next.Values.Any(x=>x.Id==item.Id))unregister(item.Id);
        active.Clear();foreach(var item in next)active.Add(item.Key,item.Value);return true;
    }
    public void Dispose(){foreach(var item in active.Values)unregister(item.Id);active.Clear();}
}

internal sealed class ShortcutDialog : Form
{
    readonly Button first=new(),second=new();
    readonly Label hint=new(){AutoSize=false,Size=new Size(500,65),ForeColor=UiTheme.Muted};
    Shortcut start,stop;
    int recording=-1;
    internal ShortcutDialog(Shortcut initialStart,Shortcut initialStop,Func<Shortcut,Shortcut,string?> apply){
        start=initialStart;stop=initialStop;Text="Atalhos de teclado";ClientSize=new Size(550,330);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(15,20,30);ForeColor=Color.White;Font=new Font("Segoe UI",10);KeyPreview=true;
        var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),FlowDirection=FlowDirection.TopDown,WrapContents=false};Controls.Add(panel);
        panel.Controls.Add(new Label{Text="Controle o vídeo sem sair do jogo",AutoSize=true,Font=new Font("Segoe UI Semibold",15)});
        panel.Controls.Add(Row("Iniciar PiP",first,0));panel.Controls.Add(Row("Parar PiP",second,1));
        hint.Text="Clique em um atalho e pressione a combinação desejada.\nA ferramenta deve permanecer aberta. O vídeo já deve estar em PiP no navegador.";panel.Controls.Add(hint);
        var buttons=new FlowLayoutPanel{Width=500,Height=50};var save=new Button();UiTheme.Button(save,"Salvar atalhos","play",true);save.Click+=(_,_)=>{recording=-1;RefreshLabels();var error=apply(start,stop);if(error!=null){hint.Text=error;return;}DialogResult=DialogResult.OK;Close();};var cancel=new Button();UiTheme.Button(cancel,"Cancelar","stop");cancel.Click+=(_,_)=>Close();buttons.Controls.Add(save);buttons.Controls.Add(cancel);panel.Controls.Add(buttons);RefreshLabels();
    }
    Control Row(string title,Button button,int index){var row=new FlowLayoutPanel{Width=500,Height=58,Padding=new Padding(0,10,0,0)};row.Controls.Add(new Label{Text=title,Width=100,Height=34,TextAlign=ContentAlignment.MiddleLeft});UiTheme.Button(button,"","pip");button.Width=255;button.Height=36;button.Click+=(_,_)=>{recording=index;RefreshLabels();button.Text="Pressione a combinação…";hint.Text="Use Ctrl, Alt ou Shift + letra, número ou F1–F24 (exceto F12). Esc cancela a captura.";};row.Controls.Add(button);var clear=new Button{Text="Limpar",Size=new Size(75,36),FlatStyle=FlatStyle.Flat,ForeColor=UiTheme.Muted};clear.Click+=(_,_)=>{if(index==0)start=new(0);else stop=new(0);recording=-1;RefreshLabels();};row.Controls.Add(clear);return row;}
    void RefreshLabels(){first.Text=start.ToString();second.Text=stop.ToString();}
    internal void ReceiveRegisteredShortcut(Shortcut chord){if(recording<0)return;if(recording==0)start=chord;else stop=chord;recording=-1;RefreshLabels();hint.Text="Clique em Salvar atalhos para aplicar.";}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(recording<0)return base.ProcessCmdKey(ref msg,keyData);if(keyData==Keys.Escape){recording=-1;RefreshLabels();return true;}var chord=new Shortcut((int)keyData);if(chord.Valid&&chord.Value!=0){if(recording==0)start=chord;else stop=chord;recording=-1;RefreshLabels();hint.Text="Clique em Salvar atalhos para aplicar.";}return true;}
}
