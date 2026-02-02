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
using System.Threading.Tasks;
using MermaidCli.Browser.Cdp.Messaging;

namespace MermaidCli.Browser.Cdp;

/// <inheritdoc />
public class CdpBrowser : Browser
{
    private const int CloseTimeout = 5000;

    internal CdpBrowser(
        Connection connection,
        LauncherBase launcher)
    {
        Launcher = launcher;
        Connection = connection;
    }

    /// <inheritdoc/>
    public override async Task<Page> NewPageAsync()
    {
        var targetId = (await Connection.SendAsync<TargetCreateTargetResponse>("Target.createTarget", new TargetCreateTargetRequest
        {
            Url = "about:blank"
        }).ConfigureAwait(false)).TargetId;

        var sessionId = (await Connection.SendAsync<TargetAttachToTargetResponse>("Target.attachToTarget", new TargetAttachToTargetRequest
        {
            TargetId = targetId,
            Flatten = true
        }).ConfigureAwait(false)).SessionId;

        var session = Connection.GetSession(sessionId);
        return new CdpPage(session, Connection);
    }

    /// <inheritdoc/>
    public override async Task CloseAsync()
    {
        try
        {
            if (Connection != null && !Connection.IsClosed)
            {
                await Connection.SendAsync("Browser.close").ConfigureAwait(false);
            }

            if (Launcher != null)
            {
                await Launcher.EnsureExitAsync(TimeSpan.FromMilliseconds(CloseTimeout)).ConfigureAwait(false);
            }
        }
        catch
        {
            if (Launcher != null)
            {
                await Launcher.KillAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            Connection?.Dispose();
        }
    }

    internal static CdpBrowser Create(
        Connection connection,
        LauncherBase launcher)
    {
        return new CdpBrowser(connection, launcher);
    }
}
