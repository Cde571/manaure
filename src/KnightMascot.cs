using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Gili {
 partial class IslandForm {
    // Original little wanderer: ivory mask, curved horns, midnight cloak.
    bool KnightSelected {get{return PetChoice("character",4)==3;}}
    readonly Spring knightCloth=new Spring(0,48,12,.003f), knightTurn=new Spring(0,85,18,.003f),
       knightWalk=new Spring(0,90,19,.003f), knightStretch=new Spring(0,65,15,.003f);
    double knightStride,knightTime;
    void StepKnight(float dt){
        if(!KnightSelected)return;
        knightTime+=dt;
        float speed=petMood==PetMood.Walk?Math.Min(1,Math.Abs(petX.Velocity)*9):0;
        knightWalk.Target=speed;
        knightTurn.Target=speed>.03f?Math.Sign(petX.Velocity):danceGazeX.Value*.6f;
        float cloth=DancerWanted?-danceTorso.Value*.12f:petMood==PetMood.Walk?-knightTurn.Value*speed*4:0;
        knightCloth.Target=cloth;
        double cycle=petClock%22;
        knightStretch.Target=!DancerWanted&&petMood==PetMood.Sit&&cycle>9&&cycle<10.6?1:0;
        knightWalk.Step(dt);knightTurn.Step(dt);knightCloth.Step(dt);knightStretch.Step(dt);
        knightStride+=dt*(.75+knightWalk.Value*1.1);
    }
    Color KnightCloak(){
        switch(PetChoice("outfit",4)){
            case 1:return Color.FromArgb(123,130,147);
            case 2:return Color.FromArgb(46,91,139);
            case 3:return Color.FromArgb(95,62,131);
            default:return Color.FromArgb(54,67,89);
        }
    }
    void KnightLeg(PointF hip,PointF foot,int side,Color accent){
        PointF knee=SolveKnee(hip,foot,8.8f,8.8f,side);
        Color edge=Color.FromArgb(117,130,153),dark=Color.FromArgb(21,26,38);
        g.DrawLines(paint.P(edge,4.7f),new[]{hip,knee,foot});
        g.DrawLines(paint.P(dark,3.3f),new[]{hip,knee,foot});
        paint.FillRound(new RectangleF(foot.X-3.5f,foot.Y-1.7f,7,3.9f),1.8f,edge);
        paint.FillRound(new RectangleF(foot.X-3.1f,foot.Y-1.3f,6.2f,3.1f),1.4f,dark);
    }
    void KnightArm(PointF shoulder,float angle,float elbow,Color accent){
        PointF joint=PointAt(shoulder,6.3f,angle),hand=PointAt(joint,6.4f,angle+elbow);
        g.DrawLines(paint.P(Color.FromArgb(115,125,146),4),new[]{shoulder,joint,hand});
        g.DrawLines(paint.P(Color.FromArgb(24,30,43),2.8f),new[]{shoulder,joint,hand});
        paint.FillCircle(hand.X,hand.Y,1.8f,Color.FromArgb(219,222,220));
    }
    void DrawKnightCloak(bool dancing){
        float sit=dancing?0:Ease.Clamp01(petSit.Value),sleep=dancing?0:Ease.Clamp01(petSleep.Value);
        float sway=knightCloth.Value+(float)Math.Sin(knightTime*2.25)*(.35f+knightWalk.Value*.7f);
        float hem=6+sit*1.5f,spread=10+sit*3;
        Color main=KnightCloak(),edge=Color.FromArgb(134,148,171);
        using(var path=new GraphicsPath()){
            path.AddBezier(-7,-12,-10,-9,-9,-3,-spread+sway,hem);
            path.AddBezier(-spread+sway,hem,-6+sway,hem-2,-5+sway,hem+2,-1+sway,hem);
            path.AddBezier(-1+sway,hem,2+sway,hem+2,4+sway,hem-1,spread+sway*.7f,hem);
            path.AddBezier(spread+sway*.7f,hem,8,-1,11,-9,7,-12);
            path.CloseFigure();
            using(var b=new LinearGradientBrush(new RectangleF(-18,-16,36,30),Theme.WithAlpha(Theme.Mix(main,Color.White,.2f),paint.Alpha),Theme.WithAlpha(main,paint.Alpha),90))g.FillPath(b,path);
            g.DrawPath(paint.P(edge,.75f),path);
        }
        g.DrawBezier(paint.P(Theme.WithAlpha(Color.FromArgb(17,23,37),.55f),.9f),-4,-9,-5,-3,-5+sway,2,-6+sway,hem-1);
        g.DrawBezier(paint.P(Theme.WithAlpha(Color.FromArgb(155,171,194),.45f),.75f),4,-9,5,-2,5+sway,2,6+sway,hem-1);
        paint.FillRound(new RectangleF(-7,-13,14,4),2,Color.FromArgb(27,32,48));
        paint.FillCircle(0,-11,1.6f,Color.FromArgb(200,210,217));
    }
    void DrawKnightFace(Color accent,bool dancing){
        Color ivory=Color.FromArgb(242,241,226),shadow=Color.FromArgb(173,185,190),ink=Color.FromArgb(19,23,34);
        float turn=danceGazeX.Value*1.3f;
        using(var path=new GraphicsPath()){
            // Curved, tapered horns are continuous with the mask.
            path.StartFigure();
            path.AddBezier(-10,-14,-16,-18,-18,-27,-13,-29);
            path.AddBezier(-13,-29,-14,-22,-8,-20,-5,-17);
            path.AddBezier(-5,-17,-1,-18,1,-18,5,-17);
            path.AddBezier(5,-17,8,-20,14,-22,13,-29);
            path.AddBezier(13,-29,18,-27,16,-18,10,-14);
            path.AddBezier(10,-14,15,-9,14,-1,9,2);
            path.AddBezier(9,2,4,5,-4,5,-9,2);
            path.AddBezier(-9,2,-14,-1,-15,-9,-10,-14);
            path.CloseFigure();
            using(var b=new LinearGradientBrush(new RectangleF(-19,-30,38,36),Theme.WithAlpha(ivory,paint.Alpha),Theme.WithAlpha(shadow,paint.Alpha),105))g.FillPath(b,path);
            g.DrawPath(paint.P(ink,1.25f),path);
        }
        float open=Math.Max(.06f,Math.Min(1,danceBlink.Value*(1-Ease.Clamp01(petSleep.Value))));
        if(dancing)open=Math.Max(.1f,danceBlink.Value);
        for(int side=-1;side<=1;side+=2){
            float x=side*5.5f+turn, y=-6.1f+danceGazeY.Value*.65f;
            float eh=7.8f*open,ew=5.9f;
            if(petExpression.Value<-.4f)y+=side*petExpression.Value*.35f;
            g.FillEllipse(paint.B(ink),x-ew/2,y-eh/2,ew,eh);
            if(open>.6f)paint.FillCircle(x-1.1f+danceGazeX.Value*.7f,y-1.6f+danceGazeY.Value*.7f,.65f,Color.FromArgb(80,91,110));
        }
        if(petMouth.Value>.07f){
            float mw=2.5f*visemeWidth,mh=.4f+petMouth.Value*1.7f;
            g.FillEllipse(paint.B(ink),-mw/2,.5f-mh/2,mw,mh);
        }
        int accessory=PetChoice("accessory",4);
        if(accessory==0&&dancing){
            g.DrawArc(paint.P(Color.FromArgb(38,47,66),2.4f),-13,-17,26,20,180,180);
            paint.FillRound(new RectangleF(-15,-12,4,9),2,accent);
            paint.FillRound(new RectangleF(11,-12,4,9),2,accent);
        }else if(accessory==1){
            g.DrawEllipse(paint.P(ink,1.1f),-10,-11,9,8);g.DrawEllipse(paint.P(ink,1.1f),1,-11,9,8);
            g.DrawLine(paint.P(ink,1),-1,-7,1,-7);
        }else if(accessory==2){
            paint.FillRound(new RectangleF(-7,-21,14,5),2,accent);
            paint.FillRound(new RectangleF(0,-17,10,2),1,accent);
        }
    }
 }
}
