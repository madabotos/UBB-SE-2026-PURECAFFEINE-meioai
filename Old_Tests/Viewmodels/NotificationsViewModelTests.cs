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
    // NotificationsViewModel treats the database-backed notification service as
    // the source of truth, so tests keep all persistence behind mocks.
    [TestFixture]
    public sealed class NotificationsViewModelTests
    {
        private const int CurrentUserIdentifier = 1;

        private Mock<INotificationService> notificationServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;

        [SetUp]
        public void SetUp()
        {
            notificationServiceMock = new Mock<INotificationService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();

            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(CurrentUserIdentifier);
            notificationServiceMock
                .Setup(service => service.Subscribe(It.IsAny<IObserver<NotificationDataTransferObject>>()))
                .Returns(Mock.Of<IDisposable>());
        }

        [Test]
        public void Constructor_LoadsAllNotificationsFromService()
        {
            // arrange
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList.Create(
                    BuildNotification(identifier: 1),
                    BuildNotification(identifier: 7),
                    BuildNotification(identifier: 2)));

            // act
            var viewModel = BuildViewModel();

            // assert
            viewModel.PagedItems
                .Should().HaveCount(3);
        }

        [Test]
        public void DeleteNotificationByIdentifier_DeletesFromServiceAndRemovesFromCollection()
        {
            // arrange
            notificationServiceMock
                .SetupSequence(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList.Create(BuildNotification(identifier: 1)))
                .Returns(ImmutableList<NotificationDataTransferObject>.Empty);
            var viewModel = BuildViewModel();

            // act
            viewModel.DeleteNotificationByIdentifier(1);

            // assert
            notificationServiceMock.Verify(
                service => service.DeleteNotificationByIdentifier(1),
                Times.Once);
            viewModel.PagedItems.Should().BeEmpty();
        }

        [Test]
        public void OnNext_ReloadsForCurrentUser()
        {
            // arrange
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList<NotificationDataTransferObject>.Empty);
            var viewModel = BuildViewModel();
            notificationServiceMock.Invocations.Clear();

            // act
            viewModel.OnNext(BuildNotification(identifier: 99));

            // assert
            notificationServiceMock.Verify(
                service => service.GetNotificationsForUser(CurrentUserIdentifier),
                Times.AtLeastOnce);
        }

        private NotificationsViewModel BuildViewModel()
        {
            return new NotificationsViewModel(
                notificationServiceMock.Object,
                currentUserContextMock.Object);
        }

        private static NotificationDataTransferObject BuildNotification(int identifier)
        {
            return new NotificationDataTransferObject
            {
                Identifier = identifier,
                User = new UserDataTransferObject { Identifier = CurrentUserIdentifier },
                Title = $"Notification {identifier}",
                Body = "Body",
                Timestamp = DateTime.UtcNow,
            };
        }
    }
}
