using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace Gili {
 partial class IslandForm {
    const int PetPage=10;
    enum PetMood { Walk, Sit, Sleep, Curious, Listening, Thinking, Talking, Happy, Concerned }
    readonly Spring petX=new Spring(.5f,18,9,.002f), petSit=new Spring(0,85,19,.005f),
        petSleep=new Spring(0,65,16,.005f), petWave=new Spring(0,105,18,.005f),
        petMouth=new Spring(0,250,25,.005f), petExpression=new Spring(0,95,19,.005f);
    PetMood petMood=PetMood.Sit;
    double petClock;
    bool petSpeaking;
    float visemeOpen,visemeWidth=1;
    SpeechSynthesizer petSynth;
    Prompt petPrompt;
    DateTime petEmotionUntil;
    int petEmotion;
    int PetChoice(string key,int count){return Math.Max(0,Math.Min(count-1,settings.GetInt("pet."+key,0)));}
    int PetDanceStyle {get{return PetChoice("dance",4);}}
    bool PetLive {get{return MascotEnabled&&(PetDetached||current==View.Eyes||(expanded&&(page==JarvisPage||page==PetPage)));}}
    bool PetNeedsAttention {get{return MascotEnabled&&(petSpeaking||jarvisListening||jarvisBusy);}}
    void PetEmotionFromText(string text){
        if(string.IsNullOrWhiteSpace(text))return;
        string t=text.ToLowerInvariant();
        if(Regex.IsMatch(t,@"\b(error|no pude|no puedo|lo siento|problema|falló|fallo)\b"))petEmotion=-1;
        else if(Regex.IsMatch(t,@"\b(genial|perfecto|listo|felicidades|excelente|gracias|claro|éxito)\b"))petEmotion=1;
        else petEmotion=0;
        petEmotionUntil=DateTime.UtcNow.AddSeconds(12);
    }
    void StepPet(float dt){
        if(!PetLive&&!PetNeedsAttention&&!DancerWanted)return;
        petClock+=dt;var now=DateTime.UtcNow;
        double idle=userIdleSeconds,cycle=petClock%22;
        bool near=false;
        Native.POINT p;
        if(dancePositionKnown&&Native.GetCursorPos(out p)){
            double dx=p.X-danceScreenX,dy=p.Y-danceScreenY;
            near=dx*dx+dy*dy<Math.Pow(130*scale,2);
        }
        if(petSpeaking)petMood=PetMood.Talking;
        else if(jarvisListening)petMood=PetMood.Listening;
        else if(jarvisBusy)petMood=PetMood.Thinking;
        else if(DancerWanted)petMood=near?PetMood.Curious:PetMood.Happy;
        else if(idle>=180&&!near)petMood=PetMood.Sleep;
        else if(near)petMood=PetMood.Curious;
        else if(now<petEmotionUntil&&petEmotion!=0)petMood=petEmotion>0?PetMood.Happy:PetMood.Concerned;
        else petMood=cycle<6||cycle>=11&&cycle<17?PetMood.Walk:PetMood.Sit;
        if(petMood==PetMood.Walk){
            double k=cycle<6?cycle/6:(cycle-11)/6;
            float eased=SmoothD((float)k);
            petX.Target=cycle<6?.10f+.80f*eased:.90f-.80f*eased;
        } else if(petMood==PetMood.Sit)petX.Target=cycle<11?.90f:.10f;
        else if(petMood==PetMood.Sleep)petX.Target=.90f;
        petSit.Target=petMood==PetMood.Sit||petMood==PetMood.Sleep?1:0;
        petSleep.Target=petMood==PetMood.Sleep?1:0;
        petWave.Target=petMood==PetMood.Curious?1:0;
        petExpression.Target=petMood==PetMood.Concerned?-1:petMood==PetMood.Happy||petMood==PetMood.Curious?1:0;
        petMouth.Target=petSpeaking?visemeOpen:0;
        petX.Step(dt);petSit.Step(dt);petSleep.Step(dt);petWave.Step(dt);petMouth.Step(dt);petExpression.Step(dt);
    }
    string PetStatus(){
        switch(petMood){
          case PetMood.Walk:return "Paseando";
          case PetMood.Sit:return "Descansando";
          case PetMood.Sleep:return "Durmiendo";
          case PetMood.Curious:return "¡Hola!";
          case PetMood.Listening:return "Escuchando";
          case PetMood.Thinking:return "Pensando";
          case PetMood.Talking:return "Hablando";
          case PetMood.Happy:return "Contento";
          default:return "Atento";
        }
    }
    Color PetBody(Color accent){
        switch(PetChoice("outfit",4)){
            case 1:return Color.FromArgb(248,243,233);
            case 2:return Color.FromArgb(118,164,239);
            case 3:return Color.FromArgb(173,130,232);
            default:return Theme.Mix(Theme.Text,accent,.24f);
        }
    }
    void PetClothes(Color accent){
        if(PetChoice("outfit",4)==0)return;
        paint.FillRound(new RectangleF(-8,-11,16,10),4,Theme.WithAlpha(accent,.75f));
        paint.FillRound(new RectangleF(-2,-11,4,10),1,Theme.WithAlpha(Color.White,.4f));
        paint.FillCircle(4,-7,1.3f,Color.White);
    }
    void PetCharacterDetails(Color body,Color accent){
        int c=PetChoice("character",4);
        if(c==1){
            using(var p=new GraphicsPath()){
                p.AddPolygon(new[]{new PointF(-12,-12),new PointF(-12,-23),new PointF(-3,-15)});
                p.AddPolygon(new[]{new PointF(12,-12),new PointF(12,-23),new PointF(3,-15)});
                g.FillPath(paint.B(body),p);
            }
        }else if(c==2){
            using(var path=Theme.Round(new RectangleF(-14,-17,28,22),10))
                g.DrawPath(paint.P(Theme.Mix(accent,Color.White,.7f),1.5f),path);
        }
    }
    void PetAccessories(Color accent){
        switch(PetChoice("accessory",4)){
            case 0:
                paint.FillRound(new RectangleF(-15,-11,4,9),2,accent);
                paint.FillRound(new RectangleF(11,-11,4,9),2,accent);
                g.DrawArc(paint.P(accent,1.8f),-12,-19,24,16,180,180);break;
            case 1:
                paint.FillRound(new RectangleF(-10,-9.8f,9,5),2,Color.FromArgb(45,55,73));
                paint.FillRound(new RectangleF(1,-9.8f,9,5),2,Color.FromArgb(45,55,73));
                g.DrawLine(paint.P(accent,1),-1,-7,1,-7);break;
            case 2:
                paint.FillRound(new RectangleF(-12,-20,24,6),3,accent);
                paint.FillRound(new RectangleF(0,-15,16,2.5f),1,accent);break;
            case 3:break;
        }
    }
    void PetFace(Color body,Color glow,Color accent,bool dancing){
        if(KnightSelected){DrawKnightFace(accent,dancing);return;}
        float closed=Ease.Clamp01(petSleep.Value),open=Math.Max(.08f,Math.Min(1,danceBlink.Value*(1-closed)));
        float eyeH=5.7f*open;
        PetCharacterDetails(body,accent);
        paint.FillRound(new RectangleF(-13,-16,26,19),7,body);
        paint.FillRound(new RectangleF(-10,-13.5f,20,13),5.5f,Color.FromArgb(24,26,32));
        for(int i=-1;i<=1;i+=2){
            float ex=i*4.5f,ey=-7.5f;
            if(closed>.8f){g.DrawArc(paint.P(glow,1.3f),ex-2.5f,ey-1,5,2.5f,0,180);continue;}
            paint.FillRound(new RectangleF(ex-2.8f,ey-eyeH/2,5.6f,eyeH),Math.Min(2.5f,eyeH/2),glow);
            if(open>.45f){
                float px=ex+danceGazeX.Value*1.15f,py=ey+danceGazeY.Value*open*1.05f;
                paint.FillCircle(px,py,1.3f,Color.FromArgb(24,26,32));paint.FillCircle(px-.35f,py-.4f,.48f,Color.White);
            }
        }
        if(petMouth.Value>.08f){
            float mh=.6f+petMouth.Value*2.5f,mw=Math.Max(1.5f,Math.Min(5.5f,visemeWidth*4));
            g.FillEllipse(paint.B(glow),-mw/2,-2.9f-mh/2,mw,mh);
        } else if(petMood==PetMood.Thinking){paint.FillCircle(0,-3,1.1f,glow);}
        else if(petExpression.Value<-.35f)g.DrawArc(paint.P(glow,1.1f),-2.5f,-3.4f,5,2.6f,180,180);
        else g.DrawArc(paint.P(glow,1.1f),-2.5f,-4,5,2.6f,0,180);
        PetAccessories(accent);
    }
    float WalkFoot(double p){
        p=(p%1+1)%1;return p<.60?LerpD(5,-5,(float)(p/.60)):LerpD(-5,5,SmoothD((float)((p-.60)/.40)));
    }
    float WalkLift(double p){p=(p%1+1)%1;return p<.60?0:3.2f*(float)Math.Sin(Math.PI*(p-.60)/.40);}
    void DrawSmartPet(float cx,float cy,float size,Color accent,bool stationary){
        if(!MascotEnabled||(PetDetached&&!detachedDrawing&&!stationary))return;
        float sit=Ease.Clamp01(petSit.Value),sleep=Ease.Clamp01(petSleep.Value),wave=Ease.Clamp01(petWave.Value);
        bool walk=!stationary&&petMood==PetMood.Walk;
        float dir=KnightSelected?knightTurn.Value:petX.Velocity<0?-1:1;
        double step=KnightSelected?knightStride:petClock*1.65;
        float walkAmount=KnightSelected&&!stationary?Ease.Clamp01(knightWalk.Value):walk?Math.Min(1,Math.Abs(petX.Velocity)*9):0;
        float hx=0,hy=7+sit*4+(float)Math.Sin(step*4*Math.PI)*walkAmount*.65f;
        float lx=-7+WalkFoot(step)*walkAmount*dir,rx=7+WalkFoot(step+.5)*walkAmount*dir;SeparateFeet(ref lx,ref rx);
        float footY=19.2f+sit*2.2f;
        float ll=WalkLift(step)*walkAmount,rl=WalkLift(step+.5)*walkAmount;
        danceScreenX=windowLeftPx+(hitOrigin.X+cx)*scale;danceScreenY=monitorPx.Top+(hitOrigin.Y+cy)*scale;dancePositionKnown=true;
        var state=g.Save();g.TranslateTransform(cx,cy);g.ScaleTransform(size/48,size/48);
        Color body=PetBody(accent),glow=Theme.Mix(Color.FromArgb(129,244,220),accent,.35f);
        paint.FillRound(new RectangleF(-15,23,30,1.4f),.7f,Theme.WithAlpha(accent,.25f));
        DrawDanceLeg(new PointF(-3.7f,hy),new PointF(lx,footY-ll),-1,body,accent);
        DrawDanceLeg(new PointF(3.7f,hy),new PointF(rx,footY-rl),1,body,accent);
        var torso=g.Save();g.TranslateTransform(hx,hy);g.RotateTransform((float)Math.Sin(step*2*Math.PI)*walkAmount*3+sleep*7);
        float arm=(float)Math.Sin(step*2*Math.PI)*walkAmount*24;
        DrawDanceArm(new PointF(-7,-10),LerpD(135-arm,225,KnightSelected?knightStretch.Value:0), -15+sit*35,body,accent);
        DrawDanceArm(new PointF(7,-10),LerpD(LerpD(45+arm,-50,KnightSelected?knightStretch.Value:0),-65,wave),LerpD(15,-30+(float)Math.Sin(petClock*10)*25,wave),body,accent);
        if(KnightSelected)DrawKnightCloak(false);else {paint.FillRound(new RectangleF(-9,-13,18,15),6,body);PetClothes(accent);
        paint.FillRound(new RectangleF(-4.5f,-7,9,3.2f),1.6f,accent);}
        var head=g.Save();g.TranslateTransform(danceGazeX.Value*1.1f,-12.5f+sleep*1.5f);
        g.RotateTransform(danceGazeX.Value*4+sleep*11+(KnightSelected?knightTurn.Value*4:0));
        PetFace(body,glow,accent,false);
        g.Restore(head);g.Restore(torso);
        if(petMood==PetMood.Sleep){
            float yy=-19-(float)(petClock%2)*3;paint.Text("z",Theme.Semi(8),Theme.Sub,16,yy);
            paint.Text("z",Theme.Semi(10),Theme.Sub,21,yy-7);
        }else if(petMood==PetMood.Thinking)paint.Text("…",Theme.Semi(12),accent,17,-16);
        else if(petMood==PetMood.Listening)paint.Icon(Theme.IconMic,10,accent,21,-10);
        g.Restore(state);
    }
    void DrawPetIdle(float w,float h){
        float x=34+Ease.Clamp01(petX.Value)*Math.Max(0,w-68);
        DrawSmartPet(x,h/2+3,51,Theme.DefaultAccent,false);
        // Keep the label opposite the character so it never overlaps.
        float tx=x>w/2?18:w-18;
        paint.Text(PetStatus(),Theme.Semi(12),Theme.Sub,tx,h/2,Math.Max(75,w/2-18),x>w/2?StringAlignment.Near:StringAlignment.Far);
    }
    void DrawPetPage(float w,float h){
        paint.Text("Tu mascota",Theme.Semi(19),Theme.Text,20,60);
        paint.Text("Personaliza a MANAURE",Theme.Ui(12),Theme.Sub,20,83);
        paint.FillRound(new RectangleF(18,101,w-36,119),18,Theme.Fill);
        DrawSmartPet(w/2,KnightSelected?166:157,KnightSelected?82:98,Theme.DefaultAccent,true);
        paint.TextCenter(PetStatus(),Theme.Ui(11),Theme.Sub,w/2,205);
        PetPicker("character","Personaje",new[]{"Robot","Gato","Astronauta","Caballero"},238);
        PetPicker("outfit",KnightSelected?"Capa":"Ropa",new[]{"Original","Blanca","Azul","Violeta"},277);
        PetPicker("accessory","Accesorio",new[]{"Audífonos","Gafas","Gorra","Ninguno"},316);
        PetPicker("dance","Baile",new[]{"Auto","Lateral","Disco","Groove"},355);
        paint.Text("Duerme tras 3 minutos de inactividad.",Theme.Ui(11),Theme.Sub,20,394);
        PillButton(new RectangleF(18,413,w-36,34),"petDesktop",PetDetached?"Regresar a la isla":"Mascota de escritorio",Theme.Fill,Theme.Text,delegate{ToggleDesktopPet(!PetDetached);});
    }
    void PetPicker(string key,string title,string[] options,float y){
        paint.Text(title,Theme.Ui(12),Theme.Sub,20,y);
        Segmented("pet"+key,128,y,options,PetChoice(key,options.Length),true,
           i=>{SetSetting("pet."+key,i);Kick();});
    }
    void StopPetSpeech(){
        petPrompt=null;
        try{if(petSynth!=null)petSynth.SpeakAsyncCancelAll();}catch{}
        petSpeaking=false;visemeOpen=0;Kick();
    }
    void SpeakPet(string text){
        StopPetSpeech();
        StopWake();
        try{
            if(petSynth==null){
                petSynth=new SpeechSynthesizer();
                petSynth.SelectVoice("Microsoft Helena Desktop");petSynth.Rate=0;petSynth.Volume=85;
                petSynth.SetOutputToDefaultAudioDevice();
                petSynth.VisemeReached+=(s,e)=>Post(()=>{
                    if(e.Prompt!=petPrompt)return;
                    int v=e.Viseme;visemeOpen=v==0||v==21?.0f:v==1||v==2||v==3?1f:v==4||v==5?.62f:.32f;
                    visemeWidth=v==6||v==7||v==8?.58f:1f;Kick();
                });
                petSynth.SpeakCompleted+=(s,e)=>Post(()=>{if(e.Prompt==petPrompt){petPrompt=null;petSpeaking=false;visemeOpen=0;Kick();}});
            }
            string clean=Regex.Replace(text,@"[*#_\x60]","");clean=clean.Substring(0,Math.Min(clean.Length,2500));
            if(string.IsNullOrWhiteSpace(clean))return;
            petSpeaking=true;petPrompt=petSynth.SpeakAsync(clean);Kick();
        }catch(Exception ex){petSpeaking=false;Log.Write("voz mascota: "+ex.Message);}
    }
    void DisposePetSpeech(){StopPetSpeech();if(petSynth!=null){petSynth.Dispose();petSynth=null;}}
 }
}
