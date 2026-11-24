using System; using System.IO; using System.Diagnostics;
namespace Gili { partial class IslandForm {
 string ProfileName {get{return settings.Get("profile.name","Usuario");}}
 string AssistantName {get{return settings.Get("profile.assistant","MANAURE");}}
 string PublicWorkspace {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Manaure","Data","workspace");}}
 void StartPublicBackend(){
    string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"backend","app.py");
    if(!File.Exists(path))return;
    try{
        var req=(System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://127.0.0.1:18765/");
        req.Proxy=null;req.Timeout=600;using(var res=req.GetResponse()){}return;
    }catch{}
    string args="\""+path+"\" "+Process.GetCurrentProcess().Id;
    try{Process.Start(new ProcessStartInfo("pyw.exe","-3 "+args){UseShellExecute=false,CreateNoWindow=true});}
    catch{try{Process.Start(new ProcessStartInfo("pythonw.exe",args){UseShellExecute=false,CreateNoWindow=true});}catch{}}
 }
} }
