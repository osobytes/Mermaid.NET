/*
 * MIT License
 *
 * Copyright (c) 2017 Darío Kondratiuk
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MermaidCli.Browser.Helpers;

namespace MermaidCli.Browser
{
    /// <summary>
    /// Represents a Base process and any associated temporary user data directory that have created
    /// by Puppeteer and therefore must be cleaned up when no longer needed.
    /// </summary>
    public abstract class LauncherBase : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LauncherBase"/> class.
        /// </summary>
        /// <param name="executable">Full path of executable.</param>
        /// <param name="options">Options for launching Base.</param>
        protected LauncherBase(string executable, LaunchOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            Process = new Process
            {
                EnableRaisingEvents = true,
            };
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.FileName = executable;
            Process.StartInfo.RedirectStandardError = true;

            SetEnvVariables(Process.StartInfo.Environment, options.Env, Environment.GetEnvironmentVariables());

            if (options.DumpIO)
            {
                Process.ErrorDataReceived += (_, e) => Console.Error.WriteLine(e.Data);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LauncherBase"/> class.
        /// </summary>
        ~LauncherBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets Base process details.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        /// Gets Base endpoint.
        /// </summary>
        public string EndPoint { get; protected set; } // Simplified: set directly for now or via helper

        internal LaunchOptions Options { get; }

        internal TempDirectory TempUserDataDir { get; init; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously starts Base process.
        /// </summary>
        /// <returns>Task which resolves when after start process begins.</returns>
        public virtual Task StartAsync() 
        {
            Process.Start();
            Process.BeginErrorReadLine();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously waits for graceful Base process exit within a given timeout period.
        /// Kills the Base process if it has not exited within this period.
        /// </summary>
        /// <param name="timeout">The maximum waiting time for a graceful process exit.</param>
        /// <returns>Task which resolves when the process is exited or killed.</returns>
        public async Task EnsureExitAsync(TimeSpan? timeout)
        {
            if (Process == null || Process.HasExited)
            {
                return;
            }

            if (timeout.HasValue)
            {
                // Simple wait loop
                var sw = Stopwatch.StartNew();
                while (!Process.HasExited && sw.Elapsed < timeout)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }

            if (!Process.HasExited)
            {
                await KillAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously kills Base process.
        /// </summary>
        /// <returns>Task which resolves when the process is killed.</returns>
        public Task KillAsync() 
        {
            if (Process != null && !Process.HasExited)
            {
                Process.Kill();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes Base process and any temporary user directory.
        /// </summary>
        /// <param name="disposing">Indicates whether disposal was initiated by <see cref="Dispose()"/> operation.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                KillAsync().GetAwaiter().GetResult();
                Process?.Dispose();
                TempUserDataDir?.Dispose();
            }
        }

        /// <summary>
        /// Set Env Variables.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="customEnv">The customEnv.</param>
        /// <param name="realEnv">The realEnv.</param>
        protected static void SetEnvVariables(IDictionary<string, string> environment, IDictionary<string, string> customEnv, IDictionary realEnv)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (realEnv == null)
            {
                throw new ArgumentNullException(nameof(realEnv));
            }

            foreach (DictionaryEntry item in realEnv)
            {
                environment[item.Key.ToString()] = item.Value.ToString();
            }

            if (customEnv != null)
            {
                foreach (var item in customEnv)
                {
                    environment[item.Key] = item.Value;
                }
            }
        }
    }
}
