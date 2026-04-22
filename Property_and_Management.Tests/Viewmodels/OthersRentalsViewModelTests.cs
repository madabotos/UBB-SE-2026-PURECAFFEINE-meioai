using System;
using System.Collections.Immutable;
using System.Linq;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class RentalsToOthersViewModelTests
    {
        private const int Test_Id = 123;
        private Mock<IRentalService> mockRentalService = null!;
        private Mock<ICurrentUserContext> mockUserContext = null!;
        private RentalsToOthersViewModel viewModelToTest = null!;

        [SetUp]
        public void SetUp()
        {
            mockRentalService = new Mock<IRentalService>();
            mockUserContext = new Mock<ICurrentUserContext>();
            mockUserContext.SetupGet(current => current.CurrentUserId).Returns(Test_Id);
            mockRentalService
                .Setup(service => service.GetRentalsForOwner(Test_Id))
                .Returns(ImmutableList<RentalDTO>.Empty);
        }

        [Test]
        public void ShowingText_WithRentals_ContainsCountAndRentalsKeyword()
        {
            var fake4RentalsList = ImmutableList.Create(
                BuildFakeRental(10),
                BuildFakeRental(20),
                BuildFakeRental(30),
                BuildFakeRental(50));

            mockRentalService.Setup(service => service.GetRentalsForOwner(Test_Id)).Returns(fake4RentalsList);

            viewModelToTest = new RentalsToOthersViewModel(mockRentalService.Object, mockUserContext.Object);
            string statusText = viewModelToTest.ShowingText;
            Assert.That(statusText, Does.Contain("rentals"));
            Assert.That(statusText, Does.Contain("4"));
        }

        [Test]
        public void Constructor_WithRentals_SetsCorrectOwnerIdAndTotalCount()
        {
            var fake3RentalList = ImmutableList.Create(
                BuildFakeRental(10),
                BuildFakeRental(20),
                BuildFakeRental(30),
                BuildFakeRental(40));
            mockRentalService.Setup(service => service.GetRentalsForOwner(Test_Id)).Returns(fake3RentalList);
            var pagedRentalIds = viewModelToTest.PagedItems.Select(rental => rental.Id).ToList();

            viewModelToTest = new RentalsToOthersViewModel(mockRentalService.Object, mockUserContext.Object);
            Assert.That(viewModelToTest.TotalCount, Is.EqualTo(4));
            Assert.That(viewModelToTest.CurrentGameOwnerUserId, Is.EqualTo(Test_Id));
        }

        [Test]
        public void Constructor_WithRentals_PagedItemsContainCorrectRentalDetails()
        {
            var fake3RentalList = ImmutableList.Create(
                BuildFakeRental(10),
                BuildFakeRental(20),
                BuildFakeRental(30));

            mockRentalService.Setup(service => service.GetRentalsForOwner(Test_Id)).Returns(fake3RentalList);
            viewModelToTest = new RentalsToOthersViewModel(mockRentalService.Object, mockUserContext.Object);

            var pagedRentalIds = viewModelToTest.PagedItems.Select(rental => rental.Id).ToList();

            Assert.That(viewModelToTest.PagedItems.All(rental => rental.Game.Id == 1), Is.True);
            Assert.That(viewModelToTest.PagedItems.All(rental => rental.Owner.Id == Test_Id), Is.True);

            Assert.That(pagedRentalIds, Does.Contain(10));
            Assert.That(pagedRentalIds, Does.Contain(20));
            Assert.That(pagedRentalIds, Does.Contain(30));
        }
        [Test]
        public void LoadRentals_AfterServiceDataChanged_RefreshesTotalCountAndPagedItems()
        {
            var fake3RentalList = ImmutableList.Create(
                BuildFakeRental(10),
                BuildFakeRental(20),
                BuildFakeRental(30));
            var fake4RentalsList = ImmutableList.Create(
                BuildFakeRental(10),
                BuildFakeRental(20),
                BuildFakeRental(30),
                BuildFakeRental(50));

            mockRentalService.Setup(service => service.GetRentalsForOwner(Test_Id)).Returns(fake3RentalList);
            viewModelToTest = new RentalsToOthersViewModel(mockRentalService.Object, mockUserContext.Object);
            Assert.That(viewModelToTest.TotalCount, Is.EqualTo(3));

            mockRentalService.Setup(service => service.GetRentalsForOwner(Test_Id)).Returns(fake4RentalsList);
            viewModelToTest.LoadRentals();

            var pagedRentalIds = viewModelToTest.PagedItems.Select(rental => rental.Id).ToList();

            Assert.That(viewModelToTest.TotalCount, Is.EqualTo(4));
            Assert.That(pagedRentalIds, Does.Contain(50));
        }
        private static RentalDTO BuildFakeRental(int rentalId)
        {
            return new RentalDTO
            {
                Id = rentalId,
                Game = new GameDTO { Id = 1 },
                Renter = new UserDTO { Id = 2 },
                Owner = new UserDTO { Id = Test_Id },
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}