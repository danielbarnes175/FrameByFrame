using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FrameByFrame.src.Engine.Export
{
    internal readonly record struct ProcessResult(int ExitCode, string StandardError);

    internal static class ProcessRunner
    {
        public static ProcessResult Run(ProcessStartInfo startInfo, TimeSpan timeout,
            CancellationToken cancellationToken, Func<Exception?>? validateWhileRunning = null)
        {
            ArgumentNullException.ThrowIfNull(startInfo);
            if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

            using Process process = new() { StartInfo = startInfo };
            if (!process.Start()) throw new InvalidOperationException($"{startInfo.FileName} could not be started.");
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                while (!process.WaitForExit(100))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Kill(process);
                        throw new OperationCanceledException("The operation was canceled.", cancellationToken);
                    }
                    Exception? violation = validateWhileRunning?.Invoke();
                    if (violation != null)
                    {
                        Kill(process);
                        throw violation;
                    }
                    if (elapsed.Elapsed >= timeout)
                    {
                        Kill(process);
                        throw new TimeoutException($"{startInfo.FileName} exceeded its {timeout.TotalSeconds:0}-second time limit.");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                Exception? finalViolation = validateWhileRunning?.Invoke();
                if (finalViolation != null) throw finalViolation;
                return new ProcessResult(process.ExitCode, standardError.GetAwaiter().GetResult());
            }
            finally
            {
                if (!process.HasExited) Kill(process);
                try { standardOutput.GetAwaiter().GetResult(); } catch { }
                try { standardError.GetAwaiter().GetResult(); } catch { }
            }
        }

        private static void Kill(Process process)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
            catch { }
        }
    }
}
