using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class EditGameViewModelTests
    {
        private const int SampleGameIdentifier = 42;
        private const int SampleOwnerIdentifier = 1;

        private Mock<IGameService> gameServiceMock = null!;
        private EditGameViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            gameServiceMock = new Mock<IGameService>();
            viewModel = new EditGameViewModel(gameServiceMock.Object);
        }

        [Test]
        public void LoadGame_PopulatesPropertiesFromService()
        {
            // arrange — this test verifies that the parameter-shadowing bug in
            // LoadGame is fixed; the public GameIdentifier property must reflect
            // the loaded game after the call.
            var existingGame = new GameDataTransferObject
            {
                Identifier = SampleGameIdentifier,
                Owner = new UserDataTransferObject { Identifier = SampleOwnerIdentifier },
                Name = "Existing Game",
                Price = 15m,
                MinimumPlayerNumber = 2,
                MaximumPlayerNumber = 5,
                Description = "A long enough description for validation.",
                IsActive = true,
            };
            gameServiceMock
                .Setup(service => service.GetGameByIdentifier(SampleGameIdentifier))
                .Returns(existingGame);

            // act
            viewModel.LoadGame(SampleGameIdentifier);

            // assert
            viewModel.GameIdentifier.Should().Be(SampleGameIdentifier);
            viewModel.OwnerIdentifier.Should().Be(SampleOwnerIdentifier);
            viewModel.Name.Should().Be("Existing Game");
        }

        [Test]
        public void UpdateGame_InvalidInputs_DoesNotCallService()
        {
            // arrange
            viewModel.Name = string.Empty;

            // act
            viewModel.UpdateGame();

            // assert
            gameServiceMock.Verify(
                service => service.UpdateGameByIdentifier(
                    It.IsAny<int>(), It.IsAny<GameDataTransferObject>()),
                Times.Never);
        }

        [Test]
        public void UpdateGame_ValidInputs_CallsUpdateWithCorrectIdentifier()
        {
            // arrange
            gameServiceMock
                .Setup(service => service.GetGameByIdentifier(SampleGameIdentifier))
                .Returns(new GameDataTransferObject
                {
                    Identifier = SampleGameIdentifier,
                    Owner = new UserDataTransferObject { Identifier = SampleOwnerIdentifier },
                    Name = "Valid Name",
                    Price = 10m,
                    MinimumPlayerNumber = 2,
                    MaximumPlayerNumber = 4,
                    Description = "A description long enough to pass validation.",
                    IsActive = true,
                });
            viewModel.LoadGame(SampleGameIdentifier);

            // act
            viewModel.UpdateGame();

            // assert
            gameServiceMock.Verify(
                service => service.UpdateGameByIdentifier(
                    SampleGameIdentifier, It.IsAny<GameDataTransferObject>()),
                Times.Once);
        }
    }
}
