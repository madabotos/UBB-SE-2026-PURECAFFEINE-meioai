using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Transactions;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Windows.Gaming.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Property_and_Management.src.Service
{
    public enum CreateRequestError
    {
        OWNER_CANNOT_RENT_ERROR = -1,
        DATES_UNAVAILABLE_ERROR = -2,
        GAMEID_DOES_NOT_EXIST_ERROR = -3
    }
    public enum ApproveRequestError
    {
        UNAUTHORIZED_ERROR = -1,
        NOT_FOUND_ERROR = -2,
        TRANSACTION_FAILED_ERROR = -3
    }
    public enum DenyRequestError
    {
        UNAUTHORIZED_ERROR = -1,
        NOT_FOUND_ERROR = -2
    }
    public class RequestService : IRequestService
    {

        private IRequestRepository _requestRepository;
        private IRentalRepository _rentalRepository;
        private INotificationService _notificationService;
        private IGameRepository _gameRepository;
        // Db connection handling should be refactored to an interface later, removing it from this refactor since SQL attributes module is gone.

        public ImmutableList<RequestDTO> GetRequestsForRenter(int renterId)
        {
            return _requestRepository
                .GetRequestsByRenter(renterId)
                .Select(r => new RequestDTO(r))
                .ToImmutableList();
        }

        public ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId)
        {
            return _requestRepository
                .GetRequestsByOwner(ownerId)
                .Select(r => new RequestDTO(r))
                .ToImmutableList();
        }

        public void SetRequestRepository(IRequestRepository newRequestRepository) =>
            _requestRepository = newRequestRepository;
        public void SetRentalRepository(IRentalRepository newRentalRepository) =>
            _rentalRepository = newRentalRepository;
        public void SetNotificationService(INotificationService newNotificationService) =>
            _notificationService = newNotificationService;

        //[BL-LFC-01] A new Request is created. We say it is PENDING while existing in the database.
        public int CreateRequest(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate)
        {
            // An Owner cannot rent their own game
            if (renterId == ownerId)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            // The GameID must exist in the database
            try
            {
                _gameRepository.Get(gameId);
            }
            catch (KeyNotFoundException)
            {
                return (int)CreateRequestError.GAMEID_DOES_NOT_EXIST_ERROR;
            }

            // The requested dates must be available
            if (!CheckAvailability(gameId, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            // If all checks pass, we create the Request object in memory
            var request = new Request(
                id: 0,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            // Tell the repo to execute the raw SQL INSERT
            _requestRepository.Add(request);

            // Not sure why we wrote the return this way, so I changed it so we do not have efficiency problems (downloading every single request for that renter) and race conditions (from the .Last() if two users hit Rent at the same time)
            /*return _requestRepository
                .GetRequestsByRenter(renterId)
                .Last(r => r.Game?.Id == gameId &&
                   r.StartDate == startDate &&
                   r.EndDate == endDate)
                .Id;*/
            return request.Id;
        }

        //[BL-LFC-02] When an Owner sends an ACCEPT signal for a Request:
        //(a) the system shall atomically create a new Rental entity from the Request's data;
        //(b) the system shall delete all other PENDING Requests for the same game_id whose date range overlaps with the accepted rental period (including its 48-hour buffer);
        //(c) the system shall send a notification to each Renter whose overlapping Request was deleted, stating the game is unavailable in their requested period and providing a link to the booking interface;
        //(d) the original Request entity shall be deleted.
        //All steps (a)–(d) must succeed atomically; if any fail, the transaction shall be rolled back.
        public int ApproveRequest(int requestId, int ownerId)
        {
            Request request;
            // Check if the request exists
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            {
                return (int)ApproveRequestError.NOT_FOUND_ERROR;
            }

            // Check if the person approving the request is the owner of the game
            if (request.Owner?.Id != ownerId)
                return (int)ApproveRequestError.UNAUTHORIZED_ERROR;

            var bufferedStart = request.StartDate.AddHours(-48);
            var bufferedEnd = request.EndDate.AddHours(48);

            try
            {
                using (var transaction = new System.Transactions.TransactionScope())
                {
                    // Create a new Rental entity using the data from the approved Reuqest
                    var rental = new Rental(
                    id: 0,
                    game: request.Game,
                    renter: request.Renter,
                    owner: request.Owner,
                    startDate: request.StartDate,
                    endDate: request.EndDate);

                    _rentalRepository.Add(rental);

                    // Find and delete all OTHER requests that overlap 
                    var overlappingRequests = _requestRepository
                        .GetRequestsByGame(request.Game?.Id ?? 0)
                        .Where(r => r.Id != requestId &&
                                    r.StartDate < bufferedEnd &&
                                    r.EndDate > bufferedStart)
                        .ToList();

                    foreach (var overlap in overlappingRequests)
                    {
                        _requestRepository.Delete(overlap.Id);
                        //_notification_service.SendNotification(
                        //    userId: overlap.Renter?.Id ?? 0,
                        //    message: $"Your request for game {request.Game?.Id} " +
                        //             $"({overlap.StartDate:d}–{overlap.EndDate:d}) was declined " +
                        //             $"because the game is no longer available in that period.");
                    }

                    // Delete the original request
                    _requestRepository.Delete(requestId);

                    // Commiting the transaction. If any of the above operations threw an exception, this line will not be reached and the transaction will be rolled back.
                    transaction.Complete();

                    // Return newly generated rental_id
                    return rental.Id;
                }
            }
            catch (Exception)
            {
                return (int)ApproveRequestError.TRANSACTION_FAILED_ERROR;
            }
        }

        //[BL-LFC-03] When an Owner sends a DECLINE signal for a Request, the system shall delete the Request entity. No Rental is created. The user shall also be notified that their request has been declined.
        public int DenyRequest(int requestId, int ownerId, string reason)
        {
            Request request;
            // Check if the request exists
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)DenyRequestError.NOT_FOUND_ERROR; }

            // Check if the person declining the request is the owner of the game
            if (request.Owner?.Id != ownerId)
                return (int)DenyRequestError.UNAUTHORIZED_ERROR;

            // Delete the request
            _requestRepository.Delete(requestId);

            /*_notification_service.SendNotification(
                userId: request.Renter?.Id ?? 0,
                message: $"Your request for game {request.Game?.Id} " +
                         $"({request.StartDate:d}–{request.EndDate:d}) was declined. " +
                         $"Reason: {reason}");*/

            return requestId;
        }

        //[BL-LFC-04] A Renter may cancel (delete) their own PENDING Request at any time without requiring Owner approval.
        public void Cancelrequest(int requestId)
        {
            _requestRepository.Delete(requestId);
        }

        //[BL-LFC-05] Upon a game having its Active flag being set to FALSE, all requests to that game shall be declined and the respective users be notified of the decline.
        public void OnGameDeactivated(int gameId)
        {
            var pending = _requestRepository.GetRequestsByGame(gameId);

            //foreach (var request in pending)
            //{
            //    _requestRepository.Delete(request.Id);

            //    _notification_service.SendNotification(
            //        userId: request.Renter?.Id ?? 0,
            //        message: $"Your request for game {gameId} " +
            //                 $"({request.StartDate:d}–{request.EndDate:d}) has been declined " +
            //                 $"because the game is no longer available.");
            //}
        }

        //[API-GBD-04] The method shall return a list of objects. Each object shall contain:
        //StartDate (DateTime) — the start of the booked interval;
        //EndDate (DateTime) — the end of the booked interval, including the 48-hour buffer period
        //(i.e., rental end_date + 48 hours).
        //[API-GBD-05] The returned list shall be sorted by StartDate ascending.
        public ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId, int month = 0, int year = 0)
        {
            if (month == 0)
                month = DateTime.Now.Month;

            if (year == 0)
                year = DateTime.Now.Year;

            return _requestRepository
                .GetRequestsByGame(gameId)
                .Where(r => r.StartDate.Month == month && r.StartDate.Year == year)
                .OrderBy(r => r.StartDate)
                .Select(r => (r.StartDate, r.EndDate.AddDays(2)))
                .ToImmutableList();
        }

        //[API-CAV-04] IsAvailable shall be TRUE if and only if all of the following conditions hold:
        //(a) the requested [startDate, endDate] range does not overlap with any existing Rental interval
        //for the specified game_id;
        //(b) the requested startDate is not within the 48-hour buffer period of any existing Rental;
        //(c) the requested startDate is not more than one month in the future from the current date;
        //(d) the requested endDate is not more than one month in the future from the current date’
        //(e) the game_id corresponds to an existent and active game. 
        //If any condition is not met, IsAvailable shall be FALSE.

        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            bool isDateWithin1Month = endDate <= DateTime.Now.AddMonths(1);

            bool isTheGameActive = _gameRepository
                .GetAll()
                .Count(g => g.Id == gameId && g.IsActive) == 1;

            bool inAvailableTimeInterval = !_requestRepository
                .GetRequestsByGame(gameId)
                .Any(r => r.StartDate <= endDate && r.EndDate >= startDate);

            return isDateWithin1Month && isTheGameActive && inAvailableTimeInterval;
        }
    }
}

