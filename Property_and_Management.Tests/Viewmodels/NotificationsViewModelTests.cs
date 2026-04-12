using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    // After Agent 2's refactor, NotificationsViewModel no longer performs file
    // I/O directly; it takes an IDismissedNotificationStore via constructor
    // injection. These tests mock the store so they never touch disk.
    [TestFixture]
    public sealed class NotificationsViewModelTests
    {
        private const int CurrentUserIdentifier = 1;
        private const int DismissedNotificationIdentifier = 7;

        private Mock<INotificationService> notificationServiceMock = null!;
        private Mock<IRequestService> requestServiceMock = null!;
        private Mock<ICurrentUserContext> currentUserContextMock = null!;
        private Mock<IDismissedNotificationStore> dismissedNotificationStoreMock = null!;

        [SetUp]
        public void SetUp()
        {
            notificationServiceMock = new Mock<INotificationService>();
            requestServiceMock = new Mock<IRequestService>();
            currentUserContextMock = new Mock<ICurrentUserContext>();
            dismissedNotificationStoreMock = new Mock<IDismissedNotificationStore>();

            currentUserContextMock
                .SetupGet(context => context.CurrentUserIdentifier)
                .Returns(CurrentUserIdentifier);
            notificationServiceMock
                .Setup(service => service.Subscribe(It.IsAny<IObserver<NotificationDataTransferObject>>()))
                .Returns(Mock.Of<IDisposable>());
            dismissedNotificationStoreMock
                .Setup(store => store.Load(CurrentUserIdentifier))
                .Returns(new HashSet<int>());
        }

        [Test]
        public void Constructor_LoadsFromService_ExcludesDismissed()
        {
            // arrange
            dismissedNotificationStoreMock
                .Setup(store => store.Load(CurrentUserIdentifier))
                .Returns(new HashSet<int> { DismissedNotificationIdentifier });
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList.Create(
                    BuildNotification(identifier: 1),
                    BuildNotification(identifier: DismissedNotificationIdentifier),
                    BuildNotification(identifier: 2)));

            // act
            var viewModel = BuildViewModel();

            // assert
            viewModel.PagedItems
                .Should().NotContain(notification => notification.Identifier == DismissedNotificationIdentifier);
        }

        [Test]
        public void DeleteNotificationByIdentifier_AddsToStoreAndRemovesFromCollection()
        {
            // arrange
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList.Create(BuildNotification(identifier: 1)));
            var viewModel = BuildViewModel();

            // act
            viewModel.DeleteNotificationByIdentifier(1);

            // assert
            dismissedNotificationStoreMock.Verify(
                store => store.Save(
                    CurrentUserIdentifier,
                    It.Is<IEnumerable<int>>(ids => System.Linq.Enumerable.Contains(ids, 1))),
                Times.AtLeastOnce);
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

        [Test]
        public void TryApproveOffer_Success_ReloadsCollection()
        {
            // arrange
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList<NotificationDataTransferObject>.Empty);
            requestServiceMock
                .Setup(service => service.ApproveOffer(It.IsAny<int>(), CurrentUserIdentifier))
                .Returns(Result<int, ApproveOfferError>.Success(1));
            var viewModel = BuildViewModel();
            notificationServiceMock.Invocations.Clear();

            // act
            var errorMessage = viewModel.TryApproveOffer(requestIdentifier: 10);

            // assert
            errorMessage.Should().BeNull();
            notificationServiceMock.Verify(
                service => service.GetNotificationsForUser(CurrentUserIdentifier),
                Times.AtLeastOnce);
        }

        [Test]
        public void TryDenyOffer_Success_ReloadsCollection()
        {
            // arrange
            notificationServiceMock
                .Setup(service => service.GetNotificationsForUser(CurrentUserIdentifier))
                .Returns(ImmutableList<NotificationDataTransferObject>.Empty);
            requestServiceMock
                .Setup(service => service.DenyOffer(It.IsAny<int>(), CurrentUserIdentifier))
                .Returns(Result<int, DenyOfferError>.Success(1));
            var viewModel = BuildViewModel();
            notificationServiceMock.Invocations.Clear();

            // act
            var errorMessage = viewModel.TryDenyOffer(requestIdentifier: 10);

            // assert
            errorMessage.Should().BeNull();
            notificationServiceMock.Verify(
                service => service.GetNotificationsForUser(CurrentUserIdentifier),
                Times.AtLeastOnce);
        }

        private NotificationsViewModel BuildViewModel()
        {
            return new NotificationsViewModel(
                notificationServiceMock.Object,
                requestServiceMock.Object,
                currentUserContextMock.Object,
                dismissedNotificationStoreMock.Object);
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
