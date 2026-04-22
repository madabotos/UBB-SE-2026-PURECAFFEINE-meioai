using System;
using System.Collections.Generic;
using System.Configuration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Repository;

namespace Property_and_Management.Tests.Repository
{
    [TestFixture]
    [Category("Integration")]
    public sealed class IntegrationRequestRepositoryTests : DataBaseTests
    {
        private const string ConnectionStringName = "BoardRent";

        private readonly List<int> createdRequestIds = new();
        private RequestRepository requestRepository = null!;

        [SetUp]
        public void SetUp()
        {
            requestRepository = new RequestRepository();
        }


        [Test]
        public void AddThenGetPreservesAllFields()
        {
            var newRequest = new Request(
                0,
                new Game { Id = 1 },
                new User(2, "Madi the Renter"),
                new User(1, "Beatrice the Owner"),
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(4));

            requestRepository.Add(newRequest);
            createdRequestIds.Add(newRequest.Id);

            var fetched = requestRepository.Get(newRequest.Id);

            fetched.Id.Should().Be(newRequest.Id);
            fetched.Game.Id.Should().Be(1);
            fetched.Renter.Id.Should().Be(2);
            fetched.Owner.Id.Should().Be(1);
        }

        [Test]
        public void GetrRequestByGameAndReturnTheSpecificOne()
        {
            var requestForFirstGame = BuildRequest(1, 50, RequestStatus.Open);
            var requestForSecondGame = BuildRequest(2, 60, RequestStatus.Open);

            requestRepository.Add(requestForFirstGame);
            requestRepository.Add(requestForSecondGame);

            createdRequestIds.Add(requestForFirstGame.Id);
            createdRequestIds.Add(requestForSecondGame.Id);

            var requestsForFirstGame = requestRepository.GetRequestsByGame(1);

            requestsForFirstGame.Should().OnlyContain(request => request.Game.Id == 1);
            requestsForFirstGame.Should().Contain(request => request.Id == requestForFirstGame.Id);
            requestsForFirstGame.Should().NotContain(request => request.Id == requestForSecondGame.Id);
        }

        private static Request BuildRequest(
            int gameId,
            int startOffsetInDays,
            RequestStatus status,
            int? offeringUserId = null)
        {
            var startDate = new DateTime(2035, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(startOffsetInDays);
            var endDate = startDate.AddDays(2);

            return new Request(
                0,
                new Game { Id = gameId },
                new User(2, "Madi the Renter"),
                new User(1, "Beatrice the Owner"),
                startDate,
                endDate,
                status,
                offeringUserId.HasValue ? new User(offeringUserId.Value, $"User {offeringUserId.Value}") : null);
        }



    }
}
