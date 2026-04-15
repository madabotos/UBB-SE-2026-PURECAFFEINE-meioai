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
                .Returns(ImmutableList<GameDTO>.Empty);
        }

        [Test]
        public void Constructor_LoadsGamesFromServiceForCurrentUser()
        {
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(
                    BuildGame(id: 1),
                    BuildGame(id: 2)));

            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            viewModel.TotalCount.Should().Be(2);
        }

        [Test]
        public void LoadGames_ReloadsFromService()
        {
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);
            gameServiceMock.Invocations.Clear();

            viewModel.LoadGames();

            gameServiceMock.Verify(
                service => service.GetGamesForOwner(SampleCurrentUserIdentifier),
                Times.Once);
        }

        [Test]
        public void DeleteGame_DelegatesToServiceAndReloads()
        {
            var gameToDelete = BuildGame(id: SampleGameIdentifier);
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(gameToDelete));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);
            gameServiceMock.Invocations.Clear();

            viewModel.DeleteGame(gameToDelete);

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
            gameServiceMock
                .Setup(service => service.GetGamesForOwner(SampleCurrentUserIdentifier))
                .Returns(ImmutableList.Create(BuildGame(id: 1)));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            var showingText = viewModel.ShowingText;

            showingText.Should().Contain("games");
        }

        [Test]
        public void TryDeleteGame_ServiceThrows_ReturnsFailureDialog()
        {
            var gameToDelete = BuildGame(id: SampleGameIdentifier);
            gameServiceMock
                .Setup(service => service.DeleteGameByIdentifier(SampleGameIdentifier))
                .Throws(new System.Exception("delete failed"));
            var viewModel = new ListingsViewModel(gameServiceMock.Object, SampleCurrentUserIdentifier);

            var result = viewModel.TryDeleteGame(gameToDelete);

            result.IsSuccess.Should().BeFalse();
            result.DialogTitle.Should().Be(Constants.DialogTitles.CannotDeleteGame);
            result.DialogMessage.Should().Be("delete failed");
        }

        private static GameDTO BuildGame(int id)
        {
            return new GameDTO
            {
                id = id,
                Owner = new UserDTO { id = SampleCurrentUserIdentifier },
                Name = $"Game {id}",
                Price = 10m,
                MinimumPlayerNumber = 2,
                MaximumPlayerNumber = 4,
                Description = "A game",
                IsActive = true,
            };
        }
    }
}