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
    public sealed class UserServiceTests
    {
        private const int CurrentUserIdentifier = 1;
        private const int OtherUserIdentifier = 2;
        private const int ThirdUserIdentifier = 3;

        private Mock<IUserRepository> userRepositoryMock = null!;
        private Mock<IMapper<User, UserDataTransferObject>> userMapperMock = null!;
        private UserService userService = null!;

        [SetUp]
        public void SetUp()
        {
            userRepositoryMock = new Mock<IUserRepository>();
            userMapperMock = new Mock<IMapper<User, UserDataTransferObject>>();
            userMapperMock
                .Setup(mapper => mapper.ToDataTransferObject(It.IsAny<User>()))
                .Returns<User>(user => new UserDataTransferObject
                {
                    Identifier = user.Identifier,
                    DisplayName = user.DisplayName,
                });
            userService = new UserService(userRepositoryMock.Object, userMapperMock.Object);
        }

        [Test]
        public void GetUsersExcept_ExcludesTheSpecifiedUser()
        {
            // arrange
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList.Create(
                    new User(CurrentUserIdentifier, "Me"),
                    new User(OtherUserIdentifier, "Alice"),
                    new User(ThirdUserIdentifier, "Bob")));

            // act
            var result = userService.GetUsersExcept(CurrentUserIdentifier);

            // assert
            result.Should().NotContain(user => user.Identifier == CurrentUserIdentifier);
        }

        [Test]
        public void GetUsersExcept_ReturnsMappedDataTransferObjects()
        {
            // arrange
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList.Create(
                    new User(OtherUserIdentifier, "Alice"),
                    new User(ThirdUserIdentifier, "Bob")));

            // act
            var result = userService.GetUsersExcept(CurrentUserIdentifier);

            // assert
            result.Should().HaveCount(2);
            result.Should().Contain(user => user.Identifier == OtherUserIdentifier && user.DisplayName == "Alice");
            result.Should().Contain(user => user.Identifier == ThirdUserIdentifier && user.DisplayName == "Bob");
        }

        [Test]
        public void GetUsersExcept_EmptyRepository_ReturnsEmptyList()
        {
            // arrange
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList<User>.Empty);

            // act
            var result = userService.GetUsersExcept(CurrentUserIdentifier);

            // assert
            result.Should().BeEmpty();
        }
    }
}
