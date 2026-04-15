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
    public sealed class NotificationsViewModelTests
    {
        private const int currentUserId = 1;

        private Mock<INotificationService> notificationServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;

        [SetUp]
        public void SetUp()
        {
            notificationServiceMock = new Mock<INotificationService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();

            currentUserContextMock
                .SetupGet(context => context.currentUserId)
                .Returns(currentUserId);
            notificationServiceMock
                .Setup(service => service.Subscribe(It.IsAny<IObserver<NotificationDTO>>()))
                .Returns(Mock.Of<IDisposable>());
        }

        [Test]
        public void Constructor_LoadsAllNotificationsFromService()
        {
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(currentUserId))
                .Returns(ImmutableList.Create(
                    BuildNotification(id: 1),
                    BuildNotification(id: 7),
                    BuildNotification(id: 2)));

            var viewModel = BuildViewModel();

            viewModel.PagedItems
                .Should().HaveCount(3);
        }

        [Test]
        public void DeleteNotificationByIdentifier_DeletesFromServiceAndRemovesFromCollection()
        {
            notificationServiceMock
                .SetupSequence(service => service.GetNotificationsForUser(currentUserId))
                .Returns(ImmutableList.Create(BuildNotification(id: 1)))
                .Returns(ImmutableList<NotificationDTO>.Empty);
            var viewModel = BuildViewModel();

            viewModel.DeleteNotificationByIdentifier(1);

            notificationServiceMock.Verify(
                service => service.DeleteNotificationByIdentifier(1),
                Times.Once);
            viewModel.PagedItems.Should().BeEmpty();
        }

        [Test]
        public void OnNext_ReloadsForCurrentUser()
        {
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(currentUserId))
                .Returns(ImmutableList<NotificationDTO>.Empty);
            var viewModel = BuildViewModel();
            notificationServiceMock.Invocations.Clear();

            viewModel.OnNext(BuildNotification(id: 99));

            notificationServiceMock.Verify(
                service => service.GetNotificationsForUser(currentUserId),
                Times.AtLeastOnce);
        }

        private NotificationsViewModel BuildViewModel()
        {
            return new NotificationsViewModel(
                notificationServiceMock.Object,
                currentUserContextMock.Object);
        }

        private static NotificationDTO BuildNotification(int id)
        {
            return new NotificationDTO
            {
                id = id,
                User = new UserDTO { id = currentUserId },
                Title = $"Notification {id}",
                Body = "Body",
                Timestamp = DateTime.UtcNow,
            };
        }
    }
}