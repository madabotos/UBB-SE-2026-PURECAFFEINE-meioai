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
    // Osherove-style mocked unit tests for RequestService.
    // Contract expectation (Agent 1): fallible operations return Result<int, TError>
    // and error enums use PascalCase (OwnerCannotRent, DatesUnavailable, etc.).
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
        private Mock<IMapper<Request, RequestDataTransferObject>> requestMapperMock = null!;
        private RequestService requestService = null!;

        [SetUp]
        public void SetUp()
        {
            requestRepositoryMock = new Mock<IRequestRepository>();
            rentalRepositoryMock = new Mock<IRentalRepository>();
            gameRepositoryMock = new Mock<IGameRepository>();
            notificationServiceMock = new Mock<INotificationService>();
            requestMapperMock = new Mock<IMapper<Request, RequestDataTransferObject>>();

            gameRepositoryMock
                .Setup(repository => repository.Get(It.IsAny<int>()))
                .Returns<int>(identifier => BuildGame(identifier));
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
            // arrange
            var sharedUserIdentifier = SampleRenterIdentifier;

            // act
            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                sharedUserIdentifier,
                sharedUserIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            // assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(CreateRequestError.OwnerCannotRent);
        }

        [Test]
        public void CreateRequest_GameNotFound_ReturnsFailureGameDoesNotExist()
        {
            // arrange
            gameRepositoryMock
                .Setup(repository => repository.Get(SampleGameIdentifier))
                .Throws<KeyNotFoundException>();

            // act
            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            // assert
            result.Error.Should().Be(CreateRequestError.GameDoesNotExist);
        }

        [Test]
        public void CreateRequest_DatesUnavailable_ReturnsFailureDatesUnavailable()
        {
            // arrange — existing rental overlaps the requested window
            var overlappingRental = new Rental(
                identifier: 1,
                game: BuildGame(),
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(5));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRental));

            // act
            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            // assert
            result.Error.Should().Be(CreateRequestError.DatesUnavailable);
        }

        [Test]
        public void CreateRequest_HappyPath_ReturnsSuccessAndPersistsRequest()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Add(It.IsAny<Request>()))
                .Callback<Request>(added => added.Identifier = SampleRequestIdentifier);

            // act
            var result = requestService.CreateRequest(
                SampleGameIdentifier,
                SampleRenterIdentifier,
                SampleOwnerIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            // assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRequestIdentifier);
            requestRepositoryMock.Verify(repository => repository.Add(It.IsAny<Request>()), Times.Once);
        }

        [Test]
        public void ApproveRequest_NotFound_ReturnsFailureNotFound()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Throws<KeyNotFoundException>();

            // act
            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            // assert
            result.Error.Should().Be(ApproveRequestError.NotFound);
        }

        [Test]
        public void ApproveRequest_WrongOwner_ReturnsFailureUnauthorized()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            // act
            var result = requestService.ApproveRequest(SampleRequestIdentifier, UnrelatedUserIdentifier);

            // assert
            result.Error.Should().Be(ApproveRequestError.Unauthorized);
        }

        [Test]
        public void ApproveRequest_TransactionThrows_ReturnsFailureTransactionFailed()
        {
            // arrange
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

            // act
            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            // assert
            result.Error.Should().Be(ApproveRequestError.TransactionFailed);
        }

        [Test]
        public void ApproveRequest_HappyPath_ReturnsRentalIdAndSendsNotifications()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());
            var overlappingRequest = BuildRequest(identifier: 200, renterIdentifier: UnrelatedUserIdentifier);
            requestRepositoryMock
                .Setup(repository => repository.GetOverlappingRequests(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ImmutableList.Create(overlappingRequest));
            requestRepositoryMock
                .Setup(repository => repository.ApproveAtomically(
                    It.IsAny<Request>(), It.IsAny<ImmutableList<Request>>()))
                .Returns(SampleRentalIdentifier);

            // act
            var result = requestService.ApproveRequest(SampleRequestIdentifier, SampleOwnerIdentifier);

            // assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRentalIdentifier);
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    UnrelatedUserIdentifier, It.IsAny<NotificationDataTransferObject>()),
                Times.AtLeastOnce);
            notificationServiceMock.Verify(
                service => service.ScheduleUpcomingRentalReminder(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [Test]
        public void DenyRequest_NotFound_ReturnsFailureNotFound()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Throws<KeyNotFoundException>();

            // act
            var result = requestService.DenyRequest(
                SampleRequestIdentifier, SampleOwnerIdentifier, "not available");

            // assert
            result.Error.Should().Be(DenyRequestError.NotFound);
        }

        [Test]
        public void DenyRequest_HappyPath_DeletesRequestAndSendsDeclineNotification()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            // act
            var result = requestService.DenyRequest(
                SampleRequestIdentifier, SampleOwnerIdentifier, "not available");

            // assert
            result.IsSuccess.Should().BeTrue();
            requestRepositoryMock.Verify(
                repository => repository.Delete(SampleRequestIdentifier), Times.Once);
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    SampleRenterIdentifier, It.IsAny<NotificationDataTransferObject>()),
                Times.Once);
        }

        [Test]
        public void CancelRequest_HappyPath_DeletesNotificationsAndRequest()
        {
            // arrange — caller must be the renter; service also deletes notifications and the request.
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            // act
            var result = requestService.CancelRequest(SampleRequestIdentifier, SampleRenterIdentifier);

            // assert
            result.Should().Be(SampleRequestIdentifier);
            notificationServiceMock.Verify(
                service => service.DeleteNotificationsByRequestId(SampleRequestIdentifier), Times.Once);
            requestRepositoryMock.Verify(
                repository => repository.Delete(SampleRequestIdentifier), Times.Once);
        }

        [Test]
        public void OnGameDeactivated_CancelsAllPendingAndNotifiesRenters()
        {
            // arrange
            var pendingRequest = BuildRequest();
            var pendingRequestOther = BuildRequest(identifier: 200);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(pendingRequest, pendingRequestOther));

            // act
            requestService.OnGameDeactivated(SampleGameIdentifier);

            // assert
            requestRepositoryMock.Verify(repository => repository.Delete(It.IsAny<int>()), Times.Exactly(2));
            notificationServiceMock.Verify(
                service => service.SendNotificationToUser(
                    SampleRenterIdentifier, It.IsAny<NotificationDataTransferObject>()),
                Times.Exactly(2));
        }

        [Test]
        public void CheckAvailability_ExistingRentalOverlaps_ReturnsFalse()
        {
            // arrange
            var overlappingRental = new Rental(
                identifier: 1,
                game: BuildGame(),
                renter: new User(SampleRenterIdentifier, "Renter"),
                owner: new User(SampleOwnerIdentifier, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(5));
            rentalRepositoryMock
                .Setup(repository => repository.GetRentalsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRental));

            // act
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            // assert
            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_ExistingRequestOverlaps_ReturnsFalse()
        {
            // arrange
            var overlappingRequest = BuildRequest(
                identifier: 11, renterIdentifier: UnrelatedUserIdentifier);
            overlappingRequest.StartDate = DateTime.UtcNow.AddDays(2);
            overlappingRequest.EndDate = DateTime.UtcNow.AddDays(5);
            requestRepositoryMock
                .Setup(repository => repository.GetRequestsByGame(SampleGameIdentifier))
                .Returns(ImmutableList.Create(overlappingRequest));

            // act
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(6));

            // assert
            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_DateBeyondOneMonth_ReturnsFalse()
        {
            // arrange — nothing to prepare, fails before touching repositories

            // act
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddMonths(2),
                DateTime.UtcNow.AddMonths(2).AddDays(1));

            // assert
            isAvailable.Should().BeFalse();
        }

        [Test]
        public void CheckAvailability_HappyPath_ReturnsTrue()
        {
            // arrange — no rentals, no requests, game is active (default)

            // act
            var isAvailable = requestService.CheckAvailability(
                SampleGameIdentifier,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            // assert
            isAvailable.Should().BeTrue();
        }

        [Test]
        public void OfferGame_NotOwner_ReturnsFailureNotOwner()
        {
            // arrange
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(BuildRequest());

            // act
            var result = requestService.OfferGame(SampleRequestIdentifier, UnrelatedUserIdentifier);

            // assert
            result.Error.Should().Be(OfferError.NotOwner);
        }

        [Test]
        public void OfferGame_RequestNotOpen_ReturnsFailureRequestNotOpen()
        {
            // arrange
            var request = BuildRequest(status: RequestStatus.OfferPending);
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(request);

            // act
            var result = requestService.OfferGame(SampleRequestIdentifier, SampleOwnerIdentifier);

            // assert
            result.Error.Should().Be(OfferError.RequestNotOpen);
        }

        [Test]
        public void ApproveOffer_HappyPath_ReturnsRentalId()
        {
            // arrange
            var request = BuildRequest(status: RequestStatus.OfferPending);
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

            // act
            var result = requestService.ApproveOffer(SampleRequestIdentifier, SampleRenterIdentifier);

            // assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(SampleRentalIdentifier);
        }

        [Test]
        public void DenyOffer_HappyPath_ResetsRequestToOpen()
        {
            // arrange
            var request = BuildRequest(status: RequestStatus.OfferPending);
            requestRepositoryMock
                .Setup(repository => repository.Get(SampleRequestIdentifier))
                .Returns(request);

            // act
            var result = requestService.DenyOffer(SampleRequestIdentifier, SampleRenterIdentifier);

            // assert
            result.IsSuccess.Should().BeTrue();
            requestRepositoryMock.Verify(
                repository => repository.UpdateStatus(
                    SampleRequestIdentifier, RequestStatus.Open, null),
                Times.Once);
        }

        private static Game BuildGame(int identifier = SampleGameIdentifier, bool isActive = true)
        {
            return new Game
            {
                Identifier = identifier,
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
            int identifier = SampleRequestIdentifier,
            int renterIdentifier = SampleRenterIdentifier,
            int ownerIdentifier = SampleOwnerIdentifier,
            RequestStatus status = RequestStatus.Open)
        {
            return new Request
            {
                Identifier = identifier,
                Game = BuildGame(),
                Renter = new User(renterIdentifier, "Renter"),
                Owner = new User(ownerIdentifier, "Owner"),
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = status,
            };
        }
    }
}
