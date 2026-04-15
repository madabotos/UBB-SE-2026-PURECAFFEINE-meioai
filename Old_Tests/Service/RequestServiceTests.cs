using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Service;

namespace Property_and_Management.Tests.Service
{
    [TestFixture]
    public sealed class RequestServiceTests
    {
        private const int SampleGameIdentifier = 10;
        private const int SampleRenterIdentifier = 1;
        private const int SampleOwnerIdentifier = 2;
        private const int UnrelatedUserIdentifier = 999;
        private const int SampleRequestIdentifier = 100;
        private const int SampleRentalIdentifier = 500;

        private Mock<IRequestRepository> requestRepositoryMock = null!;
        private Mock<IRentalRepository> rentalRepositoryMock = null!;
        private Mock<IGameRepository> gameRepositoryMock = null!;
        private Mock<INotificationService> notificationServiceMock = null!;
        private Mock<IMapper<Request, RequestDTO>> requestMapperMock = null!;
        private RequestService requestService = null!;

        [SetUp]
        public void SetUp()
        {
            requestRepositoryMock = new Mock<IRequestRepository>();
            rentalRepositoryMock = new Mock<IRentalRepository>();
            gameRepositoryMock = new Mock<IGameRepository>();
            notificationServiceMock = new Mock<INotificationService>();
            requestMapperMock = new Mock<IMapper<Request, RequestDTO>>();

            gameRepositoryMock
                .Setup(repository => repository.Get(It.IsAny<int>()))
                .Returns<int>(id => BuildGame(id));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(It.IsAny<int>()))
                .Returns(ImmutableList<Rental>.Empty);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(It.IsAny<int>()))
                .Returns(ImmutableList<Request>.Empty);

