using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PhoneBox.Agent;

/// <summary>
/// The settings page in a native window (WebView2) instead of a browser tab. Closing the window ends the program,
/// so no port stays open. All calls must come from the tray's UI thread.
/// </summary>
internal sealed class AppWindow
{
    private readonly string _url;
    private readonly Uri _origin;
    private readonly Icon? _icon;
    private readonly Action _closed;
    private readonly Action<string> _log;
    private bool _quietClose;
    private Form? _form;
    private WebView2? _web;
    private int _retries;

    /// <summary>True when creating the web view failed: the caller opens the browser instead.</summary>
    public bool Failed { get; private set; }

    public AppWindow(string url, Icon? icon, Action closed, Action<string> log)
    {
        _log = log;
        _url = url;
        _origin = new Uri(url);
        _icon = icon;
        _closed = closed;
    }

    /// <summary>Whether the Evergreen WebView2 runtime is installed (it is part of Windows 11).</summary>
    public static bool RuntimeInstalled()
    {
        try { return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString()); }
        catch (Exception) { return false; }
    }

    public void Show()
    {
        if (_form is { IsDisposed: false })
        {
            if (_form.WindowState == FormWindowState.Minimized) _form.WindowState = FormWindowState.Normal;
            _form.Show();
            _form.Activate();
            _form.BringToFront();
            return;
        }

        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1366, 768);
        _form = new Form
        {
            Text = "PhoneBox",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(Math.Min(1280, area.Width - 80), Math.Min(820, area.Height - 80)),
            MinimumSize = new Size(640, 480),
            Icon = _icon,
        };
        _web = new WebView2 { Dock = DockStyle.Fill };
        _form.Controls.Add(_web);
        _form.FormClosed += (_, _) =>
        {
            _web?.Dispose();
            _web = null;
            _form = null;
            if (!_quietClose) _closed();
        };
        _form.Show();
        _form.Activate();
        _ = InitAsync(_web);
    }

    private async Task InitAsync(WebView2 web)
    {
        try
        {
            // an own folder: the default one sits next to the program, which may be read-only
            var dataDir = Path.Combine(Path.GetDirectoryName(AppPaths.LogPath) ?? Path.GetTempPath(), "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            await web.EnsureCoreWebView2Async(env);
            var core = web.CoreWebView2;
            core.Settings.IsStatusBarEnabled = false;
            core.NavigationStarting += (_, e) =>
            {
                if (IsInternal(e.Uri)) return;
                e.Cancel = true;
                OpenExternal(e.Uri);
            };
            core.NewWindowRequested += (_, e) => { e.Handled = true; OpenExternal(e.Uri); };
            core.NavigationCompleted += async (_, e) =>
            {
                // the page is served by this very program: it may not be listening yet when the window opens
                if (e.IsSuccess || _retries++ >= 30) return;
                await Task.Delay(1000);
                try { web.CoreWebView2?.Navigate(_url); } catch (ObjectDisposedException) { }
            };
            core.Navigate(_url);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // no runtime, or it could not start: fall back to the browser and keep running from the tray
            _log($"the settings window could not start ({ex.GetType().Name}: {ex.Message}); opening the browser instead");
            Failed = true;
            _quietClose = true; // closing this window must not quit the program
            var form = _form;
            form?.Close();
            Tray.OpenInBrowser(_url);
        }
    }

    private bool IsInternal(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) return false;
        if (u.Scheme is "about" or "blob" or "data") return true;
        return u.Scheme == _origin.Scheme && u.Port == _origin.Port && u.Host is "127.0.0.1" or "localhost";
    }

    private static void OpenExternal(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Scheme is "http" or "https")
            Tray.OpenInBrowser(u.AbsoluteUri);
    }
}
