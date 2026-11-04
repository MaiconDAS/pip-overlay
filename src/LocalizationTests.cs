using System.Text.RegularExpressions;
namespace PipOverlay;
internal static class LocalizationTests
{
    internal static int Run(string report){
        var results=new List<string>();int failed=0;
        void Check(string name,bool ok){results.Add($"{(ok?"PASS":"FAIL")} {name}");if(!ok)failed++;}
        string previous=L.Current;string temp=Path.Combine(Path.GetTempPath(),"pip-languages-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try{
            Check("Ten unique languages",L.Languages.Select(x=>x.Code).Distinct().Count()==10);
            foreach(var entry in L.Entries){
                string Args(string text)=>string.Join(",",Regex.Matches(text,@"\{\d+(?::[^}]+)?\}").Select(x=>x.Value).Order());
                Check("Complete translations: "+entry.Key,entry.Value.Length==10&&entry.Value.All(x=>!string.IsNullOrWhiteSpace(x)&&Args(x)==Args(entry.Key)));
            }
            Check("Unknown language preserves Portuguese default",new Settings{Language="invalid"}.Validated().Language=="pt-BR");
            foreach(var language in L.Languages){
                var path=Path.Combine(temp,"settings.json");new Settings{Language=language.Code,Resolution="1080p",FramesPerSecond=30}.Save(path);
                var loaded=Settings.Load(path);Check(language.Code+" persistence",loaded.Language==language.Code&&loaded.Resolution=="1080p"&&loaded.FramesPerSecond==30);
            }
            L.Set("pt-BR");using var form=new MainForm(true);form.Show();Application.DoEvents();
            IEnumerable<Control> Walk(Control c){yield return c;foreach(Control child in c.Controls)foreach(var nested in Walk(child))yield return nested;}
            Walk(form).OfType<Button>().Single(b=>b.Text==L.T("Ajustes")).PerformClick();
            Walk(form).OfType<Button>().Single(b=>b.Text==L.T("Mais")).PerformClick();Application.DoEvents();
            var selector=Walk(form).OfType<ComboBox>().Single(c=>c.Items.Count==10&&c.Items[0] is L.Language);
            var quality=Walk(form).OfType<ComboBox>().Single(c=>c.Items.Count==4);int qualityIndex=quality.SelectedIndex;
            foreach(var language in L.Languages){selector.SelectedItem=language;Application.DoEvents();
                Check(language.Code+" live UI",L.Current==language.Code&&Walk(form).OfType<Button>().Any(b=>b.Text==L.T("Fechar"))&&Walk(form).OfType<Label>().Any(l=>l.Text==L.T("IDIOMA")));
                Check(language.Code+" preserves resolution selection",quality.SelectedIndex==qualityIndex);
                using var shot=new Bitmap(form.Width,form.Height);form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));shot.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!,"locale-"+language.Code+".png"));
            }
            form.Close();
        }catch(Exception ex){Check(ex.ToString(),false);}
        finally{L.Set(previous);Directory.Delete(temp,true);}
        File.WriteAllLines(report,results);return failed==0?0:1;
    }
}
