namespace PipOverlay;
internal sealed partial class MainForm
{
    void ConfigureWorkspace(TableLayoutPanel old,Control header,Control footer,Button toggle,Button refresh,Button chooseGame,Button shortcuts,Button reset,Button copy){
        SuspendLayout();
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};
        root.RowStyles.Add(new(SizeType.Absolute,88));root.RowStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.Absolute,70));
        root.Controls.Add(header,0,0);root.Controls.Add(footer,0,2);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        body.ColumnStyles.Add(new(SizeType.Percent,100));body.ColumnStyles.Add(new(SizeType.Absolute,0));root.Controls.Add(body,0,1);
        var canvas=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};
        canvas.RowStyles.Add(new(SizeType.Absolute,42));canvas.RowStyles.Add(new(SizeType.Percent,100));canvas.RowStyles.Add(new(SizeType.Absolute,28));body.Controls.Add(canvas,0,0);
        var tools=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};
        tools.ColumnStyles.Add(new(SizeType.Percent,100));foreach(int width in new[]{112,110,104})tools.ColumnStyles.Add(new(SizeType.Absolute,width));
        profileLabel.Dock=DockStyle.Fill;profileLabel.Margin=Padding.Empty;tools.Controls.Add(profileLabel,0,0);
        Compact(centerLayout,"Centralizar");Compact(undoLayout,"Desfazer");Compact(pickVideo,"Vídeos");
        tools.Controls.Add(centerLayout,1,0);tools.Controls.Add(undoLayout,2,0);tools.Controls.Add(pickVideo,3,0);
        canvas.Controls.Add(tools,0,0);canvas.Controls.Add(editor,0,1);
        canvas.Controls.Add(new Label{Text="Arraste para mover · Puxe as bordas para redimensionar · Ctrl+Z desfaz",Dock=DockStyle.Fill,ForeColor=UiTheme.Muted,Font=new Font(Font.FontFamily,9),TextAlign=ContentAlignment.MiddleLeft},0,2);
        var tabs=new Panel{Dock=DockStyle.Fill,BackColor=UiTheme.Panel,Padding=new Padding(1),Margin=new Padding(14,0,0,0)};
        var navigation=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false,Padding=new Padding(5,2,0,0)};
        var pages=new Panel{Dock=DockStyle.Fill,BackColor=BackColor,Padding=new Padding(12)};tabs.Controls.Add(pages);tabs.Controls.Add(navigation);
        var sections=new List<(FlowLayoutPanel Page,Button Tab)>();
        FlowLayoutPanel Page(string name){
            var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Visible=sections.Count==0};pages.Controls.Add(flow);flow.SizeChanged+=(_,_)=>{foreach(Control child in flow.Controls)child.Width=Math.Max(160,flow.ClientSize.Width-child.Margin.Horizontal-2);};
            var tab=new ModernButton{Text=name,Width=88,Height=32,FlatStyle=FlatStyle.Flat,BackColor=UiTheme.Panel,ForeColor=sections.Count==0?UiTheme.Accent:UiTheme.Muted,Margin=Padding.Empty,Cursor=Cursors.Hand};tab.FlatAppearance.BorderSize=0;navigation.Controls.Add(tab);sections.Add((flow,tab));
            tab.Click+=(_,_)=>{foreach(var section in sections){section.Page.Visible=section.Page==flow;section.Tab.ForeColor=section.Page==flow?UiTheme.Accent:UiTheme.Muted;}flow.BringToFront();};return flow;
        }
        void Field(FlowLayoutPanel page,string label,Control control){control.Visible=true;control.Margin=Padding.Empty;control.AccessibleName=label;var field=UiTheme.Field(label,control,240);field.Margin=new Padding(0,0,0,8);page.Controls.Add(field);}
        void Item(FlowLayoutPanel page,Control control){control.Dock=DockStyle.None;control.Visible=true;if(control is ModernToggle)control.AutoSize=false;control.Margin=new Padding(0,4,0,10);if(control is Button button){Compact(button,button.Text);button.Width=258;}page.Controls.Add(control);}
        var picture=Page("Imagem");Field(picture,"RESOLUÇÃO · NITIDEZ",quality);Field(picture,"FPS · FLUIDEZ",frameRate);
        Item(picture,new Label{Text="Resolução e FPS são independentes.\n30 fps mantém a nitidez escolhida.",Width=258,Height=48,ForeColor=UiTheme.Muted});Item(picture,adaptiveBox);Field(picture,"MÍNIMO NO MODO AUTOMÁTICO",minimumFps);
        minimumFps.Enabled=adaptiveBox.Checked;adaptiveBox.CheckedChanged+=(_,_)=>minimumFps.Enabled=adaptiveBox.Checked;
        var position=Page("Posição");Field(position,"MONITOR",monitors);Item(position,dimensions);dimensions.AutoSize=false;dimensions.Size=new Size(258,35);Item(position,snapBox);Item(position,marginLabel);margin.Width=258;margin.TickStyle=TickStyle.None;Item(position,margin);Item(position,reset);Item(position,copy);
        var more=Page("Mais");Field(more,"VÍDEO DO NAVEGADOR",sources);Field(more,"JOGO CONECTADO",targets);Item(more,refresh);Item(more,chooseGame);Item(more,shortcuts);Item(more,updateChecks);Item(more,updates);Item(more,diagnostic);
        tabs.Visible=false;body.Controls.Add(tabs,1,0);
        toggle.Click+=(_,_)=>{bool open=!tabs.Visible;body.ColumnStyles[1].Width=open?316:0;tabs.Visible=open;toggle.Text=open?"Fechar":"Ajustes";};
        Controls.Remove(old);Controls.Add(root);old.Dispose();ResumeLayout(true);
    }
    static void Compact(Button button,string text){button.Text=text;button.Image?.Dispose();button.Image=null;button.Height=34;button.Width=104;button.Padding=new Padding(6,0,4,0);button.Margin=new Padding(0,3,6,3);button.Font=new Font("Segoe UI",9);}
}
