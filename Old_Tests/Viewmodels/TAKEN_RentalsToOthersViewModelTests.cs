using System;
using System.Collections.Immutable;
using FluentAssertions;
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
        private const int SampleOwnerIdentifier = 1;

        private Mock<IRentalService> rentalServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;

        [SetUp]
        public void SetUp()
        {
            rentalServiceMock = new Mock<IRentalService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock
                .SetupGet(context => context.currentUserId)
                .Returns(SampleOwnerIdentifier);
            rentalServiceMock
                .Setup(service => service.GetRentalsForOwner(SampleOwnerIdentifier))
                .Returns(ImmutableList<RentalDTO>.Empty);
        }

        [Test]
        public void Constructor_LoadsRentalsForCurrentOwner()
        {
            rentalServiceMock
                .Setup(service => service.GetRentalsForOwner(SampleOwnerIdentifier))
                .Returns(ImmutableList.Create(BuildRental(1), BuildRental(2), BuildRental(3)));

            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            viewModel.TotalCount.Should().Be(3);
            viewModel.ownerId.Should().Be(SampleOwnerIdentifier);
        }

        [Test]
        public void LoadRentals_ReloadsFromService()
        {
            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);
            rentalServiceMock.Invocations.Clear();

            viewModel.LoadRentals();

            rentalServiceMock.Verify(
                service => service.GetRentalsForOwner(SampleOwnerIdentifier),
                Times.Once);
        }

        [Test]
        public void ShowingText_UsesRentalsVocabulary()
        {
            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            var text = viewModel.ShowingText;

            text.Should().Contain("rentals");
        }

        private static RentalDTO BuildRental(int id)
        {
            return new RentalDTO
            {
                id = id,
                Game = new GameDTO { id = 100 },
                Renter = new UserDTO { id = 99 },
                Owner = new UserDTO { id = SampleOwnerIdentifier },
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
            };
        }
    }
}