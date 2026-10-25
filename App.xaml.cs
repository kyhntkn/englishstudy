using System;
using System.Threading;
using System.Windows;

namespace EnglishStudy;

public partial class App : Application
{
    static Mutex _mutex;
    static EventWaitHandle _signal;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "EnglishStudy_SingleInstance_Mutex", out bool created);
        _signal = new EventWaitHandle(false, EventResetMode.AutoReset, "EnglishStudy_SingleInstance_Show");
        if (!created)
        {
            // Zaten çalışan kopya var: onu öne getir, bu kopyayı kapat
            _signal.Set();
            Shutdown();
            return;
        }
        base.OnStartup(e);
        var t = new Thread(() =>
        {
            while (true)
            {
                _signal.WaitOne();
                try { Dispatcher.Invoke(() => (this.MainWindow as EnglishStudy.MainWindow)?.ShowMain()); }
                catch { }
            }
        }) { IsBackground = true };
        t.Start();
    }
}
