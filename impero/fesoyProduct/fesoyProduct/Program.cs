using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Timers;
using System.Drawing; // Подключаем пространство имен для Icon
using System.Runtime.InteropServices;


namespace fesoyProduct
{
    class Program
    { // Déclaration globale de closeList
        private static string[] closeList = { "Notepad", "Calculator" };  // Ouvrir une liste d'applications à fermer
        private static string[] processNames = { "ImperoClient", "Impero Client Service", "ImperoUtilities", "Impero Winlogon Application" };
        private static double cpuThreshold = 1.5;
        private static double memoryThreshold = 200.0;
        private static System.Timers.Timer checkTimer = new System.Timers.Timer(5000); // Таймер для проверки каждые 5 секунд

        // Подключаем API Windows
        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_MINIMIZE = 6;  // Код для сворачивания
        private const uint WM_CLOSE = 0x0010; // Сообщение для закрытия окна

        [STAThread]



        static void Main()
        { 
            // Подписываемся на событие таймера
            checkTimer.Elapsed += CheckProcessActivity;
            checkTimer.Start();

            // Запуск окна уведомлений
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

                            // Обработка окон //close windows, minimize windows
                            EnumWindows((hWnd, lParam) =>
                            {
                                if (IsWindowVisible(hWnd))
                                {
                                    GetWindowThreadProcessId(hWnd, out uint processId);
                                    Process proc = null;

                                    try { proc = Process.GetProcessById((int)processId); }
                                    catch { return true; }

                                    if (proc != null && Array.Exists(closeList, name => proc.ProcessName.Contains(name)))
                                    {
                                        Console.WriteLine($"Закрываю: {proc.ProcessName}");
                                        PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Сворачиваю: {proc?.ProcessName}");
                                        ShowWindow(hWnd, SW_MINIMIZE);
                                    }
                                }
                                return true;
                            }, IntPtr.Zero);
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
                System.Threading.Thread.Sleep(2000); // Ожидаем немного, чтобы получить корректное значение
                return pc.NextValue() / Environment.ProcessorCount;
            }
        }

        private static void ShowNotification(string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,  // Используем стандартную иконку
                Visible = true,
                BalloonTipTitle = "Impero Мониторинг",
                BalloonTipText = message
            };

            notifyIcon.ShowBalloonTip(3000); // Показываем уведомление на 3 секунды
        }
    }
}
