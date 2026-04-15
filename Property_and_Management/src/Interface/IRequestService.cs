using System;
using System.Collections.Immutable;
using Property_and_Management.Src.DataTransferObjects;

namespace Property_and_Management.Src.Interface
{
    public interface IRequestService
    {
        ImmutableList<RequestDTO> GetRequestsForRenter(int renterId);

        ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId);

        Result<int, CreateRequestError> CreateRequest(
            int gameId,
            int renterId,
            int ownerId,
            DateTime startDate,
            DateTime endDate);

        Result<int, ApproveRequestError> ApproveRequest(int requestId, int ownerId);

        Result<int, DenyRequestError> DenyRequest(int requestId, int ownerId, string reason);

        int CancelRequest(int requestId, int cancellingUserId);

        void OnGameDeactivated(int gameId);

        bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate);

        ImmutableList<(DateTime StartDate, DateTime EndDate)> GetBookedDates(
            int gameId,
            int calendarMonth,
            int calendarYear);

        Result<int, OfferError> OfferGame(int requestId, int offeringUserId);
    }
}