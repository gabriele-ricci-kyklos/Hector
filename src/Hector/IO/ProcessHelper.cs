using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.IO
{
    public class ProcessHelper
    {
        private static readonly string[] _newLines = ["\n", "\r\n"];

        public static async Task<(string Output, string Error)> RunAsync(string application, string? commandLine = null, string? workingDirectory = null, string[]? stdInCmdList = null, int? timeoutInMs = null)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = application,               // The executable to run
                Arguments = commandLine,               // Arguments to pass (in this case, run 'dir' command)
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,            // Required for output redirection
                CreateNoWindow = true,               // Don't create a visible window
                WorkingDirectory = workingDirectory ?? string.Empty,
            };

            return await RunGenericProcessAsync(startInfo, stdInCmdList, timeoutInMs).ConfigureAwait(false);
        }

        public static async Task<(string Output, string Error)> RunUserInteractiveProcessAsync(string application, string commandLine, string workingDirectory, string[]? stdInCmdList = null, int? timeoutInMs = null)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = application,
                Arguments = commandLine,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            };

            return await RunGenericProcessAsync(startInfo, stdInCmdList, timeoutInMs).ConfigureAwait(false);
        }

        private static async Task<(string Output, string Error)> RunGenericProcessAsync(ProcessStartInfo startInfo, string[]? stdInCmdList = null, int? timeoutInMs = null)
        {
            bool hasStdInCommands = stdInCmdList is not null && stdInCmdList.Length > 0;
            startInfo.RedirectStandardInput = hasStdInCommands;

            StringBuilder outputBuilder = new();
            StringBuilder errorBuilder = new();

            // Create and start the process
            using Process process = new();
            process.StartInfo = startInfo;

            if (startInfo.RedirectStandardOutput)
            {
                process.OutputDataReceived += process_OutputDataReceived;
            }

            if (startInfo.RedirectStandardError)
            {
                process.ErrorDataReceived += process_ErrorDataReceived;
            }

            void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                outputBuilder.AppendLine(e.Data);
            }

            void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                errorBuilder.AppendLine(e.Data);
            }

            process.Start();

            if (hasStdInCommands)
            {
                if (!process.StandardInput.BaseStream.CanWrite)
                {
                    throw new NotSupportedException("The standard input stream can't be written");
                }

                bool hasExitCmd = false;

                foreach (string cmd in stdInCmdList!)
                {
                    await process.StandardInput.WriteLineAsync(cmd).ConfigureAwait(false);
                    hasExitCmd = cmd.ContainsIgnoreCase("exit");
                }

                if (!hasExitCmd)
                {
                    await process.StandardInput.WriteLineAsync("exit").ConfigureAwait(false);
                }
            }

            if (startInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }

            if (startInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            using CancellationTokenSource cts = new(timeoutInMs ?? 300000);
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            string errorMsg = errorBuilder.ToString();
            if (!string.IsNullOrEmpty(errorMsg)
                && _newLines.Contains(errorMsg.Substring(errorMsg.Length - 2, 2)))
            {
                errorMsg = errorMsg.Remove(errorMsg.Length - 2, 2);
            }

            return (outputBuilder.ToString(), errorMsg);
        }

        public static bool DetectRunningProcess(string processName) => Process.GetProcessesByName(processName).Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

        public static async ValueTask<bool> TryKillAllRunningProcessesByNameAsync(string processName, int? timeoutInMs = null)
        {
            bool success = true;
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process proc in processes)
            {
                try
                {
                    if (proc.HasExited)
                    {
                        continue;
                    }

                    using CancellationTokenSource cts = new(timeoutInMs ?? 5);
                    proc.Kill();
                    await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                catch
                {
                    success = false;
                }
            }

            return success;
        }
    }

    static class Ext
    {
#if NET5_0_OR_GREATER
#else

        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object> tcs = new();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null!);
            if (cancellationToken != default)
            {
                cancellationToken.Register(() => tcs.SetCanceled());
            }

            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }

#endif
    }
}
