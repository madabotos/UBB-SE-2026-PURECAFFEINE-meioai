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
    public sealed class CreateRequestViewModelTests
    {
        private const int CurrentUserId = 5;
        private const int OtherOwnerId = 50;
        private const int AvailableGameId = 300;

        private Mock<IGameService> mockGameService = null!;
        private Mock<IRequestService> mockRequestService = null!;
        private Mock<ICurrentUserContext> mockUserContext = null!;

        [SetUp]
        public void SetUp()
        {
            mockGameService = new Mock<IGameService>();
            mockRequestService = new Mock<IRequestService>();
            mockUserContext = new Mock<ICurrentUserContext>();

            mockUserContext.SetupGet(ctx => ctx.CurrentUserId).Returns(CurrentUserId);

            mockGameService
                .Setup(svc => svc.GetAvailableGamesForRenter(CurrentUserId))
                .Returns(ImmutableList.Create(BuildOtherUsersGame(AvailableGameId)));
        }

        private CreateRequestViewModel BuildViewModel()
        {
            return new CreateRequestViewModel(
                mockGameService.Object,
                mockRequestService.Object,
                mockUserContext.Object);
        }

        [Test]
        public void Constructor_LoadsGamesCurrentUserAndRefreshesCollection()
        {
            var viewModel = BuildViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.CurrentUserId, Is.EqualTo(CurrentUserId));
                Assert.That(viewModel.AvailableGamesToRequest.Select(game => game.Id), Is.EquivalentTo(new[] { AvailableGameId }));
                Assert.That(viewModel.AvailableGamesToRequest.Any(game => game.Owner?.Id == CurrentUserId), Is.False);
                Assert.That(viewModel.AvailableGamesToRequest.All(game => game.IsActive), Is.True);
            });

            mockGameService
                .Setup(svc => svc.GetAvailableGamesForRenter(CurrentUserId))
                .Returns(ImmutableList.Create(
                    BuildOtherUsersGame(AvailableGameId),
                    BuildOtherUsersGame(401)));

            viewModel.LoadAvailableGames();

            Assert.That(viewModel.AvailableGamesToRequest.Select(game => game.Id), Is.EquivalentTo(new[] { AvailableGameId, 401 }));
        }

        [Test]
        public void ValidateRequestInputs_RequiresGameAndDates()
        {
            var viewModel = BuildViewModel();

            PopulateWithValidSelections(viewModel);
            Assert.That(viewModel.ValidateRequestInputs(), Is.True);

            AssertInvalidRequestInputs(viewModel, vm => vm.SelectedGame = null);
            AssertInvalidRequestInputs(viewModel, vm => vm.StartDate = null);
            AssertInvalidRequestInputs(viewModel, vm => vm.EndDate = null);
        }

        [Test]
        public void SubmitRequest_CoversValidationSuccessAndInvalidDateRange()
        {
            var invalidViewModel = BuildViewModel();

            ViewOperationResult validationFailure = invalidViewModel.SubmitRequest();

            Assert.Multiple(() =>
            {
                Assert.That(validationFailure.IsSuccess, Is.False);
                Assert.That(validationFailure.DialogTitle, Is.EqualTo("Validation Error"));
            });
            mockRequestService.Verify(
                svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);

            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Success(1));

            var successfulViewModel = BuildViewModel();
            PopulateWithValidSelections(successfulViewModel);

            ViewOperationResult successResult = successfulViewModel.SubmitRequest();

            Assert.That(successResult.IsSuccess, Is.True);
            mockRequestService.Verify(svc => svc.CreateRequest(
                AvailableGameId,
                CurrentUserId,
                OtherOwnerId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()), Times.Once);

            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.InvalidDateRange));

            var invalidDateRangeViewModel = BuildViewModel();
            PopulateWithValidSelections(invalidDateRangeViewModel);

            ViewOperationResult invalidDateRangeResult = invalidDateRangeViewModel.SubmitRequest();

            Assert.Multiple(() =>
            {
                Assert.That(invalidDateRangeResult.IsSuccess, Is.False);
                Assert.That(invalidDateRangeResult.DialogTitle, Is.EqualTo("Validation Error"));
            });
        }

        [Test]
        public void SubmitRequest_MapsServiceErrorsAndTrySubmitRequestMirrorsResult()
        {
            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.OwnerCannotRent));

            var ownerCannotRentViewModel = BuildViewModel();
            PopulateWithValidSelections(ownerCannotRentViewModel);

            ViewOperationResult ownerCannotRentResult = ownerCannotRentViewModel.SubmitRequest();

            Assert.Multiple(() =>
            {
                Assert.That(ownerCannotRentResult.IsSuccess, Is.False);
                Assert.That(ownerCannotRentResult.DialogTitle, Is.EqualTo("Request Failed"));
                Assert.That(ownerCannotRentResult.DialogMessage, Does.Contain("own game"));
                Assert.That(ownerCannotRentViewModel.TrySubmitRequest(), Does.Contain("own game"));
            });

            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.DatesUnavailable));

            var datesUnavailableViewModel = BuildViewModel();
            PopulateWithValidSelections(datesUnavailableViewModel);
            Assert.That(datesUnavailableViewModel.SubmitRequest().DialogMessage, Does.Contain("not available"));

            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.GameDoesNotExist));

            var missingGameViewModel = BuildViewModel();
            PopulateWithValidSelections(missingGameViewModel);
            Assert.That(missingGameViewModel.SubmitRequest().DialogMessage, Does.Contain("no longer exists"));

            mockRequestService
                .Setup(svc => svc.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Success(1));

            var successfulTrySubmitViewModel = BuildViewModel();
            PopulateWithValidSelections(successfulTrySubmitViewModel);
            Assert.That(successfulTrySubmitViewModel.TrySubmitRequest(), Is.Null);
        }

        [Test]
        public void Setters_RaisePropertyChangedForBindableFields()
        {
            var viewModel = BuildViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.SelectedGame = BuildOtherUsersGame(888);
            viewModel.StartDate = DateTimeOffset.Now.AddDays(2);
            viewModel.EndDate = DateTimeOffset.Now.AddDays(10);

            Assert.That(changedProperties, Is.EqualTo(new[]
            {
                nameof(viewModel.SelectedGame),
                nameof(viewModel.StartDate),
                nameof(viewModel.EndDate)
            }));
        }

        private static void AssertInvalidRequestInputs(CreateRequestViewModel viewModel, Action<CreateRequestViewModel> invalidate)
        {
            PopulateWithValidSelections(viewModel);
            invalidate(viewModel);
            Assert.That(viewModel.ValidateRequestInputs(), Is.False);
        }

        private static void PopulateWithValidSelections(CreateRequestViewModel viewModel)
        {
            viewModel.SelectedGame = BuildOtherUsersGame(AvailableGameId);
            viewModel.StartDate = DateTimeOffset.Now.AddDays(1);
            viewModel.EndDate = DateTimeOffset.Now.AddDays(7);
        }

        private static GameDTO BuildOtherUsersGame(int gameId)
        {
            return new GameDTO
            {
                Id = gameId,
                Owner = new UserDTO { Id = OtherOwnerId },
                Name = "Board Game " + gameId,
                Price = 12m,
                IsActive = true
            };
        }
    }
}
