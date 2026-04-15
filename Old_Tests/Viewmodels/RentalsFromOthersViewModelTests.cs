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
    public sealed class RentalsFromOthersViewModelTests
    {
        private const int SampleRenterIdentifier = 1;

        private Mock<IRentalService> rentalServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;

        [SetUp]
        public void SetUp()
        {
            rentalServiceMock = new Mock<IRentalService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleRenterIdentifier);
            rentalServiceMock
                .Setup(service => service.GetRentalsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList<RentalDataTransferObject>.Empty);
        }

        [Test]
        public void Constructor_LoadsRentalsForCurrentRenter()
        {
            // arrange
            rentalServiceMock
                .Setup(service => service.GetRentalsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList.Create(BuildRental(1), BuildRental(2)));

            // act
            var viewModel = new RentalsFromOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            // assert
            viewModel.TotalCount.Should().Be(2);
            viewModel.RenterIdentifier.Should().Be(SampleRenterIdentifier);
        }

        [Test]
        public void Reload_OrdersRentalsByStartDateDescending()
        {
            // arrange
            var olderRental = BuildRental(identifier: 1, startDate: DateTime.UtcNow.AddDays(2));
            var newerRental = BuildRental(identifier: 2, startDate: DateTime.UtcNow.AddDays(10));
            rentalServiceMock
                .Setup(service => service.GetRentalsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList.Create(olderRental, newerRental));

            // act
            var viewModel = new RentalsFromOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            // assert — newest rental should come first
            viewModel.PagedItems[0].Identifier.Should().Be(2);
        }

        [Test]
        public void ShowingText_UsesRentalsVocabulary()
        {
            // arrange
            var viewModel = new RentalsFromOthersViewModel(rentalServiceMock.Object, currentUserContextMock.Object);

            // act
            var text = viewModel.ShowingText;

            // assert
            text.Should().Contain("rentals");
        }

        private static RentalDataTransferObject BuildRental(int identifier, DateTime? startDate = null)
        {
            return new RentalDataTransferObject
            {
                Identifier = identifier,
                Game = new GameDataTransferObject { Identifier = 100 },
                Renter = new UserDataTransferObject { Identifier = SampleRenterIdentifier },
                Owner = new UserDataTransferObject { Identifier = 99 },
                StartDate = startDate ?? DateTime.UtcNow.AddDays(1),
                EndDate = (startDate ?? DateTime.UtcNow.AddDays(1)).AddDays(2),
            };
        }
    }
}
