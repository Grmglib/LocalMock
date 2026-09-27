using System.Drawing;
using System.Windows.Forms;

namespace LocalMock;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly Control _dispatcher = new();

    public TrayApplicationContext(string managerUrl)
    {
        _ = _dispatcher.Handle;
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Open LocalMock", null, (_, _) => Program.OpenManager(managerUrl));
        _menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "LocalMock",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => Program.OpenManager(managerUrl);
    }

    public void RequestExit()
    {
        if (_dispatcher.IsHandleCreated)
        {
            _dispatcher.BeginInvoke(new Action(ExitThread));
        }
    }

    protected override void ExitThreadCore()
    {
        _icon.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Dispose();
            _menu.Dispose();
            _dispatcher.Dispose();
        }

        base.Dispose(disposing);
    }
}
