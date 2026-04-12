using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
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

            if (e.Parameter is RentalsToOthersViewModel rentalsToOthersViewModel)
            {
                DataContext = rentalsToOthersViewModel;
                return;
            }

            if (DataContext is not RentalsToOthersViewModel)
            {
                // Composition root: fall back to the DI container when no
                // navigation parameter was passed.
                DataContext = App.Services.GetRequiredService<RentalsToOthersViewModel>();
            }
        }

        private void CreateRentalButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(CreateRentalView));
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageFailureHandler.HandleFailure(sender as Image, Resources);
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
