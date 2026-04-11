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
    public sealed class CreateRentalViewModelTests
    {
        private const int SampleCurrentUserIdentifier = 1;
        private const int SampleGameIdentifier = 10;
        private const int SampleRenterIdentifier = 2;

        private Mock<IGameService> gameServiceMock = null!;
        private Mock<IRentalService> rentalServiceMock = null!;
        private Mock<IUserService> userServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private CreateRentalViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            gameServiceMock = new Mock<IGameService>();
            rentalServiceMock = new Mock<IRentalService>();
            userServiceMock = new Mock<IUserService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();

            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleCurrentUserIdentifier);
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList<GameDataTransferObject>.Empty);
            userServiceMock
                .Setup(service => service.GetUsersExcept(SampleCurrentUserIdentifier))
                .Returns(ImmutableList<UserDataTransferObject>.Empty);

            viewModel = new CreateRentalViewModel(
                gameServiceMock.Object,
                rentalServiceMock.Object,
                userServiceMock.Object,
                currentUserContextMock.Object)
            {
                SelectedGame = new GameDataTransferObject
                {
                    Identifier = SampleGameIdentifier,
                    IsActive = true,
                },
                SelectedRenter = new UserDataTransferObject { Identifier = SampleRenterIdentifier },
                StartDate = DateTimeOffset.Now.AddDays(1),
                EndDate = DateTimeOffset.Now.AddDays(3),
            };
        }

        [Test]
        public void LoadData_OnlyExposesActiveGames()
        {
            // arrange
            var activeGame = new GameDataTransferObject { Identifier = 1, IsActive = true };
            var inactiveGame = new GameDataTransferObject { Identifier = 2, IsActive = false };
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(activeGame, inactiveGame));

            // act
            viewModel.LoadData();

            // assert
            viewModel.MyGames.Should().Contain(game => game.Identifier == 1);
            viewModel.MyGames.Should().NotContain(game => game.Identifier == 2);
        }

        [Test]
        public void ValidateInputs_MissingSelectedGame_ReturnsFalse()
        {
            // arrange
            viewModel.SelectedGame = null!;

            // act
            var isValid = viewModel.ValidateInputs();

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void ValidateInputs_MissingSelectedRenter_ReturnsFalse()
        {
            // arrange
            viewModel.SelectedRenter = null!;

            // act
            var isValid = viewModel.ValidateInputs();

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void SaveRental_InvalidInputs_ReturnsValidationMessage()
        {
            // arrange
            viewModel.StartDate = null;

            // act
            var errorMessage = viewModel.SaveRental();

            // assert
            errorMessage.Should().NotBeNull();
            rentalServiceMock.Verify(
                service => service.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Test]
        public void SaveRental_HappyPath_ReturnsNullAndCallsService()
        {
            // arrange — defaults are valid

            // act
            var errorMessage = viewModel.SaveRental();

            // assert
            errorMessage.Should().BeNull();
            rentalServiceMock.Verify(
                service => service.CreateConfirmedRental(
                    SampleGameIdentifier,
                    SampleRenterIdentifier,
                    SampleCurrentUserIdentifier,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()),
                Times.Once);
        }

        [Test]
        public void SaveRental_ServiceThrows_ReturnsExceptionMessage()
        {
            // arrange
            rentalServiceMock
                .Setup(service => service.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("slot taken"));

            // act
            var errorMessage = viewModel.SaveRental();

            // assert
            errorMessage.Should().Be("slot taken");
        }
    }
}
