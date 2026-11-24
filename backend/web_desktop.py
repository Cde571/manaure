import ctypes, os, subprocess, threading, base64, webbrowser, urllib.parse
from pathlib import Path
ROOT=Path(__file__).resolve().parent
LOCK=threading.Lock()
voice_process=None
def desktop(data):
    global voice_process
    action=data.get('action')
    if action=='system':
        r={}
        try:
            p=subprocess.run(['nvidia-smi','--query-gpu=utilization.gpu,temperature.gpu,memory.used,memory.total','--format=csv,noheader,nounits'],capture_output=True,text=True,timeout=4,creationflags=0x08000000)
            if p.returncode==0:
                g,t,m,total=[float(x.strip()) for x in p.stdout.splitlines()[0].split(',')]
                r.update(gpu=g,temp=t,vram=f'{m:.0f}/{total:.0f} MB')
        except Exception:pass
        return r
    if action=='spotify':
        webbrowser.open('https://open.spotify.com/');return {'message':'Spotify abierto.'}
    if action=='media':
        cmd=data.get('command');q=data.get('query','')
        if cmd in ('play','open'):
            if not isinstance(q,str) or len(q)>300:raise ValueError('Consulta no válida')
            webbrowser.open('https://www.youtube.com/results?'+urllib.parse.urlencode({'search_query':q}) if q else 'https://www.youtube.com/')
            return {'message':'Búsqueda abierta en YouTube. Elige una canción.'}
        key={'pause':0xB3,'resume':0xB3,'toggle':0xB3,'next':0xB0,'previous':0xB1}.get(cmd)
        if key is None:raise ValueError('Acción no disponible')
        ctypes.windll.user32.keybd_event(key,0,0,0);ctypes.windll.user32.keybd_event(key,0,2,0)
        return {'message':'Control multimedia enviado a Windows.'}
    if action=='volume':
        key={'up':0xAF,'down':0xAE,'mute':0xAD}.get(data.get('command'))
        if key is None:raise ValueError('Volumen no válido')
        ctypes.windll.user32.keybd_event(key,0,0,0);ctypes.windll.user32.keybd_event(key,0,2,0);return {'ok':True}
    if action=='voice-stop':
        with LOCK:
            if voice_process and voice_process.poll() is None:voice_process.terminate()
        return {'ok':True}
    if action=='listen':
        with LOCK:
            if voice_process and voice_process.poll() is None:raise RuntimeError('Micrófono ocupado')
            p=subprocess.Popen(['powershell.exe','-NoProfile','-NonInteractive','-ExecutionPolicy','RemoteSigned','-File',str(ROOT/'voice.ps1'),'listen'],stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=0x08000000)
            voice_process=p
        try:
            out,err=p.communicate(timeout=25)
            if p.returncode:raise RuntimeError('Escucha detenida o micrófono no disponible')
            return {'text':base64.b64decode(out.strip()).decode('utf-8') if out.strip() else ''}
        finally:
            if p.poll() is None:p.kill();p.communicate()
            with LOCK:
                if voice_process is p:voice_process=None
    raise ValueError('Acción no permitida')
