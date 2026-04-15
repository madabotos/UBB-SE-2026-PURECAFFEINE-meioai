using System.Collections.Immutable;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class ListingsViewModelTests
    {
        private const int SampleCurrentUserIdentifier = 1;
        private const int SampleGameIdentifier = 42;

        private Mock<IGameService> gameServiceMock = null!;

        [SetUp]
        public void SetUp()
        {
            gameServiceMock = new Mock<IGameService>();
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList<GameDataTransferObject>.Empty);
        }

        [Test]
        public void Constructor_LoadsGamesFromServiceForCurrentUser()
        {
            // arrange
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(
                    BuildGame(identifier: 1),
                    BuildGame(identifier: 2)));

            // act
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            // assert
            viewModel.TotalCount.Should().Be(2);
        }

        [Test]
        public void LoadGames_ReloadsFromService()
        {
            // arrange
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);
            gameServiceMock.Invocations.Clear();

            // act
            viewModel.LoadGames();

            // assert
            gameServiceMock.Verify(
                service => service.GetGamesForOwner(SampleCurrentUserIdentifier),
                Times.Once);
        }

        [Test]
        public void DeleteGame_DelegatesToServiceAndReloads()
        {
            // arrange
            var gameToDelete = BuildGame(identifier: SampleGameIdentifier);
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(gameToDelete));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);
            gameServiceMock.Invocations.Clear();

            // act
            viewModel.DeleteGame(gameToDelete);

            // assert
            gameServiceMock.Verify(
                service => service.DeleteGameByIdentifier(SampleGameIdentifier),
                Times.Once);
            gameServiceMock.Verify(
                service => service.GetGamesForOwner(SampleCurrentUserIdentifier),
                Times.Once);
        }

        [Test]
        public void ShowingText_UsesGameVocabulary()
        {
            // arrange
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(BuildGame(identifier: 1)));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            // act
            var showingText = viewModel.ShowingText;

            // assert
            showingText.Should().Contain("games");
        }

        [Test]
        public void TryDeleteGame_ServiceThrows_ReturnsFailureDialog()
        {
            // arrange
            var gameToDelete = BuildGame(identifier: SampleGameIdentifier);
            gameServiceMock
                .Setup(service => service.DeleteGameByIdentifier(SampleGameIdentifier))
                .Throws(new System.Exception("delete failed"));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            // act
            var result = viewModel.TryDeleteGame(gameToDelete);

            // assert
            result.IsSuccess.Should().BeFalse();
            result.DialogTitle.Should().Be(Constants.DialogTitles.CannotDeleteGame);
            result.DialogMessage.Should().Be("delete failed");
        }

        private static GameDataTransferObject BuildGame(int identifier)
        {
            return new GameDataTransferObject
            {
                Identifier = identifier,
                Owner = new UserDataTransferObject { Identifier = SampleCurrentUserIdentifier },
                Name = $"Game {identifier}",
                Price = 10m,
                MinimumPlayerNumber = 2,
                MaximumPlayerNumber = 4,
                Description = "A game",
                IsActive = true,
            };
        }
    }
}
