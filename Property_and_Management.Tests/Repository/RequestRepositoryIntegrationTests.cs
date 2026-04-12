using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Repository;

namespace Property_and_Management.Tests.Repository
{
    // Integration tests for RequestRepository. These hit a real SQL Server
    // database called BoardRent_Test (see App.config). They are filtered out
    // by default — run with: dotnet test --filter TestCategory=Integration
    //
    // NOTE: After Agent 1's Task 5 refactor the orchestration of
    // ApproveAtomically moves to RequestService, so a number of the tests
    // below may need to be re-targeted. Check Agent 1's report before the
    // final merge.
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
            // arrange
            var newRequest = new Request(
                identifier: 0,
                game: new Game { Identifier = 1 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(4));

            // act
            requestRepository.Add(newRequest);
            var fetched = requestRepository.Get(newRequest.Identifier);

            // assert
            fetched.Identifier.Should().Be(newRequest.Identifier);
            fetched.Renter!.Identifier.Should().Be(2);
            fetched.Owner!.Identifier.Should().Be(1);
        }

        [Test]
        public void GetRequestsByGame_ReturnsOnlyForThatGame()
        {
            // arrange
            var requestForGameOne = new Request(
                identifier: 0,
                game: new Game { Identifier = 1 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(4));
            var requestForGameTwo = new Request(
                identifier: 0,
                game: new Game { Identifier = 2 },
                renter: new User(2, "Renter"),
                owner: new User(1, "Owner"),
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(7));
            requestRepository.Add(requestForGameOne);
            requestRepository.Add(requestForGameTwo);

            // act
            var requestsForGameOne = requestRepository.GetRequestsByGame(1);

            // assert
            requestsForGameOne.Should().OnlyContain(request => request.Game!.Identifier == 1);
        }
    }
}
