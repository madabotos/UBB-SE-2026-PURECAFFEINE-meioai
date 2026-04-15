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
                .SetupGet(context => context.currentUserId)
                .Returns(SampleRenterIdentifier);
            requestServiceMock
                .Setup(service => service.GetRequestsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList<RequestDTO>.Empty);

            viewModel = new RequestsToOthersViewModel(requestServiceMock.Object, currentUserContextMock.Object);
        }

        [Test]
        public void Constructor_LoadsRequestsForCurrentRenter()
        {
            requestServiceMock
                .Setup(service => service.GetRequestsForRenter(SampleRenterIdentifier))
                .Returns(ImmutableList.Create(
                    BuildRequest(id: 1),
                    BuildRequest(id: 2)));
            var freshViewModel = new RequestsToOthersViewModel(requestServiceMock.Object, currentUserContextMock.Object);

            var total = freshViewModel.TotalCount;

            total.Should().Be(2);
            freshViewModel.renterId.Should().Be(SampleRenterIdentifier);
        }

        [Test]
        public void TryCancelRequest_HappyPath_ReturnsNullAndReloads()
        {
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns(SampleRequestIdentifier);
            requestServiceMock.Invocations.Clear();

            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            errorMessage.Should().BeNull();
            requestServiceMock.Verify(
                service => service.GetRequestsForRenter(SampleRenterIdentifier),
                Times.AtLeastOnce);
        }

        [Test]
        public void TryCancelRequest_NotFound_ReturnsFriendlyMessage()
        {
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns((int)CancelRequestError.NotFound);

            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            errorMessage.Should().NotBeNull();
            errorMessage!.ToLowerInvariant().Should().Contain("not found");
        }

        [Test]
        public void TryCancelRequest_Unauthorized_ReturnsFriendlyMessage()
        {
            requestServiceMock
                .Setup(service => service.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier))
                .Returns((int)CancelRequestError.Unauthorized);

            var errorMessage = viewModel.TryCancelRequest(SampleRequestIdentifier);

            errorMessage.Should().NotBeNull();
        }

        [Test]
        public void ShowingText_UsesRequestsVocabulary()
        {
            var text = viewModel.ShowingText;

            text.Should().Contain("requests");
        }

        private static RequestDTO BuildRequest(int id)
        {
            return new RequestDTO
            {
                id = id,
                Game = new GameDTO { id = 100 },
                Renter = new UserDTO { id = SampleRenterIdentifier },
                Owner = new UserDTO { id = 99 },
                StartDate = System.DateTime.UtcNow.AddDays(1),
                EndDate = System.DateTime.UtcNow.AddDays(3),
            };
        }
    }
}