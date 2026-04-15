using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
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
    public sealed class NotificationServiceTests
    {
        private const int currentUserId = 1;
        private const int OtherUserIdentifier = 2;
        private const int SampleNotificationIdentifier = 42;

        private Mock<INotificationRepository> notificationRepositoryMock = null!;
        private Mock<IMapper<Notification, NotificationDTO>> notificationMapperMock = null!;
        private Mock<IServerClient> serverClientMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private Mock<IToastNotificationService> toastNotificationServiceMock = null!;
        private NotificationService notificationService = null!;

        [SetUp]
        public void SetUp()
        {
            notificationRepositoryMock = new Mock<INotificationRepository>();
            notificationMapperMock = new Mock<IMapper<Notification, NotificationDTO>>();
            serverClientMock = new Mock<IServerClient>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            toastNotificationServiceMock = new Mock<IToastNotificationService>();

            currentUserContextMock
                .SetupGet(context => context.currentUserId)
                .Returns(currentUserId);
            notificationRepositoryMock
                .Setup(repository => repository.Add(It.IsAny<Notification>()))
                .Callback<Notification>(added => added.id = SampleNotificationIdentifier);

            notificationService = new NotificationService(
                notificationRepositoryMock.Object,
                notificationMapperMock.Object,
                serverClientMock.Object,
                currentUserContextMock.Object,
                toastNotificationServiceMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            notificationService?.Dispose();
        }

        [Test]
        public void SendNotificationToUser_PersistsAndForwardsToServerClient()
        {
            var notification = new NotificationDTO
            {
                User = new UserDTO { id = OtherUserIdentifier },
                Title = "Title",
                Body = "Body",
            };

            notificationService.SendNotificationToUser(OtherUserIdentifier, notification);

            notificationRepositoryMock.Verify(
                repository => repository.Add(It.IsAny<Notification>()), Times.Once);
            serverClientMock.Verify(
                client => client.SendNotification(OtherUserIdentifier, "Title", "Body"), Times.Once);
        }

        [Test]
        public void SendNotificationToUser_WhenUserIsCurrent_NotifiesLocalSubscribers()
        {
            var subscriberMock = new Mock<IObserver<NotificationDTO>>();
            notificationService.Subscribe(subscriberMock.Object);

            var notification = new NotificationDTO
            {
                User = new UserDTO { id = currentUserId },
                Title = "Title",
                Body = "Body",
            };

            notificationService.SendNotificationToUser(currentUserId, notification);

            subscriberMock.Verify(
                observer => observer.OnNext(It.IsAny<NotificationDTO>()),
                Times.Once);
        }

        [Test]
        public void OnNext_IncomingNotification_NotifiesSubscribersAndShowsToast()
        {
            var subscriberMock = new Mock<IObserver<NotificationDTO>>();
            notificationService.Subscribe(subscriberMock.Object);

            var incomingNotification = new IncomingNotification
            {
                userId = currentUserId,
                Timestamp = DateTime.UtcNow,
                Title = "Incoming",
                Body = "Body",
            };

            notificationService.OnNext(incomingNotification);

            subscriberMock.Verify(
                observer => observer.OnNext(It.IsAny<NotificationDTO>()),
                Times.Once);
            toastNotificationServiceMock.Verify(
                toast => toast.Show("Incoming", "Body"), Times.Once);
        }

        [Test]
        public void ScheduleUpcomingRentalReminder_ReminderTimeAlreadyDue_SendsImmediately()
        {
            var soonStartDate = DateTime.UtcNow.AddHours(1);

            notificationService.ScheduleUpcomingRentalReminder(
                currentUserId, OtherUserIdentifier, "Catan", soonStartDate);

            notificationRepositoryMock.Verify(
                repository => repository.Add(It.IsAny<Notification>()), Times.AtLeast(2));
        }

        [Test]
        public void DeleteNotificationsLinkedToRequest_DelegatesToRepository()
        {
            const int relatedRequestId = 77;

            notificationService.DeleteNotificationsLinkedToRequest(relatedRequestId);

            notificationRepositoryMock.Verify(
                repository => repository.DeleteNotificationsLinkedToRequest(relatedRequestId), Times.Once);
        }

        [Test]
        public void Subscribers_ThreadSafe_MultipleConcurrentSubscribeCallsDoNotThrow()
        {
            const int ConcurrentSubscribers = 16;
            var tasks = new Task[ConcurrentSubscribers];

            for (var index = 0; index < ConcurrentSubscribers; index++)
            {
                tasks[index] = Task.Run(() =>
                    notificationService.Subscribe(Mock.Of<IObserver<NotificationDTO>>()));
            }

            var subscribeAction = () => Task.WaitAll(tasks);

            subscribeAction.Should().NotThrow();
        }
    }
}