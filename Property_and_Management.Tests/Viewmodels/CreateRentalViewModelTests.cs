using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private const int OwnerUserId = 10;
        private const int RenterUserId = 20;
        private const int GameId = 100;

        private Mock<IGameService> mockGameService = null!;
        private Mock<IRentalService> mockRentalService = null!;
        private Mock<IUserService> mockUserService = null!;
        private Mock<ICurrentUserContext> mockUserContext = null!;

        [SetUp]
        public void SetUp()
        {
            mockGameService = new Mock<IGameService>();
            mockRentalService = new Mock<IRentalService>();
            mockUserService = new Mock<IUserService>();
            mockUserContext = new Mock<ICurrentUserContext>();

            mockUserContext.SetupGet(ctx => ctx.CurrentUserId).Returns(OwnerUserId);

            mockGameService
                .Setup(svc => svc.GetActiveGamesForOwner(OwnerUserId))
                .Returns(ImmutableList.Create(BuildActiveGame(GameId, OwnerUserId)));

            mockUserService
                .Setup(svc => svc.GetUsersExcept(OwnerUserId))
                .Returns(ImmutableList.Create(new UserDTO { Id = RenterUserId, DisplayName = "Renter" }));
        }

        private CreateRentalViewModel BuildViewModel()
        {
            return new CreateRentalViewModel(
                mockGameService.Object,
                mockRentalService.Object,
                mockUserService.Object,
                mockUserContext.Object);
        }

        [Test]
        public void Constructor_LoadsCollectionsCurrentUserAndRefreshesData()
        {
            var viewModel = BuildViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.CurrentUserId, Is.EqualTo(OwnerUserId));
                Assert.That(viewModel.OwnedActiveGames.Select(game => game.Id), Is.EquivalentTo(new[] { GameId }));
                Assert.That(viewModel.OwnedActiveGames.All(game => game.IsActive), Is.True);
                Assert.That(viewModel.AvailableRenters.Select(user => user.Id), Is.EquivalentTo(new[] { RenterUserId }));
            });

            mockGameService
                .Setup(svc => svc.GetActiveGamesForOwner(OwnerUserId))
                .Returns(ImmutableList.Create(
                    BuildActiveGame(GameId, OwnerUserId),
                    BuildActiveGame(201, OwnerUserId)));

            mockUserService
                .Setup(svc => svc.GetUsersExcept(OwnerUserId))
                .Returns(ImmutableList.Create(
                    new UserDTO { Id = RenterUserId, DisplayName = "Renter" },
                    new UserDTO { Id = 21, DisplayName = "Second renter" }));

            viewModel.LoadRentalFormData();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.OwnedActiveGames.Select(game => game.Id), Is.EquivalentTo(new[] { GameId, 201 }));
                Assert.That(viewModel.AvailableRenters.Select(user => user.Id), Is.EquivalentTo(new[] { RenterUserId, 21 }));
            });
        }

        [Test]
        public void ValidateRentalInputs_RequiresGameRenterAndDates()
        {
            var viewModel = BuildViewModel();

            PopulateWithValidSelections(viewModel);
            Assert.That(viewModel.ValidateRentalInputs(), Is.True);

            AssertInvalidRentalInputs(viewModel, vm => vm.SelectedGameToRent = null);
            AssertInvalidRentalInputs(viewModel, vm => vm.SelectedRenter = null);
            AssertInvalidRentalInputs(viewModel, vm => vm.StartDate = null);
            AssertInvalidRentalInputs(viewModel, vm => vm.EndDate = null);
        }

        [Test]
        public void CreateRental_CoversSuccessValidationFailureAndExceptions()
        {
            var invalidViewModel = BuildViewModel();

            ViewOperationResult validationFailure = invalidViewModel.CreateRental();

            Assert.Multiple(() =>
            {
                Assert.That(validationFailure.IsSuccess, Is.False);
                Assert.That(validationFailure.DialogTitle, Is.EqualTo("Validation Error"));
            });
            mockRentalService.Verify(
                svc => svc.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);

            var successfulViewModel = BuildViewModel();
            PopulateWithValidSelections(successfulViewModel);

            ViewOperationResult successResult = successfulViewModel.CreateRental();

            Assert.That(successResult.IsSuccess, Is.True);
            mockRentalService.Verify(svc => svc.CreateConfirmedRental(
                GameId,
                RenterUserId,
                OwnerUserId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()), Times.Once);

            mockRentalService.Setup(svc => svc.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new ArgumentException("Start date must be before end date and not in the past."));

            var argumentExceptionViewModel = BuildViewModel();
            PopulateWithValidSelections(argumentExceptionViewModel);

            ViewOperationResult argumentExceptionResult = argumentExceptionViewModel.CreateRental();

            Assert.Multiple(() =>
            {
                Assert.That(argumentExceptionResult.IsSuccess, Is.False);
                Assert.That(argumentExceptionResult.DialogTitle, Is.EqualTo("Validation Error"));
            });

            mockRentalService.Setup(svc => svc.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("Dates overlap with existing rental."));

            var unexpectedExceptionViewModel = BuildViewModel();
            PopulateWithValidSelections(unexpectedExceptionViewModel);

            ViewOperationResult unexpectedExceptionResult = unexpectedExceptionViewModel.CreateRental();

            Assert.Multiple(() =>
            {
                Assert.That(unexpectedExceptionResult.IsSuccess, Is.False);
                Assert.That(unexpectedExceptionResult.DialogTitle, Is.EqualTo("Rental Failed"));
                Assert.That(unexpectedExceptionResult.DialogMessage, Does.Contain("overlap"));
            });
        }

        [Test]
        public void SaveRental_CoversSuccessValidationFailureAndServiceMessage()
        {
            var successfulViewModel = BuildViewModel();
            PopulateWithValidSelections(successfulViewModel);

            string? successMessage = successfulViewModel.SaveRental();
            Assert.That(successMessage, Is.Null);

            var invalidViewModel = BuildViewModel();
            string? validationMessage = invalidViewModel.SaveRental();
            Assert.That(validationMessage, Is.EqualTo("Validation failed."));

            mockRentalService.Setup(svc => svc.CreateConfirmedRental(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new Exception("Database connection lost."));

            var failingViewModel = BuildViewModel();
            PopulateWithValidSelections(failingViewModel);

            string? exceptionMessage = failingViewModel.SaveRental();
            Assert.That(exceptionMessage, Is.EqualTo("Database connection lost."));
        }

        [Test]
        public void Setters_RaisePropertyChangedForBindableFields()
        {
            var viewModel = BuildViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.SelectedGameToRent = BuildActiveGame(999, OwnerUserId);
            viewModel.SelectedRenter = new UserDTO { Id = 99, DisplayName = "Listener" };
            viewModel.StartDate = DateTimeOffset.Now.AddDays(1);
            viewModel.EndDate = DateTimeOffset.Now.AddDays(5);

            Assert.That(changedProperties, Is.EqualTo(new[]
            {
                nameof(viewModel.SelectedGameToRent),
                nameof(viewModel.SelectedRenter),
                nameof(viewModel.StartDate),
                nameof(viewModel.EndDate)
            }));
        }

        private static void AssertInvalidRentalInputs(CreateRentalViewModel viewModel, Action<CreateRentalViewModel> invalidate)
        {
            PopulateWithValidSelections(viewModel);
            invalidate(viewModel);
            Assert.That(viewModel.ValidateRentalInputs(), Is.False);
        }

        private static void PopulateWithValidSelections(CreateRentalViewModel viewModel)
        {
            viewModel.SelectedGameToRent = BuildActiveGame(GameId, OwnerUserId);
            viewModel.SelectedRenter = new UserDTO { Id = RenterUserId, DisplayName = "Renter" };
            viewModel.StartDate = DateTimeOffset.Now.AddDays(1);
            viewModel.EndDate = DateTimeOffset.Now.AddDays(7);
        }

        private static GameDTO BuildActiveGame(int gameId, int ownerId)
        {
            return new GameDTO
            {
                Id = gameId,
                Owner = new UserDTO { Id = ownerId },
                Name = "Test Game",
                Price = 10m,
                IsActive = true
            };
        }
    }
}
