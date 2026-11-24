using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Gili {
 partial class IslandForm {
    const int ModesPage=11;
    string activeMode="normal",modeInfo="Listo",gpuText="--",vramText="--",tempText="--",portsText="Comprobando...";
    bool modeSampling,wakeWanted,wakeRunning,wakeFailed,detachedDrawing;
    SpeechRecognitionEngine wakeEngine;
    DesktopPet petWindow;
    DateTime modeLast=DateTime.MinValue;
    bool QuietMode {get{return activeMode=="focus"||activeMode=="gaming";}}
    bool WorkMode {get{return activeMode=="gaming"||activeMode=="code";}}
    bool PetDetached {get{return petWindow!=null&&!petWindow.IsDisposed;}}
    void StartManaureExtras(){
        wakeWanted=settings.GetBool("wake.enabled",false);
        if(wakeWanted)RefreshWake();
    }
    void SetWake(bool enabled){
        wakeWanted=enabled;wakeFailed=false;
        SetSetting("wake.enabled",enabled);RefreshWake();Kick();
    }
    void StopWake(){
        var e=wakeEngine;wakeEngine=null;wakeRunning=false;
        if(e==null)return;
        try{e.RecognizeAsyncCancel();}catch{}
        try{e.Dispose();}catch{}
    }
    void RefreshWake(){
        bool allowed=wakeWanted&&!wakeFailed&&!QuietMode&&!jarvisBusy&&!jarvisListening&&!petSpeaking;
        if(!allowed){if(wakeEngine!=null)StopWake();return;}
        if(wakeEngine!=null)return;
        try{
            var engine=new SpeechRecognitionEngine(CultureInfo.GetCultureInfo("es-ES"));
            var builder=new GrammarBuilder("hola "+AssistantName);builder.Culture=CultureInfo.GetCultureInfo("es-ES");
            engine.LoadGrammar(new Grammar(builder));engine.SetInputToDefaultAudioDevice();
            engine.SpeechRecognized+=(s,e)=>Post(()=>{
                if(wakeEngine!=engine||!wakeWanted||jarvisBusy||petSpeaking||QuietMode||e.Result.Confidence<.72f)return;
                StopWake();OpenJarvis(true);
            });
            engine.RecognizeCompleted+=(s,e)=>Post(()=>{
                if(wakeEngine!=engine)return;
                StopWake();if(e.Error!=null){wakeFailed=true;modeInfo="Micrófono: "+e.Error.Message;}
                Kick();
            });
            wakeEngine=engine;engine.RecognizeAsync(RecognizeMode.Multiple);wakeRunning=true;
        }catch(Exception ex){StopWake();wakeFailed=true;modeInfo="Micrófono no disponible";Log.Write("wake: "+ex.Message);}
    }
    void SetManaureMode(string mode){
        activeMode=mode;
        if(mode=="focus"){
            if(!pomo.Active){pomo.SetMode(PomoMode.Focus);pomo.Start(DateTime.UtcNow);SavePomo();}
            modeInfo="Concentración: avisos de MANAURE reducidos";
        }else if(mode=="gaming")modeInfo="Gaming: avisos de MANAURE reducidos";
        else if(mode=="code")modeInfo="Programación: puertos y accesos locales";
        else modeInfo="Modo normal";
        if(QuietMode&&PetDetached)ToggleDesktopPet(false);
        RefreshWake();PollMode();Kick();
    }
    void PollMode(){
        RefreshWake();
        if(!WorkMode&&!(expanded&&page==ModesPage))return;
        if(modeSampling||(DateTime.UtcNow-modeLast).TotalSeconds<3)return;
        modeLast=DateTime.UtcNow;modeSampling=true;SampleStats();
        Task.Run(()=>{
            string gt="--",tt="--",vt="--",pt="--";
            try{
                var r=Post(Base+"/desktop",new{action="system"});
                gt=Value(r,"gpu");tt=Value(r,"temp");vt=Value(r,"vram");
                if(gt.Length>0)gt+="%";if(tt.Length>0)tt+="°C";
            }catch{}
            try{
                var list=IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                pt="";foreach(int port in new[]{4321,18765,11434,3000,5173}){
                    bool on=Array.Exists(list,x=>x.Port==port);
                    if(pt.Length>0)pt+=" · ";pt+=port+(on?" activo":" cerrado");
                }
            }catch{}
            Post(()=>{gpuText=gt;tempText=tt;vramText=vt;portsText=pt;modeSampling=false;Kick();});
        });
    }
    void DrawModeCompact(float w,float h){
        float cy=h/2;paint.Text(activeMode=="gaming"?"GAMING":"CÓDIGO",Theme.Semi(12),Theme.Green,16,cy);
        string metrics=activeMode=="gaming"?"CPU "+(int)(cpuLoad*100)+"%  ·  GPU "+gpuText+"  ·  "+tempText:"RAM "+(int)(ramLoad*100)+"%  ·  IA local";
        paint.TextRight(metrics,Theme.Ui(12),Theme.Text,w-16,cy);
    }
    void DrawModesPage(float w,float h){
        paint.Text("Modos y agentes",Theme.Semi(20),Theme.Text,20,61);
        paint.Text(modeInfo,Theme.Ui(11),Theme.Sub,20,84,w-40,StringAlignment.Near);
        string[] labels={"Normal","Gaming","Código","Focus"};
        string[] ids={"normal","gaming","code","focus"};
        Segmented("manauremode",20,114,labels,Array.IndexOf(ids,activeMode),true,i=>SetManaureMode(ids[i]));
        float bw=(w-44)/3;
        for(int i=0;i<3;i++){
            var rect=new RectangleF(18+i*(bw+4),140,bw,69);
            paint.FillRound(rect,14,Theme.Fill);
            paint.TextCenter(new[]{"CPU","GPU","RAM"}[i],Theme.Ui(11),Theme.Sub,rect.X+bw/2,157);
            paint.TextCenter(new[]{(int)(cpuLoad*100)+"%",gpuText,(int)(ramLoad*100)+"%"}[i],Theme.Semi(21),Theme.Text,rect.X+bw/2,183);
        }
        paint.Text("GPU: "+tempText+"   ·   VRAM: "+vramText,Theme.Ui(11),Theme.Sub,20,225);
        paint.Text(portsText,Theme.Ui(11),Theme.Sub,20,248,w-40,StringAlignment.Near);
        if(activeMode=="focus"){
            paint.Text("Focus · "+Mmss(pomo.Remaining(DateTime.UtcNow)),Theme.Semi(15),Theme.Green,20,273);
            PillButton(new RectangleF(w-155,258,135,30),"focusToggle",pomo.Running?"Pausar":"Continuar",Theme.Fill,Theme.Text,delegate{if(pomo.Running)pomo.Pause(DateTime.UtcNow);else pomo.Start(DateTime.UtcNow);SavePomo();Kick();});
        }else paint.Text("Acciones autorizadas",Theme.Semi(13),Theme.Text,20,273);
        float b=(w-44)/2;
        PillButton(new RectangleF(18,292,b,34),"modeWorkspace","Abrir espacio de trabajo",Theme.Fill,Theme.Text,delegate{RunAgentUi("abre espacio de trabajo");});
        PillButton(new RectangleF(26+b,292,b,34),"modeProject","Abrir proyectos",Theme.Fill,Theme.Text,delegate{RunAgentUi("abre proyectos");});
        PillButton(new RectangleF(18,335,b,34),"modeVoice",wakeWanted?"Desactivar saludo por voz":"Activar saludo por voz",Theme.Fill,wakeRunning?Theme.Green:Theme.Text,delegate{SetWake(!wakeWanted);});
        PillButton(new RectangleF(26+b,335,b,34),"modeDesktop",PetDetached?"Volver a la isla":"Mascota de escritorio",Theme.Fill,Theme.Text,delegate{ToggleDesktopPet(!PetDetached);});
        paint.Text(wakeRunning?"Micrófono activo · reconocimiento local":wakeWanted?(wakeFailed?"Micrófono no disponible":"Activación en pausa"):"Micrófono en espera: desactivado",Theme.Ui(11),Theme.Sub,20,387);
        paint.Text("Focus silencia avisos de MANAURE, no los de Windows.",Theme.Ui(11),Theme.Sub,20,409);
    }
    bool RunAgentUi(string text){
        string t=text.ToLowerInvariant().Trim(' ','.','!','?','¡','¿');
        string response=null;
        try{
            if(t=="modo focus"||t=="modo concentración"||t=="voy a estudiar"){SetManaureMode("focus");response="Modo Focus activado.";}
            else if(t=="modo gaming"||t=="voy a jugar"){SetManaureMode("gaming");response="Modo Gaming activado.";}
            else if(t=="modo programación"||t=="voy a programar"){SetManaureMode("code");response="Modo Programación activado.";}
            else if(t=="modo normal"||t=="salir de focus"){SetManaureMode("normal");response="Modo normal activado. El temporizador conserva su estado.";}
            else if(t=="abre espacio de trabajo"){Directory.CreateDirectory(PublicWorkspace);Process.Start("explorer.exe","\""+PublicWorkspace+"\"");response="Espacio de trabajo abierto.";}
            else if(t=="abre proyectos"){Process.Start("explorer.exe",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));response="Documentos abiertos.";}
            else if(t=="mascota de escritorio"||t=="saca la mascota"){ToggleDesktopPet(true);response=PetDetached?"Mascota en el escritorio.":"Desactiva Focus o Gaming para mostrarla.";}
            else if(t=="regresa a la isla"){ToggleDesktopPet(false);response="Mascota de vuelta en la isla.";}
            else if(t=="estado del equipo"){response="CPU "+(int)(cpuLoad*100)+"%, RAM "+(int)(ramLoad*100)+"%, GPU "+gpuText+", temperatura "+tempText+".";}
        }catch(Exception ex){response="No se pudo completar la acción: "+ex.Message;}
        if(response==null)return false;
        JarvisAdd("MANAURE",response);if(jarvisVoice)Speak(response);return true;
    }
    void ToggleDesktopPet(bool enabled){
        if(enabled&&!QuietMode&&MascotEnabled){
            if(!PetDetached){petWindow=new DesktopPet(this);petWindow.Show();}
        }else if(PetDetached){var w=petWindow;petWindow=null;w.Close();}
        Kick();
    }
    void DrawDesktopPet(Graphics gr,Rectangle bounds){
        if(IsDisposed)return;
        Graphics old=g,oldPaint=paint.G;float alpha=paint.Alpha;PointF oldOrigin=hitOrigin;
        try{
            detachedDrawing=true;g=gr;paint.G=gr;paint.Alpha=1;hitOrigin=PointF.Empty;
            gr.SmoothingMode=SmoothingMode.AntiAlias;
            if(DancerWanted)DrawDancer(bounds.Width/2f,bounds.Height/2f,102,Theme.DefaultAccent);
            else DrawSmartPet(bounds.Width/2f,bounds.Height/2f,102,Theme.DefaultAccent,false);
            danceScreenX=petWindow.Left+bounds.Width/2f;danceScreenY=petWindow.Top+bounds.Height/2f;dancePositionKnown=true;
        }finally{detachedDrawing=false;g=old;paint.G=oldPaint;paint.Alpha=alpha;hitOrigin=oldOrigin;}
    }
    void CloseManaureExtras(){StopWake();ToggleDesktopPet(false);}
    sealed class DesktopPet:Form{
        readonly IslandForm owner;readonly System.Windows.Forms.Timer tick=new System.Windows.Forms.Timer{Interval=40};
        bool dragging;Point down,start;int baseline;DateTime dragUntil;
        public DesktopPet(IslandForm owner){
            this.owner=owner;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
            BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;
            Size=new Size(150,150);StartPosition=FormStartPosition.Manual;
            var work=Screen.PrimaryScreen.WorkingArea;baseline=work.Bottom-Height;Location=new Point(work.Left+(work.Width-Width)/2,baseline);
            var menu=new ContextMenuStrip();menu.Items.Add("Regresar a la isla",null,(s,e)=>owner.ToggleDesktopPet(false));
            menu.Items.Add("Abrir MANAURE",null,(s,e)=>owner.OpenJarvis(false));ContextMenuStrip=menu;
            tick.Tick+=(s,e)=>{
                if(owner.IsDisposed){Close();return;}
                if(!owner.MascotEnabled||owner.QuietMode){owner.ToggleDesktopPet(false);return;}
                var area=Screen.FromControl(this).WorkingArea;
                if(!dragging&&DateTime.UtcNow>dragUntil&&owner.petMood==PetMood.Walk&&!owner.DancerWanted){
                    int target=area.Left+30+(int)((area.Width-Width-60)*Ease.Clamp01(owner.petX.Value));
                    int dx=target-Left;Left+=Math.Max(-7,Math.Min(7,dx));
                }
                owner.Kick();Invalidate();
            };
            tick.Start();
        }
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);owner.DrawDesktopPet(e.Graphics,ClientRectangle);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){dragging=true;down=Control.MousePosition;start=Location;Capture=true;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){var p=Control.MousePosition;Location=new Point(start.X+p.X-down.X,start.Y+p.Y-down.Y);}}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;dragUntil=DateTime.UtcNow.AddSeconds(15);}
        protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);owner.OpenJarvis(false);}
        protected override void OnFormClosed(FormClosedEventArgs e){tick.Stop();tick.Dispose();base.OnFormClosed(e);}
    }
 }
}
