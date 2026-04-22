// Tudor
using System;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Service;

namespace Property_and_Management.Tests.Service
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> notificationRepo;
        private Mock<IMapper<Notification, NotificationDTO>> mapper;
        private Mock<IServerClient> serverClient;
        private Mock<ICurrentUserContext> userContext;
        private Mock<IToastNotificationService> toast;
        private NotificationService service;

        [SetUp]
        public void Setup()
        {
            notificationRepo = new Mock<INotificationRepository>();
            mapper = new Mock<IMapper<Notification, NotificationDTO>>();
            serverClient = new Mock<IServerClient>();
            userContext = new Mock<ICurrentUserContext>();
            toast = new Mock<IToastNotificationService>();

            userContext.SetupGet(c => c.CurrentUserId).Returns(1);
            serverClient
                .Setup(c => c.Subscribe(It.IsAny<IObserver<IncomingNotification>>()))
                .Returns(Mock.Of<IDisposable>());

            service = new NotificationService(
                notificationRepo.Object,
                mapper.Object,
                serverClient.Object,
                userContext.Object,
                toast.Object);
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
        }

        [Test]
        public void SendNotificationToUser_SavesAndForwardsToServer()
        {
            var dto = new NotificationDTO
            {
                User = new UserDTO { Id = 2 },
                Title = "Hello",
                Body = "World"
            };

            service.SendNotificationToUser(2, dto);

            notificationRepo.Verify(r => r.Add(It.IsAny<Notification>()), Times.Once);
            serverClient.Verify(c => c.SendNotification(2, "Hello", "World"), Times.Once);
        }

        [Test]
        public void DeleteNotificationsLinkedToRequest_CallsRepository()
        {
            service.DeleteNotificationsLinkedToRequest(42);

            notificationRepo.Verify(r => r.DeleteNotificationsLinkedToRequest(42), Times.Once);
        }
    }
}
