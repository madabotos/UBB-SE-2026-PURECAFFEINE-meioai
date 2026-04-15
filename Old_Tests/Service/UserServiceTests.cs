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
        private const int currentUserId = 1;
        private const int OtherUserIdentifier = 2;
        private const int ThirdUserIdentifier = 3;

        private Mock<IUserRepository> userRepositoryMock = null!;
        private Mock<IMapper<User, UserDTO>> userMapperMock = null!;
        private UserService userService = null!;

        [SetUp]
        public void SetUp()
        {
            userRepositoryMock = new Mock<IUserRepository>();
            userMapperMock = new Mock<IMapper<User, UserDTO>>();
            userMapperMock
                .Setup(mapper => mapper.ToDTO(It.IsAny<User>()))
                .Returns<User>(user => new UserDTO
                {
                    id = user.Id,
                    DisplayName = user.DisplayName,
                });
            userService = new UserService(userRepositoryMock.Object, userMapperMock.Object);
        }

        [Test]
        public void GetUsersExcept_ExcludesTheSpecifiedUser()
        {
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList.Create(
                    new User(currentUserId, "Me"),
                    new User(OtherUserIdentifier, "Alice"),
                    new User(ThirdUserIdentifier, "Bob")));

            var result = userService.GetUsersExcept(currentUserId);

            result.Should().NotContain(user => user.Id == currentUserId);
        }

        [Test]
        public void GetUsersExcept_ReturnsMappedDataTransferObjects()
        {
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList.Create(
                    new User(OtherUserIdentifier, "Alice"),
                    new User(ThirdUserIdentifier, "Bob")));

            var result = userService.GetUsersExcept(currentUserId);

            result.Should().HaveCount(2);
            result.Should().Contain(user => user.Id == OtherUserIdentifier && user.DisplayName == "Alice");
            result.Should().Contain(user => user.Id == ThirdUserIdentifier && user.DisplayName == "Bob");
        }

        [Test]
        public void GetUsersExcept_EmptyRepository_ReturnsEmptyList()
        {
            userRepositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(ImmutableList<User>.Empty);

            var result = userService.GetUsersExcept(currentUserId);

            result.Should().BeEmpty();
        }
    }
}