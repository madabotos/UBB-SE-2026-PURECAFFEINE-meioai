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
    public sealed class CreateRequestViewModelTests
    {
        private const int SampleGameIdentifier = 10;
        private const int SampleOwnerIdentifier = 2;
        private const int SampleCurrentUserIdentifier = 1;

        private Mock<IGameService> gameServiceMock = null!;
        private Mock<IRequestService> requestServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private CreateRequestViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            gameServiceMock = new Mock<IGameService>();
            requestServiceMock = new Mock<IRequestService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();

            gameServiceMock
                .Setup(service => service.GetAllGames())
                .Returns(ImmutableList<GameDTO>.Empty);
            currentUserContextMock
                .SetupGet(context => context.currentUserId)
                .Returns(SampleCurrentUserIdentifier);

            viewModel = new CreateRequestViewModel(
                gameServiceMock.Object,
                requestServiceMock.Object,
                currentUserContextMock.Object)
            {
                SelectedGame = new GameDTO
                {
                    id = SampleGameIdentifier,
                    Owner = new UserDTO { id = SampleOwnerIdentifier },
                    IsActive = true,
                },
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(4),
            };
        }

        [Test]
        public void TrySubmitRequest_OwnerCannotRent_ReturnsFriendlyMessage()
        {
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.OwnerCannotRent));

            var errorMessage = viewModel.TrySubmitRequest();

            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("own");
        }

        [Test]
        public void TrySubmitRequest_DatesUnavailable_ReturnsFriendlyMessage()
        {
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.DatesUnavailable));

            var errorMessage = viewModel.TrySubmitRequest();

            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("dates");
        }

        [Test]
        public void TrySubmitRequest_GameDoesNotExist_ReturnsFriendlyMessage()
        {
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.GameDoesNotExist));

            var errorMessage = viewModel.TrySubmitRequest();

            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("game");
        }

        [Test]
        public void TrySubmitRequest_HappyPath_ReturnsNull()
        {
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Success(123));

            var errorMessage = viewModel.TrySubmitRequest();

            errorMessage.Should().BeNull();
        }
    }
}