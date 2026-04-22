using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.DataTransferObjects;
using System.Collections.Immutable;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class RequestsToOthersViewModelTests
    {
        [Test]
        public void LoadRequests_WithMultipleRequests_SetsRenterIdAndOrdersByStartDateDescending()
        {
            // set up dependencies
            var mockRequestService = new Mock<IRequestService>();
            var mockUserContext = new Mock<ICurrentUserContext>();

            var currentUserId = 5;
            mockUserContext.Setup(context => context.CurrentUserId).Returns(currentUserId);

            var request1 = new RequestDTO { Id = 10, StartDate = new DateTime(2025, 1, 1) };
            var request2 = new RequestDTO { Id = 11, StartDate = new DateTime(2025, 1, 5) };

            mockRequestService.Setup(service => service.GetRequestsForRenter(currentUserId))
                              .Returns(ImmutableList.Create(request1, request2));

            var viewModel = new RequestsToOthersViewModel(mockRequestService.Object, mockUserContext.Object);

            // run the method
            viewModel.LoadRequests();

            // assert
            Assert.That(viewModel.CurrentRenterUserId, Is.EqualTo(currentUserId));
            Assert.That(viewModel.PagedItems, Has.Count.EqualTo(2));
            Assert.That(viewModel.PagedItems[0].Id, Is.EqualTo(11));
            Assert.That(viewModel.PagedItems[1].Id, Is.EqualTo(10));
        }

        [Test]
        public void TryCancelRequest_WhenServiceSucceeds_ReturnsNull()
        {
            // set up dependencies
            var mockRequestService = new Mock<IRequestService>();
            var mockUserContext = new Mock<ICurrentUserContext>();

            var currentUserId = 5;
            mockUserContext.Setup(context => context.CurrentUserId).Returns(currentUserId);
            mockRequestService.Setup(service => service.GetRequestsForRenter(currentUserId))
                              .Returns(ImmutableList<RequestDTO>.Empty);

            var viewModel = new RequestsToOthersViewModel(mockRequestService.Object, mockUserContext.Object);

            var requestIdToCancel = 100;
            mockRequestService.Setup(service => service.CancelRequest(requestIdToCancel, currentUserId)).Returns(1);

<<<<<<< Updated upstream
            // run the method
            var result = viewModel.TryCancelRequest(requestIdToCancel);

            // assert
            Assert.That(result, Is.Null);
=======
           
            var cancellationErrorMessage = viewModel.TryCancelRequest(requestIdToCancel);

            
            Assert.That(cancellationErrorMessage, Is.Null);
>>>>>>> Stashed changes
        }

        [Test]
        public void TryCancelRequest_WhenRequestNotFound_ReturnsNotFoundErrorMessage()
        {
            // set up dependencies
            var mockRequestService = new Mock<IRequestService>();
            var mockUserContext = new Mock<ICurrentUserContext>();

            var currentUserId = 5;
            mockUserContext.Setup(context => context.CurrentUserId).Returns(currentUserId);
            mockRequestService.Setup(service => service.GetRequestsForRenter(currentUserId))
                              .Returns(ImmutableList<RequestDTO>.Empty);

            var viewModel = new RequestsToOthersViewModel(mockRequestService.Object, mockUserContext.Object);

            var requestIdToCancel = 100;
            mockRequestService.Setup(service => service.CancelRequest(requestIdToCancel, currentUserId)).Returns((int)CancelRequestError.NotFound);

<<<<<<< Updated upstream
            // run the method
            var result = viewModel.TryCancelRequest(requestIdToCancel);

            // assert
            Assert.That(result, Is.EqualTo("Request not found."));
=======
            
            var cancellationErroroMessage = viewModel.TryCancelRequest(requestIdToCancel);

           
            Assert.That(cancellationErroroMessage, Is.EqualTo("Request not found."));
>>>>>>> Stashed changes
        }
    }
}
