using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace Gili {
 partial class IslandForm {
    const int JarvisPage=9;
    const string Base="http://127.0.0.1:18765", Original="qwen3.5:4b", Other="qwen2.5:3b";
    readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=10000000};
    readonly List<KeyValuePair<string,string>> jarvisMessages=new List<KeyValuePair<string,string>>();
    string jarvisInput="",jarvisState="IA local",jarvisModel=Original;
    bool jarvisBusy,jarvisPinned,jarvisTyping,jarvisVoice=true,jarvisAllow,jarvisLoaded,waitingSong,jarvisVisionConfirm,jarvisListening;
    int jarvisScroll,jarvisTotalLines;

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr h);
    void OpenJarvis(bool voice){
        Expand(JarvisPage);jarvisPinned=true;
        if(!jarvisLoaded)LoadJarvisHistory();
        if(voice)ListenJarvis();else FocusJarvis();
    }
    void FocusJarvis(){
        jarvisTyping=true;jarvisPinned=true;leaveTimer.Stop();
        Native.SetWindowLong(Handle,-20,Native.GetWindowLong(Handle,-20)&~Native.WS_EX_NOACTIVATE);
        SetForegroundWindow(Handle);SetFocus(Handle);Focus();Kick();
    }
    void ReleaseJarvis(){
        jarvisTyping=false;jarvisPinned=false;jarvisVisionConfirm=false;
        Native.SetWindowLong(Handle,-20,Native.GetWindowLong(Handle,-20)|Native.WS_EX_NOACTIVATE);
    }
    void CloseJarvis(){ReleaseJarvis();expanded=false;Kick();}
    void JarvisAdd(string role,string text){
        if(IsDisposed)return;
        jarvisMessages.Add(new KeyValuePair<string,string>(role,text));
        while(jarvisMessages.Count>30)jarvisMessages.RemoveAt(0);
        chatRevision++;jarvisScroll=0;chatAnchorReply=role=="MANAURE";if(role=="MANAURE"&&jarvisBusy)PetEmotionFromText(text);Kick();
    }
    async void LoadJarvisHistory(){
        if(jarvisLoaded)return;jarvisLoaded=true;
        try{
            string raw=await Task.Run(()=>{using(var w=new WebClient()){w.Proxy=null;w.Encoding=Encoding.UTF8;return w.DownloadString(Base+"/history");}});
            if(IsDisposed)return;
            var list=json.Deserialize<Dictionary<string,object>[]>(raw);
            foreach(var m in list)JarvisAdd(Value(m,"role")=="user"?"TÚ":"MANAURE",Value(m,"content"));
            if(jarvisMessages.Count==0)JarvisAdd("MANAURE","Hola, "+ProfileName+". ¿En qué puedo ayudarte?");
        }catch{JarvisAdd("MANAURE","IA opcional: instala Python 3.10+ y Ollama, descarga qwen3.5:4b y reinicia la isla.");}
    }
    List<string> WrapJarvis(string text,float width,Font font){
        var lines=new List<string>();
        foreach(string paragraph in text.Replace("\r","").Split('\n')){
            string line="";
            foreach(string word in paragraph.Split(' ')){
                string test=line.Length==0?word:line+" "+word;
                if(paint.Measure(test,font)>width&&line.Length>0){lines.Add(line);line=word;}else line=test;
                while(line.Length>1&&paint.Measure(line,font)>width){int k=line.Length;while(k>1&&paint.Measure(line.Substring(0,k),font)>width)k--;lines.Add(line.Substring(0,k));line=line.Substring(k);}
            }
            lines.Add(line);
        }
        return lines;
    }
    void DrawJarvisPage(float w,float h){
        paint.Text("Hola, "+ProfileName,Theme.Semi(23),Theme.Text,22,62);
        paint.Text(AssistantName+" · "+jarvisState,Theme.Ui(12),Theme.Sub,23,86,w-150,StringAlignment.Near);
        DrawSmartPet(w-105,74,51,Theme.DefaultAccent,true);
        PillButton(new RectangleF(w-55,49,35,30),"jarvisClose","×",Theme.Fill,Theme.Text,CloseJarvis);
        float chatTop=105,chatBottom=h-133,chatH=chatBottom-chatTop;
        paint.FillRound(new RectangleF(18,chatTop,w-36,chatH),20,Theme.Fill);
        DrawChatMessages(w,chatTop,chatH);
        var font=Theme.Ui(13);
        float iy=chatBottom+10,sendW=82,micW=78,iw=w-36-sendW-micW-16;
        var ir=new RectangleF(18,iy,iw,40);
        paint.FillRound(ir,15,jarvisTyping?Theme.FillHover:Theme.Fill);
        string shown=jarvisInput;while(shown.Length>1&&paint.Measure(shown,font)>iw-24)shown=shown.Substring(1);
        paint.Text(shown.Length==0?"Escribe o pega tu mensaje…":shown+(jarvisTyping?"│":""),font,shown.Length==0?Theme.Sub:Theme.Text,30,iy+20,iw-22,StringAlignment.Near);
        AddHit(ir,"jarvisInput",FocusJarvis);
        PillButton(new RectangleF(26+iw,iy,sendW,40),"jarvisSend",jarvisBusy?"…":"Enviar",Theme.FillHover,Theme.Green,delegate{SendJarvis();});
        PillButton(new RectangleF(34+iw+sendW,iy,micW,40),"jarvisMic",jarvisListening?"Detener":"Hablar",Theme.Fill,Theme.Text,delegate{ListenJarvis();});
        float row=iy+50,bw=(w-60)/3;
        PillButton(new RectangleF(18,row,bw,30),"jarvisModel",jarvisModel==Original?"Qwen 4B":"Qwen 3B",Theme.Fill,Theme.Text,delegate{if(!jarvisBusy){jarvisModel=jarvisModel==Original?Other:Original;Kick();}});
        PillButton(new RectangleF(26+bw,row,bw,30),"jarvisVoice",jarvisVoice?"Voz activada":"Voz apagada",Theme.Fill,jarvisVoice?Theme.Green:Theme.Sub,delegate{jarvisVoice=!jarvisVoice;if(!jarvisVoice)StopSpeech();Kick();});
        PillButton(new RectangleF(34+2*bw,row,bw,30),"jarvisAllow",jarvisAllow?"Crear: autorizado":"Crear archivo: no",Theme.Fill,jarvisAllow?Theme.Green:Theme.Sub,delegate{jarvisAllow=!jarvisAllow;Kick();});
        float bottom=row+38;
        PillButton(new RectangleF(18,bottom,(w-44)/2,29),"jarvisVision",jarvisVisionConfirm?"Confirmar captura local":"Analizar pantalla",Theme.Fill,jarvisVisionConfirm?Theme.Orange:Theme.Text,delegate{VisionJarvis();});
        PillButton(new RectangleF(26+(w-44)/2,bottom,(w-44)/2,29),"jarvisStop","Detener voz",Theme.Fill,Theme.Text,delegate{StopSpeech();if(jarvisListening)ListenJarvis();});
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys key){
        if(expanded&&page==JarvisPage&&jarvisTyping){
            if(key==Keys.Enter){SendJarvis();return true;}
            if(key==Keys.Escape){CloseJarvis();return true;}
            if(key==Keys.Back){if(jarvisInput.Length>0)jarvisInput=jarvisInput.Substring(0,jarvisInput.Length-1);Kick();return true;}
            if(key==(Keys.Control|Keys.V)){try{if(Clipboard.ContainsText())jarvisInput=(jarvisInput+Clipboard.GetText()).Substring(0,Math.Min(8000,jarvisInput.Length+Clipboard.GetText().Length));}catch{}Kick();return true;}
            if(key==(Keys.Control|Keys.A)){jarvisInput="";Kick();return true;}
            if(key==(Keys.Control|Keys.C)){try{if(jarvisMessages.Count>0)Clipboard.SetText(jarvisMessages[jarvisMessages.Count-1].Value);}catch{}return true;}
        }
        return base.ProcessCmdKey(ref msg,key);
    }
    protected override void OnKeyPress(KeyPressEventArgs e){
        if(expanded&&page==JarvisPage&&jarvisTyping){if(!char.IsControl(e.KeyChar)&&jarvisInput.Length<8000){jarvisInput+=e.KeyChar;Kick();}e.Handled=true;return;}
        base.OnKeyPress(e);
    }
    async void SendJarvis(){
        string t=jarvisInput.Trim();if(jarvisBusy||t.Length==0)return;
        jarvisInput="";JarvisAdd("TÚ",t);if(RunAgentUi(t)){jarvisAllow=false;return;}bool permit=jarvisAllow;jarvisAllow=false;string chosen=jarvisModel;
        jarvisBusy=true;jarvisState="Pensando…";Kick();
        try{string a=await Task.Run(()=>Command(t,permit,chosen));JarvisAdd("MANAURE",a);if(jarvisVoice)Speak(a);}
        catch(Exception e){JarvisAdd("MANAURE","Error: "+e.Message);}
        finally{jarvisBusy=false;jarvisState="IA local";Kick();}
    }
    async void ListenJarvis(){
        if(jarvisListening){try{await Task.Run(()=>Post(Base+"/desktop",new{action="voice-stop"}));}catch{}return;}
        if(jarvisBusy)return;StopWake();StopSpeech();jarvisBusy=true;jarvisListening=true;jarvisState="Escuchando…";Kick();
        try{var r=await Task.Run(()=>Post(Base+"/desktop",new{action="listen"}));jarvisInput=Value(r,"text");if(jarvisInput.Length==0)JarvisAdd("MANAURE","No reconocí una frase.");}
        catch(Exception e){JarvisAdd("MANAURE","Micrófono: "+e.Message);}
        finally{jarvisListening=false;jarvisBusy=false;jarvisState="Revisa tu frase y envía";if(expanded&&page==JarvisPage)FocusJarvis();Kick();}
    }
    async void VisionJarvis(){
        if(jarvisBusy)return;
        if(!jarvisVisionConfirm){jarvisVisionConfirm=true;jarvisState="Cierra información privada y confirma la captura";Kick();return;}
        jarvisVisionConfirm=false;string question=jarvisInput.Trim();if(question.Length==0)question="Describe la pantalla y ofrece ayuda concreta.";
        jarvisInput="";JarvisAdd("TÚ","Analizar pantalla: "+question);jarvisBusy=true;jarvisState="Analizando pantalla local…";
        Native.ShowWindow(Handle,Native.SW_HIDE);await Task.Delay(400);
        try{
            string image;var bounds=Screen.PrimaryScreen.Bounds;
            using(var bmp=new Bitmap(bounds.Width,bounds.Height))using(var gr=Graphics.FromImage(bmp)){
                gr.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size);double ratio=Math.Min(1,1280.0/Math.Max(bounds.Width,bounds.Height));
                using(var scaled=new Bitmap(bmp,new Size((int)(bounds.Width*ratio),(int)(bounds.Height*ratio))))using(var ms=new MemoryStream()){scaled.Save(ms,ImageFormat.Jpeg);image=Convert.ToBase64String(ms.ToArray());}
            }
            Native.ShowWindow(Handle,Native.SW_SHOWNOACTIVATE);Kick();
            var r=await Task.Run(()=>Post("http://127.0.0.1:11434/api/chat",new{model=Original,stream=false,think=false,messages=new[]{new{role="user",content="Eres "+AssistantName+", asistente local. "+question,images=new[]{image}}},options=new{num_ctx=4096,num_predict=700}}));
            string a=Value((Dictionary<string,object>)r["message"],"content");JarvisAdd("MANAURE",a);if(jarvisVoice)Speak(a);
        }catch(Exception e){JarvisAdd("MANAURE","Visión: "+e.Message);}
        finally{Native.ShowWindow(Handle,Native.SW_SHOWNOACTIVATE);jarvisBusy=false;jarvisState="IA local";Kick();}
    }
        Dictionary<string,object> Post(string url,object payload) {
            var req=(HttpWebRequest)WebRequest.Create(url);req.Method="POST";req.ContentType="application/json";req.Timeout=660000;req.ReadWriteTimeout=660000;req.Proxy=null;
            if(url.StartsWith(Base+"/"))req.Headers["Origin"]=Base;
            byte[] b=Encoding.UTF8.GetBytes(json.Serialize(payload));req.ContentLength=b.Length;
            using(var st=req.GetRequestStream())st.Write(b,0,b.Length);
            try{using(var r=req.GetResponse())using(var sr=new StreamReader(r.GetResponseStream()))return json.Deserialize<Dictionary<string,object>>(sr.ReadToEnd());}
            catch(WebException e){if(e.Response==null)throw;using(var r=e.Response)using(var sr=new StreamReader(r.GetResponseStream()))throw new Exception(sr.ReadToEnd());}
        }
        static string Value(Dictionary<string,object> r,string k){object v;return r.TryGetValue(k,out v)&&v!=null?v.ToString():"";}

        string Command(string t,bool permit,string chosen){
            string n=t.ToLowerInvariant().Trim(' ','.','!','?','¿');
            if(waitingSong){waitingSong=false;if(n=="cancelar")return "Búsqueda cancelada.";return Music("play",t);}
            if(n=="quiero escuchar youtube"||n=="quiero escuchar música"||n=="youtube"){waitingSong=true;return "Claro, "+ProfileName+". ¿Qué canción o artista quieres escuchar?";}
            if(n.StartsWith("quiero escuchar ")||(n.StartsWith("ponme ")&&n.EndsWith(" en youtube"))){
                int pos=(n.StartsWith("ponme ")&&n.EndsWith(" en youtube"))?6:16;string q=t.Substring(pos).Trim();
                if(q.EndsWith(" en YouTube",StringComparison.OrdinalIgnoreCase))q=q.Substring(0,q.Length-11);
                return Music("play",q);
            }
            if(n=="pausa la música"||n=="pausa youtube")return Music("pause","");
            if(n=="siguiente canción")return Music("next","");
            var apps=new Dictionary<string,string>{{"abre calculadora","calc.exe"},{"abre bloc de notas","notepad.exe"},{"abre explorador","explorer.exe"},{"abre configuración","ms-settings:"}};
            if(apps.ContainsKey(n)){Process.Start(apps[n]);return "Aplicación abierta.";}
            if(n=="abre spotify")return Value(Post(Base+"/desktop",new{action="spotify"}),"message");
            var volume=new Dictionary<string,string>{{"sube el volumen","up"},{"baja el volumen","down"},{"silencia el volumen","mute"}};
            if(volume.ContainsKey(n)){Post(Base+"/desktop",new{action="volume",command=volume[n]});return "Volumen actualizado.";}
            var r=Post(Base+"/chat",new{message=t,allow_write=permit,model=chosen});return Value(r,"answer");
        }
        string Music(string command,string query){var r=Post(Base+"/desktop",new{action="media",command=command,query=query});return Value(r,"message");}

        void StopSpeech(){StopPetSpeech();}
        void Speak(string text){SpeakPet(text);}


 }
}
