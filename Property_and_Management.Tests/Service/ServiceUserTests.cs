using Moq;
using NUnit.Framework;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Service;
using System.Collections.Immutable;
using System.Linq;

namespace Property_and_Management.Tests.Service
{
    [TestFixture]
    public sealed class ServiceUserTests
    {
        private const int MeId = 1;
        private const int SecondId = 2;
        private const int ThirdId = 3;

        private Mock<IUserRepository> RepositoryMock = null!;
        private Mock<IMapper<User, UserDTO>> MapperMock = null!;
        private UserService Service = null!;

        [SetUp]
        public void SetUp()
        {
            RepositoryMock = new Mock<IUserRepository>();
            MapperMock = new Mock<IMapper<User, UserDTO>>();
            MapperMock
                .Setup(m => m.ToDTO(It.IsAny<User>()))
                .Returns<User>(u => new UserDTO { Id = u.Id, DisplayName = u.DisplayName });

            Service = new UserService(RepositoryMock.Object, MapperMock.Object);
        }

        [Test]
        public void GetNumberOfUsersExceptMe()
        {
            var allUsers = ImmutableList.Create(
                new User(MeId, "Me"),
                new User(SecondId, "Alice"),
                new User(ThirdId, "Bob")
            );
            RepositoryMock.Setup(db => db.GetAll()).Returns(allUsers);


            var result = Service.GetUsersExcept(MeId);

            Assert.That(result.Select(u => u.Id), Does.Not.Contain(MeId));
            Assert.That(result, Has.Count.EqualTo(2));
            
        }

        [Test]
        public void GetUsersWhenReturnedListIsEmpty()
        {

            RepositoryMock.Setup(db => db.GetAll()).Returns(ImmutableList<User>.Empty);
            var result = Service.GetUsersExcept(MeId);
            Assert.That(result, Is.Empty);

            RepositoryMock.Setup(db => db.GetAll()).Returns(ImmutableList.Create(new User(MeId, "Me")));
            var result1 = Service.GetUsersExcept(MeId);
            Assert.Equals(result1, 0);
        }

        [Test]
        public void GetUsersWithCorrecData()
        {

            var allUsers = ImmutableList.Create(
                new User(SecondId, "Alice"),
                new User(ThirdId, "Bob")
            );
            RepositoryMock.Setup(db => db.GetAll()).Returns(allUsers);

            var result = Service.GetUsersExcept(MeId);
            Assert.That(result.Any(u => u.Id == SecondId && u.DisplayName == "Alice"), Is.True);
            Assert.That(result.Any(u => u.Id == ThirdId && u.DisplayName == "Bob"), Is.True);
        }
    }
}