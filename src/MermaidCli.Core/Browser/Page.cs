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
 
#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MermaidCli.Browser
{
    public abstract class Page : IDisposable
    {
        public event EventHandler Console;
        public event EventHandler Request;
        public event EventHandler RequestFailed;
        public event EventHandler PageError;

        protected void NotifyConsole() => Console?.Invoke(this, EventArgs.Empty);
        protected void NotifyPageError() => PageError?.Invoke(this, EventArgs.Empty);

        public abstract Task GoToAsync(string url, NavigationOptions options = null);
        public abstract Task SetContentAsync(string html, NavigationOptions options = null);
        public abstract Task<T> EvaluateFunctionAsync<T>(string script, params object[] args);
        public abstract Task EvaluateFunctionAsync(string script, params object[] args);
        public abstract Task<byte[]> PdfDataAsync(PdfOptions options = null);
        public abstract Task<byte[]> ScreenshotDataAsync(ScreenshotOptions options = null);
        public abstract Task SetViewportAsync(ViewPortOptions viewport);
        public abstract Task CloseAsync();
        public abstract Task<IElementHandle> AddScriptTagAsync(AddTagOptions options);
        public abstract Task<IElementHandle> AddStyleTagAsync(AddTagOptions options);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) => _ = CloseAsync();
    }

    // Dummy IElementHandle for compilation
    public interface IElementHandle : IDisposable {}
}