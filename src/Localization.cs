using System.Globalization;
using System.Text.Json;

namespace PipOverlay;

// Source strings remain stable keys. Localization never changes persisted data keys.
internal static class L
{
    internal sealed record Language(string Code,string Name){public override string ToString()=>Name;}
    internal static readonly Language[] Languages=[new("pt-BR","Português (Brasil)"),new("en","English"),new("es","Español"),new("de","Deutsch"),new("it","Italiano"),new("fr","Français"),new("ja","日本語"),new("ko","한국어"),new("zh-CN","简体中文"),new("ru","Русский")];
    static readonly Dictionary<string,string[]> Catalog=Load();
    internal static string Current {get;private set;}="pt-BR";
    internal static string Normalize(string? code)=>Languages.Any(x=>x.Code==code)?code!:"pt-BR";
    internal static void Set(string? code){Current=Normalize(code);}
    internal static string T(string key)=>Catalog.TryGetValue(key,out var row)?row[Array.FindIndex(Languages,x=>x.Code==Current)]:key;
    internal static string F(string key,params object[] args)=>string.Format(CultureInfo.GetCultureInfo(Current),T(key),args);
    internal static IReadOnlyDictionary<string,string[]> Entries=>Catalog;
    static Dictionary<string,string[]> Load(){
        using var stream=typeof(L).Assembly.GetManifestResourceStream("PipOverlay.Locales.json")!;
        var rows=JsonSerializer.Deserialize<string[][]>(stream)!;
        if(rows.Any(r=>r.Length!=Languages.Length||r.Any(string.IsNullOrWhiteSpace)))throw new InvalidDataException("Invalid translation catalog");
        return rows.ToDictionary(r=>r[0],StringComparer.Ordinal);
    }
    // Rebind already-created static labels without restarting the capture session.
    internal static void Refresh(Control root,string previous){
        int index=Array.FindIndex(Languages,x=>x.Code==previous);
        string Translate(string value){var row=Catalog.Values.FirstOrDefault(r=>r[index]==value);return row==null?value:T(row[0]);}
        void Visit(Control c){
            c.Text=Translate(c.Text);if(c.AccessibleName is string a)c.AccessibleName=Translate(a);
            if(c is ComboBox box)for(int i=0;i<box.Items.Count;i++)if(box.Items[i] is string s)box.Items[i]=Translate(s);
            foreach(Control child in c.Controls)Visit(child);
        }
        Visit(root);
    }
    internal static void RefreshMenu(ToolStripItemCollection items,string previous){
        int index=Array.FindIndex(Languages,x=>x.Code==previous);
        foreach(ToolStripItem item in items){var row=Catalog.Values.FirstOrDefault(r=>r[index]==item.Text);if(row!=null)item.Text=T(row[0]);if(item is ToolStripDropDownItem drop)RefreshMenu(drop.DropDownItems,previous);}
    }
}
