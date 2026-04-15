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
    public sealed class RequestsToOthersViewModelTests
    {
        private const int SampleRenterIdentifier = 1;
        private const int SampleRequestIdentifier = 42;

        private Mock<IRequestService> requestServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private RequestsToOthersViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            requestServiceMock = new Mock<IRequestService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleRenterIdentifier);
            requestServiceMock
                .Setup(service => service.GetRequestsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList<RequestDataTransferObject>.Empty);

            viewModel = new RequestsToOthersViewModel(requestServiceMock.Object, currentUserContextMock.Object);
        }

        [Test]
        public void Constructor_LoadsRequestsForCurrentRenter()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.GetRequestsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList.Create(
                    BuildRequest(identifier: 1),
                    BuildRequest(identifier: 2)));
            var freshViewModel = new RequestsToOthersViewModel(requestServiceMock.Object, currentUserContextMock.Object);

            // act
            var total = freshViewModel.TotalCount;

            // assert
            total.Should().Be(2);
            freshViewModel.RenterIdentifier.Should().Be(SampleRenterIdentifier);
        }

        [Test]
        public void TryCancelRequest_HappyPath_ReturnsNullAndReloads()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns(SampleRequestIdentifier);
            requestServiceMock.Invocations.Clear();

            // act
            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            // assert
            errorMessage.Should().BeNull();
            requestServiceMock.Verify(
                service => service.GetRequestsForRenter(SampleRenterIdentifier),
                Times.AtLeastOnce);
        }

        [Test]
        public void TryCancelRequest_NotFound_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns((int)CancelRequestError.NotFound);

            // act
            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            // assert
            errorMessage.Should().NotBeNull();
            errorMessage!.ToLowerInvariant().Should().Contain("not found");
        }

        [Test]
        public void TryCancelRequest_Unauthorized_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns((int)CancelRequestError.Unauthorized);

            // act
            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            // assert
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public void ShowingText_UsesRequestsVocabulary()
        {
            // arrange — constructor-loaded viewModel

            // act
            var text = viewModel.ShowingText;

            // assert
            text.Should().Contain("requests");
        }

        private static RequestDataTransferObject BuildRequest(int identifier)
        {
            return new RequestDataTransferObject
            {
                Identifier = identifier,
                Game = new GameDataTransferObject { Identifier = 100 },
                Renter = new UserDataTransferObject { Identifier = SampleRenterIdentifier },
                Owner = new UserDataTransferObject { Identifier = 99 },
                StartDate = System.DateTime.UtcNow.AddDays(1),
                EndDate = System.DateTime.UtcNow.AddDays(3),
            };
        }
    }
}
