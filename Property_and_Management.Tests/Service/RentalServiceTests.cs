using System;
using System.Collections.Immutable;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Service;

namespace Property_and_Management.Tests.Service
{
    [TestFixture]
    public sealed class RentalServiceTests
    {
        private const int SampleGameIdentifier = 10;
        private const int SampleOwnerIdentifier = 1;
        private const int SampleRenterIdentifier = 2;
        private const int UnrelatedUserIdentifier = 999;

        private Mock<IRentalRepository> rentalRepositoryMock = null!;
        private Mock<IGameRepository> gameRepositoryMock = null!;
        private Mock<IMapper<Rental, RentalDataTransferObject>> rentalMapperMock = null!;
        private RentalService rentalService = null!;

        [SetUp]
        public void SetUp()
        {
            rentalRepositoryMock = new Mock<IRentalRepository>();
            gameRepositoryMock = new Mock<IGameRepository>();
            rentalMapperMock = new Mock<IMapper<Rental, RentalDataTransferObject>>();

            gameRepositoryMock
                .Setup(repository => repository.Get(SampleGameIdentifier))
                .Returns(new Game
                {
                    Identifier = SampleGameIdentifier,
                    Owner = new User(SampleOwnerIdentifier, "Owner"),
                    IsActive = true,
                });
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList<Rental>.Empty);

            rentalService = new RentalService(
                rentalRepositoryMock.Object,
                gameRepositoryMock.Object,
                rentalMapperMock.Object);
        }

        [Test]
        public void CreateConfirmedRental_OwnerMismatch_ThrowsInvalidOperation()
        {
            var createAction = () => rentalService.CreateConfirmedRental(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                UnrelatedUserIdentifier,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(3));

            createAction.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void CreateConfirmedRental_HappyPath_CallsAddConfirmed()
        {
            rentalService.CreateConfirmedRental(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(3));

            rentalRepositoryMock.Verify(
                repository => repository.AddConfirmed(It.IsAny<Rental>()), Times.Once);
        }

        [Test]
        public void CreateConfirmedRental_WhenSlotUnavailable_ThrowsAndDoesNotPersist()
        {
            var existingRental = new Rental(
                identifier: 1,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(3));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(existingRental));

            var createAction = () => rentalService.CreateConfirmedRental(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(2).AddHours(6));

            createAction.Should().Throw<InvalidOperationException>();
            rentalRepositoryMock.Verify(
                repository => repository.AddConfirmed(It.IsAny<Rental>()), Times.Never);
        }

        [Test]
        public void IsSlotAvailable_WithinBufferOfExistingRental_ReturnsFalse()
        {
            var existingRental = new Rental(
                identifier: 1,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(10),
                endDate: DateTime.UtcNow.AddDays(12));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(existingRental));

            var isAvailable = rentalService.IsSlotAvailable(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(13),
                DateTime.UtcNow.AddDays(13).AddHours(6));

            isAvailable.Should().BeFalse();
        }

        [Test]
        public void IsSlotAvailable_OutsideBuffer_ReturnsTrue()
        {
            var existingRental = new Rental(
                identifier: 1,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(3));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(existingRental));

            var isAvailable = rentalService.IsSlotAvailable(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(12));

            isAvailable.Should().BeTrue();
        }
    }
}
