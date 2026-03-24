using System;
using System.Collections.Immutable;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Service;

namespace Property_and_Management.src.Interface
{
    public interface IRequestService
    {
        int ApproveRequest(int requestId, int ownerId);
        void Cancelrequest(int requestId);
        bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate);
        int CreateRequest(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate);
        int DenyRequest(int requestId, int ownerId, string reason);
        ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId, int month = 0, int year = 0);
        ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId);
        ImmutableList<RequestDTO> GetRequestsForRenter(int renterId);
        void OnGameDeactivated(int gameId);
        void SetNotificationService(NotificationService newNotificationService);
        void SetRentalRepository(IRentalRepository newRentalRepository);
        void SetRequestRepository(IRequestRepository newRequestRepository);
    }
}