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
                .SetupGet(context => context.currentUserId)
                .Returns(SampleCurrentUserIdentifier);
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList<GameDTO>.Empty);
            userServiceMock
                .Setup(service => service.GetUsersExcept(SampleCurrentUserIdentifier))
                .Returns(ImmutableList<UserDTO>.Empty);

            viewModel = new CreateRentalViewModel(
                gameServiceMock.Object,
                rentalServiceMock.Object,
                userServiceMock.Object,
                currentUserContextMock.Object)
            {
                SelectedGame = new GameDTO
                {
                    id = SampleGameIdentifier,
                    IsActive = true,
                },
                SelectedRenter = new UserDTO { id = SampleRenterIdentifier },
                StartDate = DateTimeOffset.Now.AddDays(1),
                EndDate = DateTimeOffset.Now.AddDays(3),
            };
        }

        [Test]
        public void LoadData_OnlyExposesActiveGames()
        {
            var activeGame = new GameDTO { id = 1, IsActive = true };
            var inactiveGame = new GameDTO { id = 2, IsActive = false };
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(activeGame, inactiveGame));

            viewModel.LoadData();

            viewModel.MyGames.Should().Contain(game => game.id == 1);
            viewModel.MyGames.Should().NotContain(game => game.id == 2);
        }

        [Test]
        public void ValidateInputs_MissingSelectedGame_ReturnsFalse()
        {
            viewModel.SelectedGame = null!;

            var isValid = viewModel.ValidateInputs();

            isValid.Should().BeFalse();
        }

        [Test]
        public void ValidateInputs_MissingSelectedRenter_ReturnsFalse()
        {
            viewModel.SelectedRenter = null!;

            var isValid = viewModel.ValidateInputs();

            isValid.Should().BeFalse();
        }

        [Test]
        public void SaveRental_InvalidInputs_ReturnsValidationMessage()
        {
            viewModel.StartDate = null;

            var errorMessage = viewModel.SaveRental();

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
            var errorMessage = viewModel.SaveRental();

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
            rentalServiceMock
                .Setup(service => service.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("slot taken"));

            var errorMessage = viewModel.SaveRental();

            errorMessage.Should().Be("slot taken");
        }
    }
}