            requestService = new RequestService(
                requestRepositoryMock.Object,
                rentalRepositoryMock.Object,
                gameRepositoryMock.Object,
                notificationServiceMock.Object,
                requestMapperMock.Object);
        }

        [Test]
        public void CreateRequest_RenterEqualsOwner_ReturnsFailureOwnerCannotRent()
        {
            var sharedUserIdentifier = SampleRenterIdentifier;

            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                sharedUserIdentifier,
                sharedUserIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(CreateRequestError.OwnerCannotRent);
        }

        [Test]
        public void CreateRequest_GameNotFound_ReturnsFailureGameDoesNotExist()
        {
            gameRepositoryMock
                .Setup(repository => repository.Get(SampleGameIdentifier))
                .Throws<KeyNotFoundException>();

            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            result.Error.Should().Be(CreateRequestError.GameDoesNotExist);
        }

        [Test]
        public void CreateRequest_DatesUnavailable_ReturnsFailureDatesUnavailable()
        {
            var overlappingRental = new Rental(
                id: 1,
                game: BuildGame(),
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(5));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRental));

            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            result.Error.Should().Be(CreateRequestError.DatesUnavailable);
        }

        [Test]
        public void CreateRequest_HappyPath_ReturnsSuccessAndPersistsRequest()
        {
            requestRepositoryMock
                .Setup(repository => repository.Add(It.IsAny<Request>()))
                .Callback<Request>(added => added.id = SampleRequestIdentifier);

            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRequestIdentifier);
            requestRepositoryMock.Verify(repository => repository.Add(It.IsAny<Request>()), Times.Once);
        }

        [Test]
        public void ApproveRequest_NotFound_ReturnsFailureNotFound()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Throws<KeyNotFoundException>();

            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.Error.Should().Be(ApproveRequestError.NotFound);
        }

        [Test]
        public void ApproveRequest_WrongOwner_ReturnsFailureUnauthorized()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            var result = requestService.ApproveRequest(SampleRequestIdentifier, UnrelatedUserIdentifier);

            result.Error.Should().Be(ApproveRequestError.Unauthorized);
        }

        [Test]
        public void ApproveRequest_TransactionThrows_ReturnsFailureTransactionFailed()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());
            requestRepositoryMock
                .Setup(repository => repository.GetOverlappingRequests(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ImmutableList<Request>.Empty);
            requestRepositoryMock
                .Setup(repository => repository.ApproveAtomically(
                    It.IsAny<Request>(), It.IsAny<ImmutableList<Request>>()))
                .Throws(new InvalidOperationException("boom"));

            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.Error.Should().Be(ApproveRequestError.TransactionFailed);
        }

        [Test]
        public void ApproveRequest_HappyPath_ReturnsRentalIdAndSendsNotifications()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());
            var overlappingRequest = BuildRequest(id: 200, renterId: UnrelatedUserIdentifier);
            requestRepositoryMock
                .Setup(repository => repository.GetOverlappingRequests(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ImmutableList.Create(overlappingRequest));
            requestRepositoryMock
                .Setup(repository => repository.ApproveAtomically(
                    It.IsAny<Request>(), It.IsAny<ImmutableList<Request>>()))
                .Returns(SampleRentalIdentifier);

            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRentalIdentifier);
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    UnrelatedUserIdentifier, It.IsAny<NotificationDTO>()),
                Times.AtLeastOnce);
            notificationServiceMock.Verify(
                service => service.ScheduleUpcomingRentalReminder(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [Test]
        public void DenyRequest_NotFound_ReturnsFailureNotFound()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Throws<KeyNotFoundException>();

            var result = requestService.DenyRequest(
                SampleRequestIdentifier, SampleOwnerIdentifier, "not available");

            result.Error.Should().Be(DenyRequestError.NotFound);
        }

        [Test]
        public void DenyRequest_HappyPath_DeletesRequestAndSendsDeclineNotification()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            var result = requestService.DenyRequest(
                SampleRequestIdentifier, SampleOwnerIdentifier, "not available");

            result.IsSuccess.Should().BeTrue();
            requestRepositoryMock.Verify(
                repository => repository.Delete(SampleRequestIdentifier), Times.Once);
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    SampleRenterIdentifier, It.IsAny<NotificationDTO>()),
                Times.Once);
        }

        [Test]
        public void CancelRequest_HappyPath_DeletesNotificationsAndRequest()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            var result = requestService.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier);

            result.Should().Be(SampleRequestIdentifier);
            notificationServiceMock.Verify(
                service => service.DeleteNotificationsLinkedToRequest(SampleRequestIdentifier), Times.Once);
            requestRepositoryMock.Verify(
                repository => repository.Delete(SampleRequestIdentifier), Times.Once);
        }

        [Test]
        public void OnGameDeactivated_CancelsAllPendingAndNotifiesRenters()
        {
            var pendingRequest = BuildRequest();
            var pendingRequestOther = BuildRequest(id: 200);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(pendingRequest, pendingRequestOther));

            requestService.OnGameDeactivated(SampleGameIdentifier);

            requestRepositoryMock.Verify(repository => repository.Delete(It.IsAny<int>()), Times.Exactly(2));
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    SampleRenterIdentifier, It.IsAny<NotificationDTO>()),
                Times.Exactly(2));
        }

        [Test]
        public void OnGameDeactivated_CancelsOfferPendingRequests()
        {
            var openRequest = BuildRequest(id: 101, status: RequestStatus.Open);
            var offerPendingRequest = BuildRequest(id: 102, status: RequestStatus.OfferPending);
            var acceptedRequest = BuildRequest(id: 103, status: RequestStatus.Accepted);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(openRequest, offerPendingRequest, acceptedRequest));

            requestService.OnGameDeactivated(SampleGameIdentifier);

            requestRepositoryMock.Verify(repository => repository.Delete(101), Times.Once);
            requestRepositoryMock.Verify(repository => repository.Delete(102), Times.Once);
            requestRepositoryMock.Verify(repository => repository.Delete(103), Times.Never);
            notificationServiceMock.Verify(
                service => service.DeleteNotificationsLinkedToRequest(102),
                Times.Once);
        }

        [Test]
        public void CheckAvailability_ExistingRentalOverlaps_ReturnsFalse()
        {
            var overlappingRental = new Rental(
                id: 1,
                game: BuildGame(),
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(5));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRental));

            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_ExistingRequestOverlaps_ReturnsFalse()
        {
            var overlappingRequest = BuildRequest(
                id: 11, renterId: UnrelatedUserIdentifier);
            overlappingRequest.StartDate = DateTime.UtcNow.AddDays(2);
            overlappingRequest.EndDate = DateTime.UtcNow.AddDays(5);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRequest));

            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_DateBeyondOneMonth_ReturnsFalse()
        {
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddMonths(2),
                DateTime.UtcNow.AddMonths(2).AddDays(1));

            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_HappyPath_ReturnsTrue()
        {
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            isAvailable.Should().BeTrue();
        }

        [Test]
        public void OfferGame_NotOwner_ReturnsFailureNotOwner()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            var result = requestService.OfferGame(SampleRequestIdentifier, UnrelatedUserIdentifier);

            result.Error.Should().Be(OfferError.NotOwner);
        }

        [Test]
        public void OfferGame_RequestNotOpen_ReturnsFailureRequestNotOpen()
        {
            var request = BuildRequest(status: RequestStatus.OfferPending);
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(request);

            var result = requestService.OfferGame(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.Error.Should().Be(OfferError.RequestNotOpen);
        }

        [Test]
        public void OfferGame_HappyPath_ReturnsRentalId()
        {
            var request = BuildRequest();
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(request);
            requestRepositoryMock
                .Setup(repository => repository.GetOverlappingRequests(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ImmutableList<Request>.Empty);
            requestRepositoryMock
                .Setup(repository => repository.ApproveAtomically(
                    It.IsAny<Request>(), It.IsAny<ImmutableList<Request>>()))
                .Returns(SampleRentalIdentifier);

            var result = requestService.OfferGame(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRentalIdentifier);
        }

        [Test]
        public void OfferGame_TransactionThrows_ReturnsFailureTransactionFailed()
        {
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());
            requestRepositoryMock
                .Setup(repository => repository.GetOverlappingRequests(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ImmutableList<Request>.Empty);
            requestRepositoryMock
                .Setup(repository => repository.ApproveAtomically(
                    It.IsAny<Request>(), It.IsAny<ImmutableList<Request>>()))
                .Throws(new InvalidOperationException("boom"));

            var result = requestService.OfferGame(SampleRequestIdentifier, SampleOwnerIdentifier);

            result.Error.Should().Be(OfferError.TransactionFailed);
        }

        private static Game BuildGame(int id = SampleGameIdentifier, bool isActive = true)
        {
            return new Game
            {
                id = id,
                Owner = new User(SampleOwnerIdentifier, "Owner"),
                Name = "Some Game",
                Price = 10m,
                MinimumPlayerNumber = 2,
                MaximumPlayerNumber = 4,
                Description = "Description",
                Image = Array.Empty<byte>(),
                IsActive = isActive,
            };
        }

        private static Request BuildRequest(
            int id = SampleRequestIdentifier,
            int renterId = SampleRenterIdentifier,
            int ownerId = SampleOwnerIdentifier,
            RequestStatus status = RequestStatus.Open)
        {
            return new Request
            {
                id = id,
                Game = BuildGame(),
                Renter = new User(renterId, "Renter"),
                Owner = new User(ownerId, "Owner"),
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = status,
            };
        }
    }
}