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
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleOwnerIdentifier);
            rentalServiceMock
                .Setup(service => service.GetRentalsForOwner(SampleOwnerIdentifier))
                .Returns(ImmutableList<RentalDataTransferObject>.Empty);
        }

        [Test]
        public void Constructor_LoadsRentalsForCurrentOwner()
        {
            // arrange
            rentalServiceMock
                .Setup(service => service.GetRentalsForOwner(SampleOwnerIdentifier))
                .Returns(ImmutableList.Create(BuildRental(1), BuildRental(2), BuildRental(3)));

            // act
            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            // assert
            viewModel.TotalCount.Should().Be(3);
            viewModel.OwnerIdentifier.Should().Be(SampleOwnerIdentifier);
        }

        [Test]
        public void LoadRentals_ReloadsFromService()
        {
            // arrange
            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);
            rentalServiceMock.Invocations.Clear();

            // act
            viewModel.LoadRentals();

            // assert
            rentalServiceMock.Verify(
                service => service.GetRentalsForOwner(SampleOwnerIdentifier),
                Times.Once);
        }

        [Test]
        public void ShowingText_UsesRentalsVocabulary()
        {
            // arrange
            var viewModel = new RentalsToOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            // act
            var text = viewModel.ShowingText;

            // assert
            text.Should().Contain("rentals");
        }

        private static RentalDataTransferObject BuildRental(int identifier)
        {
            return new RentalDataTransferObject
            {
                Identifier = identifier,
                Game = new GameDataTransferObject { Identifier = 100 },
                Renter = new UserDataTransferObject { Identifier = 99 },
                Owner = new UserDataTransferObject { Identifier = SampleOwnerIdentifier },
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
            };
        }
    }
}
