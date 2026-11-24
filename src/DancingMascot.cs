using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Gili {
 partial class IslandForm {
    // Layered 2D rig: choreography -> foot contacts and IK -> torso/arm follow-through -> gaze.
    readonly Spring danceGazeX=new Spring(0,145,22,.005f), danceGazeY=new Spring(0,145,22,.005f),
      danceBlink=new Spring(1,440,31,.005f), danceMix=new Spring(0,35,12,.005f),
      danceTorso=new Spring(0,110,19,.005f), danceHead=new Spring(0,85,16,.005f),
      danceShoulders=new Spring(0,75,15,.005f), danceCompression=new Spring(0,165,24,.005f);
    float danceScreenX,danceScreenY,bassFast,bassSlow,bassPrev,energy;
    double danceClock,beatAge=2,beatPeriod=.5,phaseBeat,phraseMix;
    int phrase=0,oldStyle=0,newStyle=0;
    bool dancePositionKnown;
    DateTime danceNextBlink=DateTime.MinValue,danceBlinkEnd;
    bool DancerWanted {get{return MascotEnabled && !QuietMode && mediaInfo.Present && mediaInfo.Playing;}}
    struct DancePose {
        public float L,R,Hip,Drop,Lean,LA,RA,LE,RE;
        public DancePose(float l,float r,float hip,float drop,float lean,float la,float ra,float le,float re){
            L=l;R=r;Hip=hip;Drop=drop;Lean=lean;LA=la;RA=ra;LE=le;RE=re;
        }
    }
    // Every segment moves at most one foot. Last pose matches the first.
    static readonly DancePose[][] dances={
        new[]{
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-12,7,-3,1.8f,-9,195,65,38,-45),
            new DancePose(-12,-1,-5,0,-5,220,20,30,-65),
            new DancePose(-7,-1,-3,1.6f,6,155,-15,20,-40),
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-7,12,3,1.8f,9,115,-15,45,-38),
            new DancePose(1,12,5,0,5,160,-40,65,-30),
            new DancePose(1,7,3,1.6f,-6,195,25,40,-20),
            new DancePose(-7,7,0,0,0,140,40,-20,20)},
        new[]{
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-11,7,-2,2,-8,120,-40,-55,-20),
            new DancePose(-11,1,-4,.4f,-6,155,-85,-25,35),
            new DancePose(-7,1,-2,2,5,215,-10,-35,-45),
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-7,11,2,2,8,220,60,20,55),
            new DancePose(-1,11,4,.4f,6,265,25,-35,25),
            new DancePose(-1,7,2,2,-5,190,-35,45,35),
            new DancePose(-7,7,0,0,0,140,40,-20,20)},
        new[]{
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-10,7,-2,2.2f,-7,185,25,-40,60),
            new DancePose(-10,2,-3,1,-3,120,75,-65,-10),
            new DancePose(-7,2,-1,2.2f,4,195,15,-55,70),
            new DancePose(-7,7,0,0,0,140,40,-20,20),
            new DancePose(-7,10,2,2.2f,7,155,-5,-60,40),
            new DancePose(-2,10,3,1,3,105,60,10,65),
            new DancePose(-2,7,1,2.2f,-4,165,-15,-70,55),
            new DancePose(-7,7,0,0,0,140,40,-20,20)}
    };
    static float LerpD(float a,float b,float t){return a+(b-a)*t;}
    static float SmoothD(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    static DancePose BlendD(DancePose a,DancePose b,float t){
        return new DancePose(LerpD(a.L,b.L,t),LerpD(a.R,b.R,t),LerpD(a.Hip,b.Hip,t),LerpD(a.Drop,b.Drop,t),
          LerpD(a.Lean,b.Lean,t),LerpD(a.LA,b.LA,t),LerpD(a.RA,b.RA,t),LerpD(a.LE,b.LE,t),LerpD(a.RE,b.RE,t));
    }
    static DancePose PoseAt(int style,double phase){
        double p=((phase%8)+8)%8;int i=(int)p;
        return BlendD(dances[style][i],dances[style][i+1],SmoothD((float)(p-i)));
    }
    static PointF PointAt(PointF a,float length,float degrees){
        double r=degrees*Math.PI/180;
        return new PointF(a.X+length*(float)Math.Cos(r),a.Y+length*(float)Math.Sin(r));
    }
    static void SeparateFeet(ref float l,ref float r){if(r-l<7.4f){float c=(l+r)*.5f;l=c-3.7f;r=c+3.7f;}}
    static PointF SolveKnee(PointF hip,PointF foot,float upper,float lower,int bend){
        float dx=foot.X-hip.X,dy=foot.Y-hip.Y;
        float raw=(float)Math.Sqrt(dx*dx+dy*dy);
        float d=Math.Max(.01f,Math.Min(upper+lower-.01f,raw));
        float ux=raw>.01f?dx/raw:0,uy=raw>.01f?dy/raw:1;
        float a=(upper*upper-lower*lower+d*d)/(2*d);
        float h=(float)Math.Sqrt(Math.Max(0,upper*upper-a*a));
        return new PointF(hip.X+ux*a+bend*uy*h,hip.Y+uy*a-bend*ux*h);
    }
    static float FootLift(int style,double phase,bool left){
        double p=((phase%8)+8)%8;int i=(int)p;
        DancePose a=dances[style][i],b=dances[style][i+1];
        float delta=left?Math.Abs(b.L-a.L):Math.Abs(b.R-a.R);
        if(delta<.01f)return 0;
        // contact at both ends; arc with a soft lift and soft landing
        return Math.Min(4.2f,1.8f+.38f*delta)*(float)Math.Pow(Math.Sin(Math.PI*(p-i)),1.5);
    }
    void StepDancer(float dt){
        danceClock+=dt;beatAge+=dt;
        bool preview=DancerWanted&&mediaInfo.TrackKey.StartsWith("demo");
        bool real=DancerWanted&&spectrum.Active&&!spectrum.Failed&&!preview;
        float bass=0,mid=0;
        if(real){for(int i=0;i<5;i++)bass+=bands[i]/5;for(int i=5;i<14;i++)mid+=bands[i]/9;}
        else if(preview){bass=.24f+.48f*(float)Math.Pow(Math.Max(0,Math.Cos(danceClock*4*Math.PI)),8);mid=.45f;}
        bassFast+=(bass-bassFast)*(1-(float)Math.Exp(-dt*22));
        bassSlow+=(bass-bassSlow)*(1-(float)Math.Exp(-dt*2));
        float flux=bassFast-bassPrev;bassPrev=bassFast;
        if((real||preview)&&bassFast>Math.Max(.075f,bassSlow*1.13f+.018f)&&flux>.006f&&beatAge>.27){
            if(beatAge<1.05){double observed=beatAge;if(observed>.75&&beatPeriod<.47)observed*=.5;beatPeriod=Math.Max(.32,Math.Min(.82,beatPeriod*.82+observed*.18));}
            // Phase correction is gradual, not a snap between poses.
            double error=phaseBeat-Math.Round(phaseBeat);phaseBeat-=error*.09;beatAge=0;
        }
        energy+=(Math.Min(1,bass*.65f+mid*.7f)-energy)*(1-(float)Math.Exp(-dt*5));
        phaseBeat+=dt/beatPeriod;
        if(phaseBeat>=2048){phaseBeat-=2048;phrase-=256;}
        int next=(int)Math.Floor(phaseBeat/8);
        if(next!=phrase){phrase=next;oldStyle=newStyle;newStyle=PetDanceStyle==0?Math.Abs(phrase)%dances.Length:PetDanceStyle-1;phraseMix=0;}
        phraseMix=Math.Min(1,phraseMix+dt/(beatPeriod*2));
        danceMix.Target=(real||preview)&&DancerWanted?Ease.Clamp01(energy*2.7f):0;
        double phase=phaseBeat*2;float mix=SmoothD((float)phraseMix);
        var pose=BlendD(PoseAt(oldStyle,phase),PoseAt(newStyle,phase),mix);
        danceTorso.Target=pose.Lean*danceMix.Value;
        danceHead.Target=-pose.Lean*.55f*danceMix.Value;
        danceShoulders.Target=-pose.Lean*.3f*danceMix.Value;
        danceCompression.Target=pose.Drop*danceMix.Value;
        Native.POINT p;
        if(dancePositionKnown&&Native.GetCursorPos(out p)){
            double dx=p.X-danceScreenX,dy=p.Y-danceScreenY,len=Math.Sqrt(dx*dx+dy*dy+1600*scale*scale);
            danceGazeX.Target=(float)(dx/len);danceGazeY.Target=(float)(dy/len);
        }
        var now=DateTime.UtcNow;
        if(now>=danceNextBlink){danceBlinkEnd=now.AddMilliseconds(125);danceNextBlink=now.AddMilliseconds(rng.Next(2300,5700));}
        danceBlink.Target=now<danceBlinkEnd?.08f:1;
        danceMix.Step(dt);danceTorso.Step(dt);danceHead.Step(dt);danceShoulders.Step(dt);danceCompression.Step(dt);
        danceGazeX.Step(dt);danceGazeY.Step(dt);danceBlink.Step(dt);
    }
    void DrawDanceLeg(PointF hip,PointF foot,int side,Color body,Color accent){
        if(KnightSelected){KnightLeg(hip,foot,side,accent);return;}
        PointF knee=SolveKnee(hip,foot,8.8f,8.8f,side);
        g.DrawLine(paint.P(body,3.6f),hip,knee);
        g.DrawLine(paint.P(body,3.4f),knee,foot);
        paint.FillCircle(knee.X,knee.Y,1.65f,body);
        paint.FillRound(new RectangleF(foot.X-3.1f,foot.Y-1,6.2f,3.1f),1.5f,body);
        paint.FillRound(new RectangleF(foot.X-2.5f,foot.Y+1,5,1.1f),.5f,accent);
    }
    void DrawDanceArm(PointF shoulder,float angle,float elbow,Color body,Color accent){
        if(KnightSelected){KnightArm(shoulder,angle,elbow,accent);return;}
        PointF e=PointAt(shoulder,6.3f,angle),hand=PointAt(e,6.4f,angle+elbow);
        g.DrawLine(paint.P(body,3.4f),shoulder,e);
        g.DrawLine(paint.P(body,3.2f),e,hand);
        paint.FillCircle(e.X,e.Y,1.5f,body);
        paint.FillCircle(hand.X,hand.Y,2.1f,body);
    }
    void DrawDancer(float cx,float cy,float size,Color accent){
        if(!DancerWanted||(PetDetached&&!detachedDrawing))return;
        danceScreenX=windowLeftPx+(hitOrigin.X+cx)*scale;danceScreenY=monitorPx.Top+(hitOrigin.Y+cy)*scale;dancePositionKnown=true;
        float a=Ease.Clamp01(danceMix.Value),mix=SmoothD((float)phraseMix);double phase=phaseBeat*2;
        DancePose p=BlendD(PoseAt(oldStyle,phase),PoseAt(newStyle,phase),mix);
        DancePose arms=BlendD(PoseAt(oldStyle,phase-.30),PoseAt(newStyle,phase-.30),mix);
        float lx=LerpD(-7,p.L,a),rx=LerpD(7,p.R,a); SeparateFeet(ref lx,ref rx);
        float ll=LerpD(FootLift(oldStyle,phase,true),FootLift(newStyle,phase,true),mix)*a;
        float rl=LerpD(FootLift(oldStyle,phase,false),FootLift(newStyle,phase,false),mix)*a;
        float hx=p.Hip*a,hy=7+Math.Max(0,danceCompression.Value)+(float)Math.Sin(danceClock*2.1)*.18f*(1-a);
        var state=g.Save();g.TranslateTransform(cx,cy);g.ScaleTransform(size/48f,size/48f);
        Color body=PetBody(accent),glow=Theme.Mix(Color.FromArgb(129,244,220),accent,.35f);
        // Feet are world-space within the mascot, not attached to the swaying torso.
        paint.FillRound(new RectangleF(-15,21,30,1.4f),.7f,Theme.WithAlpha(accent,.24f));
        DrawDanceLeg(new PointF(hx-3.7f,hy),new PointF(lx,19.2f-ll),-1,body,accent);
        DrawDanceLeg(new PointF(hx+3.7f,hy),new PointF(rx,19.2f-rl),1,body,accent);
        var torso=g.Save();g.TranslateTransform(hx,hy);g.RotateTransform(danceTorso.Value);
        var shoulder=g.Save();g.RotateTransform(danceShoulders.Value);
        DrawDanceArm(new PointF(-7,-10),LerpD(135,arms.LA,a),LerpD(-15,arms.LE,a),body,accent);
        DrawDanceArm(new PointF(7,-10),LerpD(45,arms.RA,a),LerpD(15,arms.RE,a),body,accent);
        g.Restore(shoulder);
        float squash=Math.Max(0,danceCompression.Value)*.025f;
        if(KnightSelected)DrawKnightCloak(true);else{paint.FillRound(new RectangleF(-9*(1+squash),-13+2*squash,18*(1+squash),15*(1-squash)),6,body);
        PetClothes(accent);
        paint.FillRound(new RectangleF(-4.5f,-7,9,3.2f),1.6f,accent);}
        var head=g.Save();g.TranslateTransform(danceGazeX.Value*1.1f,-12.5f);
        g.RotateTransform(danceHead.Value+danceGazeX.Value*3);
        PetFace(body,glow,accent,true);
        g.Restore(head);g.Restore(torso);g.Restore(state);
    }
    internal static void CheckDanceRig(){
        int checkedPoses=0;
        for(int s=0;s<dances.Length;s++){
            for(int i=0;i<8;i++){
                var p=dances[s][i];var q=dances[s][i+1];
                if(Math.Abs(p.L-q.L)>.001f&&Math.Abs(p.R-q.R)>.001f)throw new Exception("Both feet move in segment");
                for(int j=0;j<40;j++){
                    double phase=i+j/40.0;var pose=PoseAt(s,phase);
                    float l=FootLift(s,phase,true),r=FootLift(s,phase,false);
                    if(l>.001f&&r>.001f)throw new Exception("No supporting foot");
                    for(int side=-1;side<=1;side+=2){
                        PointF hip=new PointF(pose.Hip+side*3.7f,7+pose.Drop);
                        PointF foot=new PointF(side<0?pose.L:pose.R,19.2f-(side<0?l:r));
                        PointF knee=SolveKnee(hip,foot,8.8f,8.8f,side);
                        if((knee.X-hip.X)*side < -1.5f)throw new Exception("Inward knee");
                        if(float.IsNaN(knee.X)||float.IsNaN(knee.Y))throw new Exception("Invalid knee");
                        double first=Math.Sqrt(Math.Pow(knee.X-hip.X,2)+Math.Pow(knee.Y-hip.Y,2));
                        double second=Math.Sqrt(Math.Pow(knee.X-foot.X,2)+Math.Pow(knee.Y-foot.Y,2));
                        if(Math.Abs(first-8.8)>.03||Math.Abs(second-8.8)>.03)throw new Exception("Unreachable leg pose");
                    }
                    checkedPoses++;
                }
            }
        }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"dance-rig-test.txt"),"OK: "+checkedPoses+" poses; 3 choreography loops; supported feet; fixed leg lengths.");
    }
 }
}
