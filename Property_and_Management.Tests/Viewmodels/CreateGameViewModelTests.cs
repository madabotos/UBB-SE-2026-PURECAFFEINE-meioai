using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.Constants;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Service;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class CreateGameViewModelTests
    {
        private const int TestUserId = 42;
        private const string ValidGameName = "Settlers of Catan";
        private const decimal ValidPrice = 15.99m;
        private const int ValidMinPlayers = 2;
        private const int ValidMaxPlayers = 6;
        private const string ValidDescription = "A classic resource-trading board game for families.";

        private Mock<IGameService> mockGameService = null!;
        private Mock<ICurrentUserContext> mockUserContext = null!;
        private CreateGameViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            mockGameService = new Mock<IGameService>();
            mockUserContext = new Mock<ICurrentUserContext>();
            mockUserContext.SetupGet(ctx => ctx.CurrentUserId).Returns(TestUserId);
            mockGameService
                .Setup(svc => svc.ValidateGame(It.IsAny<GameDTO>()))
                .Returns((GameDTO gameDto) => GameInputHelper.BuildValidationErrors(
                    gameDto.Name,
                    gameDto.Price,
                    gameDto.MinimumPlayerNumber,
                    gameDto.MaximumPlayerNumber,
                    gameDto.Description,
                    DomainConstants.GameMinimumNameLength,
                    DomainConstants.GameMaximumNameLength,
                    DomainConstants.GameMinimumAllowedPrice,
                    DomainConstants.GameMinimumPlayerCount,
                    DomainConstants.GameMinimumDescriptionLength,
                    DomainConstants.GameMaximumDescriptionLength));

            viewModel = new CreateGameViewModel(mockGameService.Object, mockUserContext.Object);
        }

        [Test]
        public void Constructor_InitializesCurrentUserAndDefaultState()
        {
            Assert.Multiple(() =>
            {
                Assert.That(viewModel.CurrentUserId, Is.EqualTo(TestUserId));
                Assert.That(viewModel.GameName, Is.EqualTo(string.Empty));
                Assert.That(viewModel.GameDescription, Is.EqualTo(string.Empty));
                Assert.That(viewModel.IsGameActive, Is.True);
                Assert.That(viewModel.GameImage, Is.Null);
            });
        }

        [Test]
        public void ValidateGameInputs_CoversValidAndInvalidScenarios()
        {
            PopulateWithValidInputs();
            Assert.That(viewModel.ValidateGameInputs(), Is.Empty);

            AssertValidationError(vm => vm.GameName = "AB", "Name");
            AssertValidationError(vm => vm.GameName = string.Empty, "Name");
            AssertValidationError(vm => vm.GamePrice = 0m, "Price");
            AssertValidationError(vm => vm.MinimumPlayersRequired = 0, "player");
            AssertValidationError(vm =>
            {
                vm.MinimumPlayersRequired = 5;
                vm.MaximumPlayersAllowed = 2;
            }, "Maximum");
            AssertValidationError(vm => vm.GameDescription = "Short", "Description");

            PopulateWithValidInputs();
            viewModel.GameName = string.Empty;
            viewModel.GamePrice = 0m;
            viewModel.GameDescription = string.Empty;

            List<string> errors = viewModel.ValidateGameInputs();

            Assert.That(errors.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void PriceHelpers_ParseAndRoundTripValues()
        {
            viewModel.SetGamePriceFromText("25.50");
            Assert.That(viewModel.GamePrice, Is.EqualTo(25.50m));

            viewModel.GamePrice = 10m;
            viewModel.SetGamePriceFromText(string.Empty);
            Assert.That(viewModel.GamePrice, Is.EqualTo(0m));

            viewModel.GamePrice = 10m;
            viewModel.SetGamePriceFromText("not-a-price");
            Assert.That(viewModel.GamePrice, Is.EqualTo(0m));

            viewModel.GamePriceAsDouble = 19.99;

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.GamePrice, Is.EqualTo(19.99m));
                Assert.That(viewModel.GamePriceAsDouble, Is.EqualTo(19.99).Within(0.001));
            });
        }

        [Test]
        public void SubmitCreateGame_CoversSuccessAndValidationFailure()
        {
            PopulateWithValidInputs();

            ViewOperationResult successResult = viewModel.SubmitCreateGame();

            Assert.That(successResult.IsSuccess, Is.True);
            mockGameService.Verify(svc => svc.AddGame(It.Is<GameDTO>(game =>
                game.Owner.Id == TestUserId &&
                game.Name == ValidGameName &&
                game.Price == ValidPrice)), Times.Once);

            mockGameService.Invocations.Clear();
            viewModel.GameName = string.Empty;

            ViewOperationResult failureResult = viewModel.SubmitCreateGame();

            Assert.Multiple(() =>
            {
                Assert.That(failureResult.IsSuccess, Is.False);
                Assert.That(failureResult.DialogTitle, Is.EqualTo("Validation Error"));
            });
            mockGameService.Verify(svc => svc.AddGame(It.IsAny<GameDTO>()), Times.Never);
        }

        [Test]
        public void SaveGame_CoversSuccessAndValidationFailure()
        {
            PopulateWithValidInputs();

            GameDTO savedGame = viewModel.SaveGame();

            Assert.Multiple(() =>
            {
                Assert.That(savedGame, Is.Not.Null);
                Assert.That(savedGame.Owner.Id, Is.EqualTo(TestUserId));
                Assert.That(savedGame.Name, Is.EqualTo(ValidGameName));
                Assert.That(savedGame.Price, Is.EqualTo(ValidPrice));
                Assert.That(savedGame.MinimumPlayerNumber, Is.EqualTo(ValidMinPlayers));
                Assert.That(savedGame.MaximumPlayerNumber, Is.EqualTo(ValidMaxPlayers));
            });
            mockGameService.Verify(svc => svc.AddGame(It.IsAny<GameDTO>()), Times.Once);

            mockGameService.Invocations.Clear();
            viewModel.GameName = string.Empty;

            GameDTO invalidGame = viewModel.SaveGame();

            Assert.That(invalidGame, Is.Null);
            mockGameService.Verify(svc => svc.AddGame(It.IsAny<GameDTO>()), Times.Never);
        }

        private void AssertValidationError(Action<CreateGameViewModel> mutate, string expectedMessageFragment)
        {
            PopulateWithValidInputs();
            mutate(viewModel);

            List<string> errors = viewModel.ValidateGameInputs();

            Assert.That(errors, Has.Some.Contain(expectedMessageFragment));
        }

        private void PopulateWithValidInputs()
        {
            viewModel.GameName = ValidGameName;
            viewModel.GamePrice = ValidPrice;
            viewModel.MinimumPlayersRequired = ValidMinPlayers;
            viewModel.MaximumPlayersAllowed = ValidMaxPlayers;
            viewModel.GameDescription = ValidDescription;
        }
    }
}
