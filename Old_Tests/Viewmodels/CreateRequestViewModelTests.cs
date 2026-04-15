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
    // These tests target the refactored CreateRequestViewModel that exposes a
    // TrySubmitRequest() method returning the user-facing error string (or null
    // on success). Agent 2 adds this method so the View can stop importing the
    // service namespace and stop doing its own error-code → message mapping.
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
                .Returns(ImmutableList<GameDataTransferObject>.Empty);
            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleCurrentUserIdentifier);

            viewModel = new CreateRequestViewModel(
                gameServiceMock.Object,
                requestServiceMock.Object,
                currentUserContextMock.Object)
            {
                SelectedGame = new GameDataTransferObject
                {
                    Identifier = SampleGameIdentifier,
                    Owner = new UserDataTransferObject { Identifier = SampleOwnerIdentifier },
                    IsActive = true,
                },
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(4),
            };
        }

        [Test]
        public void TrySubmitRequest_OwnerCannotRent_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.OwnerCannotRent));

            // act
            var errorMessage = viewModel.TrySubmitRequest();

            // assert
            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("own");
        }

        [Test]
        public void TrySubmitRequest_DatesUnavailable_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.DatesUnavailable));

            // act
            var errorMessage = viewModel.TrySubmitRequest();

            // assert
            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("dates");
        }

        [Test]
        public void TrySubmitRequest_GameDoesNotExist_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Failure(CreateRequestError.GameDoesNotExist));

            // act
            var errorMessage = viewModel.TrySubmitRequest();

            // assert
            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("game");
        }

        [Test]
        public void TrySubmitRequest_HappyPath_ReturnsNull()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CreateRequest(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Result<int, CreateRequestError>.Success(123));

            // act
            var errorMessage = viewModel.TrySubmitRequest();

            // assert
            errorMessage.Should().BeNull();
        }
    }
}
