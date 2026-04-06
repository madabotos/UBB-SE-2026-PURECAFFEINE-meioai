using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class RentalsToOthersPage : Page
    {
        public RentalsToOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RentalsToOthersViewModel vm)
            {
                DataContext = vm;
                return;
            }

            if (DataContext is not RentalsToOthersViewModel)
            {
                DataContext = App.Services.GetRequiredService<RentalsToOthersViewModel>();
            }
        }

        private void CreateRentalButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(CreateRentalView));
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img)
                return;

            if (img.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith("/Assets/default-game-placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Resources.TryGetValue("DefaultGameImage", out var localResource) && localResource is BitmapImage localImage)
            {
                img.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue("DefaultGameImage", out var appResource) && appResource is BitmapImage appImage)
            {
                img.Source = appImage;
                return;
            }

            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RentalsToOthersViewModel)?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RentalsToOthersViewModel)?.PrevPage();
        }
    }
}
