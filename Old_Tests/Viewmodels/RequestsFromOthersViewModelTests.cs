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
    public sealed class RequestsFromOthersViewModelTests
    {
        private const int SampleOwnerIdentifier = 1;
        private const int SampleRequestIdentifier = 42;

        private Mock<IRequestService> requestServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private RequestsFromOthersViewModel viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            requestServiceMock = new Mock<IRequestService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(SampleOwnerIdentifier);
            requestServiceMock
                .Setup(service => service.GetRequestsForOwner(SampleOwnerIdentifier))
                .Returns(ImmutableList<RequestDataTransferObject>.Empty);

            viewModel = new RequestsFromOthersViewModel(
                requestServiceMock.Object, currentUserContextMock.Object);
        }

        [Test]
        public void TryApproveRequest_HappyPath_ReturnsNullAndReloadsCurrentPage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier))
                .Returns(Result<int, ApproveRequestError>.Success(500));
            requestServiceMock.Invocations.Clear();

            // act
            var errorMessage = viewModel.TryApproveRequest(SampleRequestIdentifier);

            // assert
            errorMessage.Should().BeNull();
            requestServiceMock.Verify(
                service => service.GetRequestsForOwner(SampleOwnerIdentifier),
                Times.AtLeastOnce);
        }

        [Test]
        public void TryDenyRequest_Unauthorized_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.DenyRequest(
                    SampleRequestIdentifier, SampleOwnerIdentifier, It.IsAny<string>()))
                .Returns(Result<int, DenyRequestError>.Failure(DenyRequestError.Unauthorized));

            // act
            var errorMessage = viewModel.TryDenyRequest(SampleRequestIdentifier, "unavailable");

            // assert
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public void TryOfferGame_RequestNotOpen_ReturnsFriendlyMessage()
        {
            // arrange
            requestServiceMock
                .Setup(service => service.OfferGame(SampleRequestIdentifier, SampleOwnerIdentifier))
                .Returns(Result<int, OfferError>.Failure(OfferError.RequestNotOpen));

            // act
            var errorMessage = viewModel.TryOfferGame(SampleRequestIdentifier);

            // assert
            errorMessage.Should().NotBeNull();
        }
    }
}
