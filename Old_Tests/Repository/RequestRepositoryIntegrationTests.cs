using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Repository;

namespace Property_and_Management.Tests.Repository
{
    [TestFixture]
    [Category("Integration")]
    public sealed class RequestRepositoryIntegrationTests : DatabaseTestBase
    {
        private RequestRepository requestRepository = null!;

        [SetUp]
        public void CreateRepository()
        {
            requestRepository = new RequestRepository();
        }

        [Test]
        public void AddThenGet_Roundtrip_PreservesAllFields()
        {
            var newRequest = new Request(
                id: 0,
                game: new Game { id = 1 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(4));

            requestRepository.Add(newRequest);
            var fetched = requestRepository.Get(newRequest.id);

            fetched.id.Should().Be(newRequest.id);
            fetched.Renter!.id.Should().Be(2);
            fetched.Owner!.id.Should().Be(1);
        }

        [Test]
        public void GetRequestsByGame_ReturnsOnlyForThatGame()
        {
            var requestForGameOne = new Request(
                id: 0,
                game: new Game { id = 1 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(4));
            var requestForGameTwo = new Request(
                id: 0,
                game: new Game { id = 2 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(7));
            requestRepository.Add(requestForGameOne);
            requestRepository.Add(requestForGameTwo);

            var requestsForGameOne = requestRepository.GetRequestsByGame(1);

            requestsForGameOne.Should().OnlyContain(request => request.Game!.id == 1);
        }
    }
}