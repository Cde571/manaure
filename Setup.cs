using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;

static class Setup {
 static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Manaure");
 static string Profile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Manaure");
 static string Menu=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"MANAURE Community");
 static string Link=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"MANAURE Community.lnk");
 const string UninstallKey=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ManaureCommunity";
 [STAThread] static void Main(string[] args){
  Application.EnableVisualStyles();
  if(args.Length==2&&args[0]=="--check"){Extract(args[1]);return;}
  if(args.Length==1&&args[0]=="--uninstall"){
    if(MessageBox.Show("¿Desinstalar MANAURE? Tus datos personales se conservarán.","MANAURE",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
    try{
      StopInstalled();
      if(File.Exists(Link))File.Delete(Link);
      if(Directory.Exists(Menu))Directory.Delete(Menu,true);
      Registry.CurrentUser.DeleteSubKeyTree(UninstallKey,false);
      using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)){if(key!=null)key.DeleteValue("Manaure",false);}
      // Self-executable is removed after it exits, without deleting any user data.
      var script=Path.Combine(Path.GetTempPath(),"Manaure-uninstall-"+Guid.NewGuid().ToString("N")+".cmd");
      File.WriteAllText(script,"@echo off\r\nping 127.0.0.1 -n 3 >nul\r\nrmdir /s /q \""+Root+"\"\r\ndel \"%~f0\"\r\n",Encoding.Default);
      Process.Start(new ProcessStartInfo("cmd.exe","/c \""+script+"\""){CreateNoWindow=true,UseShellExecute=false});
    }catch(Exception ex){MessageBox.Show(ex.Message,"Error");}
    return;
  }
  var form=new Form{Text="MANAURE | Instalación",ClientSize=new Size(490,345),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,BackColor=Color.FromArgb(23,25,33),ForeColor=Color.White,Font=new Font("Segoe UI",10)};
  form.Controls.Add(new Label{Text="MANAURE",Font=new Font("Segoe UI",27,FontStyle.Bold),Location=new Point(24,18),AutoSize=true});
  form.Controls.Add(new Label{Text="Tu isla dinámica. Tu asistente. Tu nombre.",Location=new Point(27,77),AutoSize=true,ForeColor=Color.Silver});
  form.Controls.Add(new Label{Text="¿Cómo te llamas?",Location=new Point(27,113),AutoSize=true});
  var name=new TextBox{Location=new Point(27,137),Size=new Size(434,28),MaxLength=32};form.Controls.Add(name);
  form.Controls.Add(new Label{Text="Nombre del asistente",Location=new Point(27,177),AutoSize=true});
  var assistant=new TextBox{Location=new Point(27,201),Size=new Size(434,28),MaxLength=32,Text="MANAURE"};form.Controls.Add(assistant);
  form.Controls.Add(new Label{Text="Gratis · MIT · Sin permisos de administrador\nIA opcional: Python y Ollama por separado.",Location=new Point(27,241),Size=new Size(434,44),ForeColor=Color.Silver});
  var install=new Button{Text="Instalar y abrir",Location=new Point(27,293),Size=new Size(434,34),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(121,232,194),ForeColor=Color.FromArgb(18,24,32)};
  form.Controls.Add(install);form.AcceptButton=install;
  install.Click+=(s,e)=>{
    string n=name.Text.Trim(),a=assistant.Text.Trim();
    if(n.Length==0||a.Length==0){MessageBox.Show("Escribe ambos nombres.");return;}
    foreach(char c in n+a)if(char.IsControl(c)||c=='='){MessageBox.Show("Usa nombres sin caracteres de control ni =.");return;}
    install.Enabled=false;
    try{
      StopInstalled();Extract(Root);Directory.CreateDirectory(Profile);
      var settings=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
      var cfg=Path.Combine(Profile,"settings.ini");
      if(File.Exists(cfg))foreach(var line in File.ReadAllLines(cfg,Encoding.UTF8)){int pos=line.IndexOf('=');if(pos>0&&!line.StartsWith("#"))settings[line.Substring(0,pos)]=line.Substring(pos+1);}
      settings["profile.name"]=n;settings["profile.assistant"]=a;
      foreach(var key in new[]{"wake.enabled","clipboard","notif.content","location.auto","weather"})if(!settings.ContainsKey(key))settings[key]="0";
      if(!settings.ContainsKey("pet.character"))settings["pet.character"]="3";
      var lines=new List<string>();foreach(var item in settings)lines.Add(item.Key+"="+item.Value);
      File.WriteAllLines(cfg,lines,new UTF8Encoding(false));
      var self=Path.Combine(Root,"Uninstall.exe");File.Copy(Assembly.GetExecutingAssembly().Location,self,true);
      Directory.CreateDirectory(Menu);
      string exe=Path.Combine(Root,"Gili.exe");
      Shortcut(Link,exe,"");Shortcut(Path.Combine(Menu,"MANAURE.lnk"),exe,"");
      Shortcut(Path.Combine(Menu,"Desinstalar MANAURE.lnk"),self,"--uninstall");
      using(var key=Registry.CurrentUser.CreateSubKey(UninstallKey)){
        key.SetValue("DisplayName","MANAURE Community");key.SetValue("DisplayVersion","1.0.0");key.SetValue("UninstallString","\""+self+"\" --uninstall");key.SetValue("InstallLocation",Root);key.SetValue("DisplayIcon",exe);key.SetValue("NoModify",1);key.SetValue("NoRepair",1);
      }
      Process.Start(new ProcessStartInfo(exe){WorkingDirectory=Root});form.Close();
    }catch(Exception ex){MessageBox.Show(ex.Message,"No se pudo instalar");install.Enabled=true;}
  };
  Application.Run(form);
 }
 static void StopInstalled(){
  foreach(var p in Process.GetProcessesByName("Gili"))try{if(string.Equals(p.MainModule.FileName,Path.Combine(Root,"Gili.exe"),StringComparison.OrdinalIgnoreCase)){p.CloseMainWindow();if(!p.WaitForExit(1500)){p.Kill();p.WaitForExit();}}}catch{}
 }
 static void Extract(string dest){
  Directory.CreateDirectory(dest);string prefix=Path.GetFullPath(dest).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
  using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
    foreach(var e in zip.Entries){
      if(e.Name.Length==0)continue;
      string file=Path.GetFullPath(Path.Combine(dest,e.FullName));
      if(!file.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Ruta inválida.");
      Directory.CreateDirectory(Path.GetDirectoryName(file));using(var input=e.Open())using(var output=File.Create(file))input.CopyTo(output);
    }
  }
 }
 static void Shortcut(string path,string target,string args){
  dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
  dynamic link=shell.CreateShortcut(path);link.TargetPath=target;link.Arguments=args;link.WorkingDirectory=Root;link.IconLocation=target+",0";link.Save();
 }
}
