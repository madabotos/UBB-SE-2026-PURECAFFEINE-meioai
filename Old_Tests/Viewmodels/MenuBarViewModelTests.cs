using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class MenuBarViewModelTests
    {
        private MenuBarViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            viewModel = new MenuBarViewModel();
        }

        [Test]
        public void NavigationActions_ContainsEntryForEveryAppPage()
        {
            var navigationActions = viewModel.NavigationActions;

            navigationActions.Keys.Should().Contain(new[]
            {
                "My Games",
                "Others' Requests",
                "Others' Rentals",
                "My Requests",
                "My Rentals",
                "Notifications",
            });
        }

        [Test]
        public void SelectedPageName_MyGames_RaisesNavigationToListings()
        {
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            viewModel.SelectedPageName = "My Games";

            capturedPage.Should().Be(AppPage.Listings);
        }

        [Test]
        public void SelectedPageName_Notifications_RaisesNavigationToNotifications()
        {
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            viewModel.SelectedPageName = "Notifications";

            capturedPage.Should().Be(AppPage.Notifications);
        }

        [Test]
        public void SelectedPageName_MyRentals_RaisesNavigationToRentalsFromOthers()
        {
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            viewModel.SelectedPageName = "My Rentals";

            capturedPage.Should().Be(AppPage.RentalsFromOthers);
        }

        [Test]
        public void SelectedPageName_OthersRentals_RaisesNavigationToRentalsToOthers()
        {
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            viewModel.SelectedPageName = "Others' Rentals";

            capturedPage.Should().Be(AppPage.RentalsToOthers);
        }

        [Test]
        public void SelectedPageName_UnknownName_DoesNotRaiseNavigation()
        {
            var navigationWasRaised = false;
            viewModel.RequestNavigation += _ => navigationWasRaised = true;

            viewModel.SelectedPageName = "Non-existent page";

            navigationWasRaised.Should().BeFalse();
        }

        [Test]
        public void SelectedPageName_SettingTheSameValueTwice_OnlyRaisesNavigationOnce()
        {
            var navigationCount = 0;
            viewModel.RequestNavigation += _ => navigationCount++;

            viewModel.SelectedPageName = "My Rentals";
            viewModel.SelectedPageName = "My Rentals";

            navigationCount.Should().Be(1);
        }
    }
}