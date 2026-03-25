using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class RentalsFromOthersPage : Page
    {
        public RentalsFromOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RentalsFromOthersViewModel vm)
            {
                DataContext = vm;
                return;
            }

            if (DataContext is not RentalsFromOthersViewModel)
            {
                var rentalService = new RentalService(new RentalRepository(), new GameRepository());
                DataContext = new RentalsFromOthersViewModel(rentalService);
            }
        }

        private void RentalItem_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RentalDTO rental && rental.Id > 0)
                Frame?.Navigate(typeof(ChatView), rental.Id);
        }

        private void RentalItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RentalDTO rental && rental.Id > 0)
                Frame?.Navigate(typeof(ChatView), rental.Id);
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img)
                return;

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

            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.png"));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RentalsFromOthersViewModel)?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RentalsFromOthersViewModel)?.PrevPage();
        }
    }
}
