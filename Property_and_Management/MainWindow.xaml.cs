using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Property_and_Management
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public new AppWindow AppWindow { get; }

        public MainWindow()
        {
            InitializeComponent();

            AppWindow = GetAppWindow();

            // Clicking X truly exits the process (previously this hid the window to the
            // tray, which left orphan processes alive that had to be killed manually).
            // Environment.Exit fires AppDomain.ProcessExit, which runs the cleanup handler
            // in App.xaml.cs that disposes the notification service and kills any child
            // processes spawned by two-window dev mode.
            AppWindow.Closing += (sender, args) =>
            {
                Environment.Exit(0);
            };
        }

        private AppWindow GetAppWindow()
        {

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
