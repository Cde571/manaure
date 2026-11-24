using System;
using System.Threading;
using System.Windows.Forms;

namespace Gili
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--check-dance") { IslandForm.CheckDanceRig(); return; }
            if (args.Length > 0 && args[0] == "--check-visemes") {
                int n=0; using(var sp=new System.Speech.Synthesis.SpeechSynthesizer()) {
                    sp.SelectVoice("Microsoft Helena Desktop");sp.SetOutputToNull();
                    sp.VisemeReached+=(sender,e)=>System.Threading.Interlocked.Increment(ref n);
                    sp.Speak("Hola, soy Manaure. Estoy listo para ayudarte.");
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"pet-voice-test.txt"),"Visemes: "+n);
                return;
            }
            bool created;
            using (var mutex = new Mutex(true, @"Local\ManaurePublicIsland", out created))
            {
                if (!created) return; // already running

                try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); } // per-monitor v2
                catch (EntryPointNotFoundException) { }

                Application.ThreadException += (s, e) => Log.Write("ui: " + e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Write("fatal: " + e.ExceptionObject);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-CO");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("es-CO");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new IslandForm());
            }
        }
    }
}
