using System;
using System.Collections.Generic;
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
    public sealed class GameServiceTests
    {
        private const int SampleGameIdentifier = 42;
        private const int SampleOwnerIdentifier = 1;

        private Mock<IGameRepository> gameRepositoryMock = null!;
        private Mock<IRentalRepository> rentalRepositoryMock = null!;
        private Mock<IMapper<Game, GameDataTransferObject>> gameMapperMock = null!;
        private Mock<IRequestService> requestServiceMock = null!;
        private GameService gameService = null!;

        [SetUp]
        public void SetUp()
        {
            gameRepositoryMock = new Mock<IGameRepository>();
            rentalRepositoryMock = new Mock<IRentalRepository>();
            gameMapperMock = new Mock<IMapper<Game, GameDataTransferObject>>();
            requestServiceMock = new Mock<IRequestService>();

            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(It.IsAny<int>()))
                .Returns(ImmutableList<Rental>.Empty);
            gameMapperMock
                .Setup(mapper => mapper.ToDataTransferObject(It.IsAny<Game>()))
                .Returns(new GameDataTransferObject { Identifier = SampleGameIdentifier });
            gameMapperMock
                .Setup(mapper => mapper.ToModel(It.IsAny<GameDataTransferObject>()))
                .Returns(new Game { Identifier = SampleGameIdentifier });

            gameService = new GameService(
                gameRepositoryMock.Object,
                rentalRepositoryMock.Object,
                gameMapperMock.Object,
                requestServiceMock.Object);
        }

        [Test]
        public void AddGame_DelegatesToRepository()
        {
            // arrange
            var gameDataTransferObject = new GameDataTransferObject { Identifier = SampleGameIdentifier };

            // act
            gameService.AddGame(gameDataTransferObject);

            // assert
            gameRepositoryMock.Verify(repository => repository.Add(It.IsAny<Game>()), Times.Once);
        }

        [Test]
        public void UpdateGameByIdentifier_DelegatesToRepository()
        {
            // arrange
            var gameDataTransferObject = new GameDataTransferObject { Identifier = SampleGameIdentifier };

            // act
            gameService.UpdateGameByIdentifier(SampleGameIdentifier, gameDataTransferObject);

            // assert
            gameRepositoryMock.Verify(
                repository => repository.Update(SampleGameIdentifier, It.IsAny<Game>()),
                Times.Once);
        }

        [Test]
        public void DeleteGameByIdentifier_NoRentals_DeletesAndNotifiesRequests()
        {
            // arrange
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList<Rental>.Empty);
            gameRepositoryMock
                .Setup(repository => repository.Delete(SampleGameIdentifier))
                .Returns(new Game { Identifier = SampleGameIdentifier });

            // act
            gameService.DeleteGameByIdentifier(SampleGameIdentifier);

            // assert
            requestServiceMock.Verify(
                service => service.OnGameDeactivated(SampleGameIdentifier), Times.Once);
            gameRepositoryMock.Verify(
                repository => repository.Delete(SampleGameIdentifier), Times.Once);
        }

        [Test]
        public void DeleteGameByIdentifier_OneActiveRental_ThrowsInvalidOperation()
        {
            // arrange
            var activeRental = new Rental(
                identifier: 1,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(2, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.Now.AddDays(-1),
                endDate: DateTime.Now.AddDays(3));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(activeRental));

            // act
            var deleteAction = () => gameService.DeleteGameByIdentifier(SampleGameIdentifier);

            // assert
            deleteAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*1 active rental*");
        }

        [Test]
        public void DeleteGameByIdentifier_MultipleActiveRentals_ThrowsWithPluralMessage()
        {
            // arrange
            var activeRentalA = new Rental(
                identifier: 1,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(2, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.Now.AddDays(-1),
                endDate: DateTime.Now.AddDays(3));
            var activeRentalB = new Rental(
                identifier: 2,
                game: new Game { Identifier = SampleGameIdentifier },
                renter: new User(2, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.Now.AddDays(4),
                endDate: DateTime.Now.AddDays(6));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(activeRentalA, activeRentalB));

            // act
            var deleteAction = () => gameService.DeleteGameByIdentifier(SampleGameIdentifier);

            // assert
            deleteAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*2 active rentals*");
        }

        [Test]
        public void GetGameByIdentifier_ReturnsMappedDataTransferObject()
        {
            // arrange
            gameRepositoryMock
                .Setup(repository => repository.Get(SampleGameIdentifier))
                .Returns(new Game { Identifier = SampleGameIdentifier });

            // act
            var gameDataTransferObject = gameService.GetGameByIdentifier(SampleGameIdentifier);

            // assert
            gameDataTransferObject.Identifier.Should().Be(SampleGameIdentifier);
        }
    }
}
