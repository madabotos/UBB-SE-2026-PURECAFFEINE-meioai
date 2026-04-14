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
        private const int CurrentUserIdentifier = 1;
        private const int OtherUserIdentifier = 2;
        private const int SampleNotificationIdentifier = 42;

        private Mock<INotificationRepository> notificationRepositoryMock = null!;
        private Mock<IMapper<Notification, NotificationDataTransferObject>> notificationMapperMock = null!;
        private Mock<IServerClient> serverClientMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private Mock<IToastNotificationService> toastNotificationServiceMock = null!;
        private NotificationService notificationService = null!;

        [SetUp]
        public void SetUp()
        {
            notificationRepositoryMock = new Mock<INotificationRepository>();
            notificationMapperMock = new Mock<IMapper<Notification, NotificationDataTransferObject>>();
            serverClientMock = new Mock<IServerClient>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            toastNotificationServiceMock = new Mock<IToastNotificationService>();

            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(CurrentUserIdentifier);
            notificationRepositoryMock
                .Setup(repository => repository.Add(It.IsAny<Notification>()))
                .Callback<Notification>(added => added.Identifier = SampleNotificationIdentifier);

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
            // arrange
            var notification = new NotificationDataTransferObject
            {
                User = new UserDataTransferObject { Identifier = OtherUserIdentifier },
                Title = "Title",
                Body = "Body",
            };

            // act
            notificationService.SendNotificationToUser(OtherUserIdentifier, notification);

            // assert
            notificationRepositoryMock.Verify(
                repository => repository.Add(It.IsAny<Notification>()), Times.Once);
            serverClientMock.Verify(
                client => client.SendNotification(OtherUserIdentifier, "Title", "Body"), Times.Once);
        }

        [Test]
        public void SendNotificationToUser_WhenUserIsCurrent_NotifiesLocalSubscribers()
        {
            // arrange
            var subscriberMock = new Mock<IObserver<NotificationDataTransferObject>>();
            notificationService.Subscribe(subscriberMock.Object);

            var notification = new NotificationDataTransferObject
            {
                User = new UserDataTransferObject { Identifier = CurrentUserIdentifier },
                Title = "Title",
                Body = "Body",
            };

            // act
            notificationService.SendNotificationToUser(CurrentUserIdentifier, notification);

            // assert
            subscriberMock.Verify(
                observer => observer.OnNext(It.IsAny<NotificationDataTransferObject>()),
                Times.Once);
        }

        [Test]
        public void OnNext_IncomingNotification_NotifiesSubscribersAndShowsToast()
        {
            // arrange
            var subscriberMock = new Mock<IObserver<NotificationDataTransferObject>>();
            notificationService.Subscribe(subscriberMock.Object);

            var incomingNotification = new IncomingNotification
            {
                UserIdentifier = CurrentUserIdentifier,
                Timestamp = DateTime.UtcNow,
                Title = "Incoming",
                Body = "Body",
            };

            // act
            notificationService.OnNext(incomingNotification);

            // assert
            subscriberMock.Verify(
                observer => observer.OnNext(It.IsAny<NotificationDataTransferObject>()),
                Times.Once);
            toastNotificationServiceMock.Verify(
                toast => toast.Show("Incoming", "Body"), Times.Once);
        }

        [Test]
        public void ScheduleUpcomingRentalReminder_ReminderTimeAlreadyDue_SendsImmediately()
        {
            // arrange - start date is upcoming, but less than 24 hours away, so the reminder is due now
            var soonStartDate = DateTime.UtcNow.AddHours(1);

            // act
            notificationService.ScheduleUpcomingRentalReminder(
                CurrentUserIdentifier, OtherUserIdentifier, "Catan", soonStartDate);

            // assert - because delay <= 0, service calls SendNotificationToUser for each party
            notificationRepositoryMock.Verify(
                repository => repository.Add(It.IsAny<Notification>()), Times.AtLeast(2));
        }

        [Test]
        public void DeleteNotificationsByRequestId_DelegatesToRepository()
        {
            // arrange
            const int RelatedRequestIdentifier = 77;

            // act
            notificationService.DeleteNotificationsByRequestId(RelatedRequestIdentifier);

            // assert
            notificationRepositoryMock.Verify(
                repository => repository.DeleteByRequestId(RelatedRequestIdentifier), Times.Once);
        }

        [Test]
        public void Subscribers_ThreadSafe_MultipleConcurrentSubscribeCallsDoNotThrow()
        {
            // arrange
            const int ConcurrentSubscribers = 16;
            var tasks = new Task[ConcurrentSubscribers];

            // act
            for (var index = 0; index < ConcurrentSubscribers; index++)
            {
                tasks[index] = Task.Run(() =>
                    notificationService.Subscribe(Mock.Of<IObserver<NotificationDataTransferObject>>()));
            }

            var subscribeAction = () => Task.WaitAll(tasks);

            // assert
            subscribeAction.Should().NotThrow();
        }
    }
}
