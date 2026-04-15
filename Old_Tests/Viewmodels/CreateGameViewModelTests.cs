using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class CreateGameViewModelTests
    {
        private const int SampleCurrentUserIdentifier = 1;

        private Mock<IGameService> gameServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private CreateGameViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            gameServiceMock = new Mock<IGameService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleCurrentUserIdentifier);

            viewModel = new CreateGameViewModel(
                gameServiceMock.Object, currentUserContextMock.Object)
            {
                Name = "Valid Name",
                Price = 10m,
                MinimumPlayers = 2,
                MaximumPlayers = 4,
                Description = "A well-described game that meets length requirements.",
                IsActive = true,
            };
        }

        [Test]
        public void ValidateInputs_EmptyName_ReportsNameLengthError()
        {
            // arrange
            viewModel.Name = string.Empty;

            // act
            var errors = viewModel.ValidateInputs();

            // assert
            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void ValidateInputs_PriceBelowMinimum_ReportsPriceError()
        {
            // arrange
            viewModel.Price = 0m;

            // act
            var errors = viewModel.ValidateInputs();

            // assert
            errors.Should().Contain(message => message.Contains("Price"));
        }

        [Test]
        public void ValidateInputs_MaxPlayersBelowMin_ReportsPlayerOrderError()
        {
            // arrange
            viewModel.MinimumPlayers = 4;
            viewModel.MaximumPlayers = 2;

            // act
            var errors = viewModel.ValidateInputs();

            // assert
            errors.Should().Contain(message => message.Contains("player"));
        }

        [Test]
        public void ValidateInputs_DescriptionTooShort_ReportsDescriptionError()
        {
            // arrange
            viewModel.Description = "short";

            // act
            var errors = viewModel.ValidateInputs();

            // assert
            errors.Should().Contain(message => message.Contains("Description"));
        }

        [Test]
        public void ValidateInputs_AllValid_ReturnsEmpty()
        {
            // arrange — default setup is all valid

            // act
            var errors = viewModel.ValidateInputs();

            // assert
            errors.Should().BeEmpty();
        }

        [Test]
        public void SaveGame_InvalidInputs_DoesNotCallService()
        {
            // arrange
            viewModel.Name = string.Empty;

            // act
            viewModel.SaveGame();

            // assert
            gameServiceMock.Verify(
                service => service.AddGame(It.IsAny<GameDataTransferObject>()), Times.Never);
        }

        [Test]
        public void SaveGame_ValidInputs_PersistsGameDataTransferObject()
        {
            // arrange — default setup valid

            // act
            viewModel.SaveGame();

            // assert
            gameServiceMock.Verify(
                service => service.AddGame(It.Is<GameDataTransferObject>(
                    gameDataTransferObject => gameDataTransferObject.Name == "Valid Name"
                           && gameDataTransferObject.Owner.Identifier == SampleCurrentUserIdentifier)),
                Times.Once);
        }
    }
}
