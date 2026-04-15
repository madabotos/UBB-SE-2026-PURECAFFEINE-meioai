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
                .SetupGet(context => context.currentUserId)
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
            viewModel.Name = string.Empty;

            var errors = viewModel.ValidateInputs();

            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void ValidateInputs_PriceBelowMinimum_ReportsPriceError()
        {
            viewModel.Price = 0m;

            var errors = viewModel.ValidateInputs();

            errors.Should().Contain(message => message.Contains("Price"));
        }

        [Test]
        public void ValidateInputs_MaxPlayersBelowMin_ReportsPlayerOrderError()
        {
            viewModel.MinimumPlayers = 4;
            viewModel.MaximumPlayers = 2;

            var errors = viewModel.ValidateInputs();

            errors.Should().Contain(message => message.Contains("player"));
        }

        [Test]
        public void ValidateInputs_DescriptionTooShort_ReportsDescriptionError()
        {
            viewModel.Description = "short";

            var errors = viewModel.ValidateInputs();

            errors.Should().Contain(message => message.Contains("Description"));
        }

        [Test]
        public void ValidateInputs_AllValid_ReturnsEmpty()
        {
            var errors = viewModel.ValidateInputs();

            errors.Should().BeEmpty();
        }

        [Test]
        public void SaveGame_InvalidInputs_DoesNotCallService()
        {
            viewModel.Name = string.Empty;

            viewModel.SaveGame();

            gameServiceMock.Verify(
                service => service.AddGame(It.IsAny<GameDTO>()), Times.Never);
        }

        [Test]
        public void SaveGame_ValidInputs_PersistsGameDTO()
        {
            viewModel.SaveGame();

            gameServiceMock.Verify(
                service => service.AddGame(It.Is<GameDTO>(
                    GameDTO => GameDTO.Name == "Valid Name"
                           && GameDTO.Owner.id == SampleCurrentUserIdentifier)),
                Times.Once);
        }
    }
}