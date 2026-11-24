using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
namespace Gili {
 partial class IslandForm {
    class ChatRun {public string Text;public Font Font;public float X,Width;public bool Code;}
    class ChatLine {public readonly List<ChatRun> Runs=new List<ChatRun>();public float Y,H;public bool Code;}
    class ChatCard {public bool User;public float X,Y,W,H;public readonly List<ChatLine> Lines=new List<ChatLine>();}
    readonly List<ChatCard> chatCards=new List<ChatCard>();
    readonly Font chatMono=new Font("Consolas",12,FontStyle.Regular,GraphicsUnit.Pixel);
    readonly Font chatItalic=new Font("Segoe UI",13,FontStyle.Italic,GraphicsUnit.Pixel);
    int chatRevision,chatBuilt=-1;
    float chatLayoutWidth,chatContentHeight;
    bool chatAnchorReply;
    void AddChatRun(ChatCard card,ref ChatLine line,string text,Font font,bool code,float maxWidth,float indent,float lineHeight) {
        foreach(Match m in Regex.Matches(text,@"\S+|\s+")){
            string token=m.Value;
            if(token.Trim().Length==0&&line.Runs.Count==0&&!line.Code)continue;
            float x=line.Runs.Count==0?indent:line.Runs[line.Runs.Count-1].X+line.Runs[line.Runs.Count-1].Width;
            float tw=paint.Measure(token,font);
            if(x+tw>maxWidth&&line.Runs.Count>0){card.Lines.Add(line);line=new ChatLine{Y=line.Y+line.H,H=lineHeight,Code=code};x=indent;if(token.Trim().Length==0)continue;}
            while(token.Length>0){
                int n=token.Length;
                while(n>1&&paint.Measure(token.Substring(0,n),font)>maxWidth-x)n--;
                string part=token.Substring(0,n);float width=paint.Measure(part,font);
                line.Runs.Add(new ChatRun{Text=part,Font=font,X=x,Width=width,Code=code});token=token.Substring(n);
                if(token.Length>0){card.Lines.Add(line);line=new ChatLine{Y=line.Y+line.H,H=lineHeight,Code=code};x=indent;}
            }
        }
    }
    void LayoutChatParagraph(ChatCard card,string raw,ref float y,bool code,bool user){
        string t=raw.TrimEnd();if(t.Trim().Length==0){y+=8;return;}
        int heading=0;float indent=0;
        if(!user&&!code){
            var h=Regex.Match(t,@"^\s*(#{1,6})\s+(.+)$");if(h.Success){heading=h.Groups[1].Length;t=h.Groups[2].Value;y+=5;}
            var bullet=Regex.Match(t,@"^\s*[-+*]\s+(.+)$");if(bullet.Success){t="•  "+bullet.Groups[1].Value;indent=12;}
            else if(Regex.IsMatch(t,@"^\s*\d+[.)]\s+")){t=t.TrimStart();indent=12;}
            if(Regex.IsMatch(t,@"^\s*([-*_])\1\1+\s*$")){y+=8;return;}
        }
        float height=code?19:heading>0?26:20,max=card.W-28;
        var line=new ChatLine{Y=y,H=height,Code=code};
        Font normal=code?chatMono:heading>0?Theme.Semi(heading<=2?17:15):Theme.Ui(13.5f);
        if(code||user)AddChatRun(card,ref line,t,normal,code,max,indent,height);
        else {
            var pattern=@"(\*\*[^*\n]+\*\*|__[^_\n]+__|\x60[^\x60\n]+\x60|(?<!\*)\*[^*\n]+\*(?!\*)|(?<!_)_[^_\n]+_(?!_))";
            int pos=0;
            foreach(Match m in Regex.Matches(t,pattern)){
                if(m.Index>pos)AddChatRun(card,ref line,t.Substring(pos,m.Index-pos),normal,false,max,indent,height);
                string v=m.Value;bool bold=v.StartsWith("**")||v.StartsWith("__");bool inlineCode=v[0]==96;int cut=bold?2:1;
                AddChatRun(card,ref line,v.Substring(cut,v.Length-2*cut),bold?Theme.Semi(13.5f):inlineCode?chatMono:chatItalic,inlineCode,max,indent,height);
                pos=m.Index+m.Length;
            }
            if(pos<t.Length)AddChatRun(card,ref line,t.Substring(pos),normal,false,max,indent,height);
        }
        if(line.Runs.Count>0)card.Lines.Add(line);
        y=line.Y+line.H+(heading>0?5:2);
    }
    void BuildChatLayout(float width){
        if(chatBuilt==chatRevision&&Math.Abs(chatLayoutWidth-width)<.1f)return;
        chatBuilt=chatRevision;chatLayoutWidth=width;chatCards.Clear();float y=0;
        foreach(var msg in jarvisMessages){
            bool user=msg.Key!="MANAURE";float cw=user?width*.84f:width;
            var card=new ChatCard{User=user,X=user?width-cw:0,Y=y,W=cw};float cy=36;bool code=false;
            foreach(string raw in (msg.Value??"").Replace("\r","").Split('\n')){
                if(!user&&raw.TrimStart().StartsWith(new string((char)96,3))){
                    code=!code;cy+=6;
                    if(code&&raw.Trim().Length>3)LayoutChatParagraph(card,raw.Trim().Substring(3),ref cy,true,false);
                    continue;
                }
                LayoutChatParagraph(card,raw,ref cy,code,user);
            }
            card.H=Math.Max(52,cy+8);chatCards.Add(card);y+=card.H+10;
        }
        chatContentHeight=Math.Max(0,y-10);
    }
    void DrawChatMessages(float w,float top,float height){
        float x=28,width=w-62,viewport=height-16;
        BuildChatLayout(width);
        int maxScroll=(int)Math.Max(0,Math.Ceiling(chatContentHeight-viewport));
        jarvisTotalLines=maxScroll;
        if(chatAnchorReply){chatAnchorReply=false;if(chatCards.Count>0){var last=chatCards[chatCards.Count-1];jarvisScroll=last.H>viewport?(int)Math.Max(0,maxScroll-last.Y):0;}}
        jarvisScroll=Math.Max(0,Math.Min(maxScroll,jarvisScroll));
        float offset=maxScroll-jarvisScroll;
        var saved=g.Save();
        g.SetClip(new RectangleF(x,top+8,width+2,viewport),System.Drawing.Drawing2D.CombineMode.Intersect);
        foreach(var card in chatCards){
            float yy=top+8+card.Y-offset;
            if(yy+card.H<top||yy>top+height)continue;
            float xx=x+card.X;
            Color fill=card.User?Theme.FillHover:Theme.WithAlpha(Theme.FillHover,.65f);
            paint.FillRound(new RectangleF(xx,yy,card.W,card.H),15,fill);
            paint.Text(card.User?"TÚ":AssistantName,Theme.Semi(11),card.User?Theme.Sub:Theme.Green,xx+14,yy+18);
            foreach(var line in card.Lines){
                float ly=yy+line.Y;
                if(ly+line.H<top||ly>top+height)continue;
                if(line.Code)paint.FillRound(new RectangleF(xx+9,ly,card.W-18,line.H+2),4,Theme.Fill);
                foreach(var run in line.Runs){
                    if(run.Code&&!line.Code)paint.FillRound(new RectangleF(xx+13+run.X,ly+1,run.Width+2,line.H-2),4,Theme.Fill);
                    paint.Text(run.Text,run.Font,run.Code?Theme.Green:Theme.Text,xx+14+run.X,ly+line.H/2,run.Width+3,StringAlignment.Near);
                }
            }
        }
        g.Restore(saved);
        if(maxScroll>0){
            float th=Math.Max(18,viewport*viewport/chatContentHeight),py=top+8+(viewport-th)*offset/maxScroll;
            paint.FillRound(new RectangleF(w-28,top+8,3,viewport),1.5f,Theme.Fill);
            paint.FillRound(new RectangleF(w-28,py,3,th),1.5f,Theme.Sub);
        }
    }
 }
}
