using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Mapper;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;
using Property_and_Management.src.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Property_and_Management
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        // Public application state
        public static Window MainWindow { get; set; }
        public Frame RootFrame { get; set; }
        public string AppUserModelId { get; }
        public int CurrentUserID { get; }
        public NotificationsViewModel NotificationsViewModel { get; private set; }


        // Tray icon
        private TaskbarIcon _trayIcon;


        // Private dependencies and state
        private Window? _mainWindow;
        private NotificationRepository _notificationRepository;
        private NotificationService _notification_service;
        private GameRepository _gameRepository;
        private GameService _gameService;
        private readonly NotificationManager _notificationManager;

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App()
        {
            CurrentUserID = GetUserIdFromArgs();

            // Two-window dev mode: user 1 spawns the notification server + a second client
            if (CurrentUserID == 1 && IsTwoWindowsEnabled())
            {
                StartNotificationServer();
                LaunchSecondClient();
            }

            AppUserModelId = $"BoardRent -- user-{CurrentUserID}";

            // Create manager and wire its generic handlers (handlers may reference fields initialized later)
            _notificationManager = new NotificationManager();

            SetupNotificationManager();

            EnsureSingleInstance(AppUserModelId);

            ConfigureServices();
            InitializeServices(CurrentUserID);

            InitializeComponent();
        }

        private void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IMapper<User, UserDTO>, UserMapper>();
            serviceCollection.AddSingleton<IMapper<Game, GameDTO>, GameMapper>();
            serviceCollection.AddSingleton<IMapper<Notification, NotificationDTO>, NotificationMapper>();
            serviceCollection.AddSingleton<IMapper<Rental, RentalDTO>, RentalMapper>();
            serviceCollection.AddSingleton<IMapper<Request, RequestDTO>, RequestMapper>();


            serviceCollection.AddSingleton<IUserRepository, UserRepository>();
            serviceCollection.AddSingleton<IGameRepository, GameRepository>();
            serviceCollection.AddSingleton<IRequestRepository, RequestRepository>();
            serviceCollection.AddSingleton<IRentalRepository, RentalRepository>();
            serviceCollection.AddSingleton<INotificationRepository, NotificationRepository>();
            serviceCollection.AddSingleton<NotificationRepository>(sp => (NotificationRepository)sp.GetRequiredService<INotificationRepository>());

            serviceCollection.AddSingleton<IGameService, GameService>();
            serviceCollection.AddSingleton<IRentalService, RentalService>();
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            serviceCollection.AddSingleton<NotificationService>(sp => (NotificationService)sp.GetRequiredService<INotificationService>());

            serviceCollection.AddSingleton<IRequestService, RequestService>();

            serviceCollection.AddSingleton<NotificationsViewModel>();
            serviceCollection.AddSingleton<MenuBarViewModel>();
            serviceCollection.AddTransient(sp => new ListingsViewModel(sp.GetRequiredService<IGameService>(), CurrentUserID));
            serviceCollection.AddTransient<CreateGameViewModel>();
            serviceCollection.AddTransient<EditGameViewModel>();
            serviceCollection.AddTransient<CreateRequestViewModel>();
            serviceCollection.AddTransient<CreateRentalViewModel>();
            serviceCollection.AddTransient<RequestsFromOthersViewModel>();
            serviceCollection.AddTransient<RequestsToOthersViewModel>();
            serviceCollection.AddTransient<RentalsFromOthersViewModel>();
            serviceCollection.AddTransient<RentalsToOthersViewModel>();

            Services = serviceCollection.BuildServiceProvider();
        }

        private int GetUserIdFromArgs()
        {
            int defaultUserId = 1;
            string[] commandLineArgs = Environment.GetCommandLineArgs(); // first arg is the executable path
            if (commandLineArgs.Length > 1 && int.TryParse(commandLineArgs[1], out int parsedUserId))
            {
                return parsedUserId;
            }

            return defaultUserId;
        }

        #region Two-window dev mode

        private static string? FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(System.IO.Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        private static bool IsTwoWindowsEnabled()
        {
            try
            {
                var repoRoot = FindRepoRoot();
                if (repoRoot == null) return false;

                var envPath = System.IO.Path.Combine(repoRoot, ".env");
                if (!File.Exists(envPath)) return false;

                foreach (var line in File.ReadAllLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith('#') || !trimmed.Contains('=')) continue;
                    var parts = trimmed.Split('=', 2);
                    if (parts[0].Trim() == "TWO_WINDOWS")
                        return parts[1].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }

        private static void StartNotificationServer()
        {
            try
            {
                if (Process.GetProcessesByName("NotificationServer").Length > 0) return;

                var repoRoot = FindRepoRoot();
                if (repoRoot == null) return;

                var serverBinDir = System.IO.Path.Combine(repoRoot, "NotificationServer", "bin");
                if (!Directory.Exists(serverBinDir)) return;

                var serverExe = Directory.GetFiles(serverBinDir, "NotificationServer.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (serverExe == null) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = serverExe,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                });
            }
            catch { }
        }

        private static void LaunchSecondClient()
        {
            try
            {
                var currentExe = Environment.ProcessPath;
                if (currentExe == null) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = currentExe,
                    Arguments = "2",
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(currentExe)
                });
            }
            catch { }
        }

        #endregion

        private void SetupNotificationManager()
        {
            // Ensure the manager is unregistered when the process exits
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                _notificationManager.Unregister();
                _notification_service?.Dispose();
            };

            // When a notification is clicked, bring the window to foreground and optionally navigate
            _notificationManager.NotificationClicked += (sender, eventArguments) =>
            {
                _mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    _mainWindow?.Activate();

                    if (eventArguments.Arguments.ContainsKey("navigate") &&
                        eventArguments.Arguments["navigate"] == nameof(NotificationsPage))
                    {
                        ActivateWindow();
                        NavigateToNotificationsWithinShell();
                    }
                });
            };

            _notificationManager.Init();
        }

        private void NavigateToNotificationsWithinShell()
        {
            if (RootFrame?.Content is MenuBarView currentShell)
            {
                currentShell.NavigateToNotifications();
                return;
            }

            void OnShellLoaded(object sender, NavigationEventArgs e)
            {
                if (e.Content is MenuBarView loadedShell)
                {
                    RootFrame.Navigated -= OnShellLoaded;
                    loadedShell.NavigateToNotifications();
                }
            }

            RootFrame.Navigated += OnShellLoaded;
            RootFrame.Navigate(typeof(MenuBarView), _gameService);
        }

        private void EnsureSingleInstance(string appUserModelId)
        {
            var appInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(appUserModelId);
            if (!appInstance.IsCurrent)
            {
                appInstance.RedirectActivationToAsync(Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs()).AsTask().Wait();
                Environment.Exit(0);
            }

            // Get the current instance and activate the window
            appInstance.Activated += (sender, args) =>
            {
                ActivateWindow();
            };
        }

        private void InitializeServices(int userId)
        {
            // Initialize navigation frame
            RootFrame = new Frame();

            // Resolve repository/service/viewmodel from DI container
            _notificationRepository = Services.GetRequiredService<NotificationRepository>();
            _notification_service = Services.GetRequiredService<NotificationService>();
            _gameRepository = (GameRepository)Services.GetRequiredService<IGameRepository>();
            _gameService = (GameService)Services.GetRequiredService<IGameService>();
            NotificationsViewModel = Services.GetRequiredService<NotificationsViewModel>();

            // Start listening and subscribe for the configured user
            _notification_service.StartListening();
            _notification_service.SubscribeToServer(userId);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            CreateAndShowMainWindow();


            // Wrap your Frame in a Grid so the tray icon can be added later
            Grid rootGrid = new Grid();
            rootGrid.Children.Add(RootFrame);  // Your navigation frame
            MainWindow.Content = rootGrid;            // Set Grid as window content

            RootFrame.Navigate(typeof(MenuBarView), _gameService);

            CreateTrayIcon();

            // debug:
        }

        private void CreateAndShowMainWindow()
        {
            MainWindow = _mainWindow = new MainWindow();
            _mainWindow.Content = RootFrame;
            _mainWindow.Activate();

            // Display the AppUserModelId in the window title for debugging / identification
            _mainWindow.Title = AppUserModelId;
        }

        private void ActivateWindow()
        {
            _mainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (_mainWindow is MainWindow win)
                {
                    win.AppWindow.Show();
                }
                _mainWindow.Activate();
            });
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = $"{AppUserModelId}",
                IconSource = new BitmapImage(new Uri(Constants.APP_TRAY_ICON_URI)),
            };

            // 1. Create a Command for Open
            var openCommand = new XamlUICommand();
            openCommand.ExecuteRequested += (s, e) =>
            {
                ActivateWindow();
            };

            var openItem = new MenuFlyoutItem
            {
                Text = "Open",
                Command = openCommand // Bind to Command instead of Click
            };

            // 2. Create a Command for Exit
            var exitCommand = new XamlUICommand();
            exitCommand.ExecuteRequested += (s, e) =>
            {
                _trayIcon.Dispose();
                Environment.Exit(0);
            };

            var exitItem = new MenuFlyoutItem
            {
                Text = "Exit",
                Command = exitCommand // Bind to Command instead of Click
            };

            _trayIcon.ContextFlyout = new MenuFlyout
            {
                Items =
                {
                    openItem, exitItem
                }
            };

            if (_mainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(_trayIcon);
            }

        }
    }
}
