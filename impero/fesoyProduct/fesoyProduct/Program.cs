using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Timers;
using System.Drawing; // Подключаем пространство имен для Icon


namespace fesoyProduct
{
    class Program
    {
        private static string[] processNames = { "ImperoClient", "Impero Client Service", "ImperoUtilities", "Impero Winlogon Application" };
        private static double cpuThreshold = 1.5;
        private static double memoryThreshold = 200.0;
        private static System.Timers.Timer checkTimer = new System.Timers.Timer(5000);

        [STAThread]
        static void Main()
        {
            checkTimer.Elapsed += CheckProcessActivity;
            checkTimer.Start();
            Application.Run();
        }

        private static void CheckProcessActivity(object sender, ElapsedEventArgs e)
        {
            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                        double cpuUsage = GetCpuUsage(process);
                        double memoryUsage = process.WorkingSet64 / 1024.0 / 1024.0;

                        if (cpuUsage > cpuThreshold || memoryUsage > memoryThreshold)
                        {
                            ShowNotification($"Процесс {process.ProcessName} активен!\nCPU: {cpuUsage:F1}% | RAM: {memoryUsage:F1} MB");
                        }
                    }
                }
            }
        }

        private static double GetCpuUsage(Process process)
        {
            using (PerformanceCounter pc = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true))
            {
                pc.NextValue();
                System.Threading.Thread.Sleep(2000);
                return pc.NextValue() / Environment.ProcessorCount;
            }
        }

        private static void ShowNotification(string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,  // Исправлено: SystemIcons с заглавной буквы
                Visible = true,
                BalloonTipTitle = "Impero Мониторинг",
                BalloonTipText = message
            };

            notifyIcon.ShowBalloonTip(3000);
        }
    }
}
