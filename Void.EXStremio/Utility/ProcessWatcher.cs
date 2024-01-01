using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Void.EXStremio.Utility {
    internal class ProcessWatcher {
        readonly string exePath;

        public event Action ProcessStarted;
        public event Action ProcessExited;

        Task watcherTask = Task.CompletedTask;
        public bool IsProcessRunning { get; private set; }
        public bool IsWatching { get; private set; }

        public ProcessWatcher(string exePath) {
            this.exePath = exePath;
        }

        public void StartWatching() {
            IsWatching = true;
            Task.Run(() => {
                while (true) {
                    var wasRunning = IsProcessRunning;
                    var process = GetProcess();
                    IsProcessRunning = process != null;

                    if (!wasRunning && IsProcessRunning) {
                        ProcessStarted?.Invoke();
                        process.WaitForExit();

                        continue;
                    } else if (wasRunning && !IsProcessRunning) {
                        ProcessExited?.Invoke();
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        public void StopWatching() {
            IsWatching = false;
        }

        public void RunProcess() {
            var process = GetProcess();
            if (process != null) { return; }

            Process.Start(exePath);
        }

        public void StopProcess() {
            var process = GetProcess();
            process?.Kill(true);
        }

        Process GetProcess() {
            var processName = Path.GetFileNameWithoutExtension(exePath);
            return Process.GetProcessesByName(processName).FirstOrDefault();
        }
    }
}
