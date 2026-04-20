// Tudor
using System;
using System.Collections.Immutable;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Service;

namespace Property_and_Management.Tests.Service
{
    [TestFixture]
    public class RequestServiceTests
    {
        private Mock<IRequestRepository> requestRepo;
        private Mock<IRentalRepository> rentalRepo;
        private Mock<IGameRepository> gameRepo;
        private Mock<INotificationService> notifications;
        private Mock<IMapper<Request, RequestDTO>> mapper;
        private RequestService service;

        [SetUp]
        public void Setup()
        {
            requestRepo = new Mock<IRequestRepository>();
            rentalRepo = new Mock<IRentalRepository>();
            gameRepo = new Mock<IGameRepository>();
            notifications = new Mock<INotificationService>();
            mapper = new Mock<IMapper<Request, RequestDTO>>();

            service = new RequestService(
                requestRepo.Object,
                rentalRepo.Object,
                gameRepo.Object,
                notifications.Object,
                mapper.Object);
        }

        [Test]
        public void CreateRequest_WhenRenterIsOwner_ReturnsOwnerCannotRent()
        {
            var result = service.CreateRequest(
                gameId: 10,
                renterUserId: 1,
                ownerUserId: 1,
                proposedStartDate: DateTime.UtcNow.AddDays(2),
                proposedEndDate: DateTime.UtcNow.AddDays(4));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(CreateRequestError.OwnerCannotRent));
        }

        [Test]
        public void CancelRequest_AsRenter_DeletesRequestAndNotifications()
        {
            var existing = new Request
            {
                Id = 100,
                Renter = new User(1, "Renter"),
                Owner = new User(2, "Owner"),
                Game = new Game { Id = 10, Name = "Game" },
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(5),
                Status = RequestStatus.Open
            };
            requestRepo.Setup(r => r.Get(100)).Returns(existing);

            var result = service.CancelRequest(100, 1);

            Assert.That(result, Is.EqualTo(100));
            requestRepo.Verify(r => r.Delete(100), Times.Once);
            notifications.Verify(n => n.DeleteNotificationsLinkedToRequest(100), Times.Once);
        }
    }
}
