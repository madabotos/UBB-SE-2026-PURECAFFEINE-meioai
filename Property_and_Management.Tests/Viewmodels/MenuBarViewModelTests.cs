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
            // arrange — constructor fills the dictionary

            // act
            var navigationActions = viewModel.NavigationActions;

            // assert — one action for each known menu label
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
            // arrange
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            // act
            viewModel.SelectedPageName = "My Games";

            // assert
            capturedPage.Should().Be(AppPage.Listings);
        }

        [Test]
        public void SelectedPageName_Notifications_RaisesNavigationToNotifications()
        {
            // arrange
            AppPage? capturedPage = null;
            viewModel.RequestNavigation += page => capturedPage = page;

            // act
            viewModel.SelectedPageName = "Notifications";

            // assert
            capturedPage.Should().Be(AppPage.Notifications);
        }

        [Test]
        public void SelectedPageName_UnknownName_DoesNotRaiseNavigation()
        {
            // arrange
            var navigationWasRaised = false;
            viewModel.RequestNavigation += _ => navigationWasRaised = true;

            // act
            viewModel.SelectedPageName = "Non-existent page";

            // assert
            navigationWasRaised.Should().BeFalse();
        }

        [Test]
        public void SelectedPageName_SettingTheSameValueTwice_OnlyRaisesNavigationOnce()
        {
            // arrange
            var navigationCount = 0;
            viewModel.RequestNavigation += _ => navigationCount++;

            // act
            viewModel.SelectedPageName = "My Rentals";
            viewModel.SelectedPageName = "My Rentals";

            // assert — second assignment is a no-op because the setter short-circuits on equal values
            navigationCount.Should().Be(1);
        }
    }
}
