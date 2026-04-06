using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class RentalService : IRentalService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IMapper<Rental, RentalDTO> _rentalMapper;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const int BufferHours = 48;

        public RentalService(
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            IMapper<Rental, RentalDTO> rentalMapper)
        {
            _rentalRepository = rentalRepository;
            _gameRepository = gameRepository;
            _rentalMapper = rentalMapper;
        }

        public bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd)
        {
            var existingRentals = _rentalRepository.GetRentalsByGame(gameId);

            foreach (var rental in existingRentals)
            {
                var bufferStart = rental.StartDate.AddHours(-BufferHours);
                var bufferEnd = rental.EndDate.AddHours(BufferHours);
                if (newStart < bufferEnd && newEnd > bufferStart)
                    return false;
            }
            return true;
        }

        public void CreateConfirmedRental(Rental rental)
        {
            var game = _gameRepository.Get(rental.Game.Id);
            if (game.Owner.Id != rental.Owner.Id)
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                if (!IsSlotAvailable(rental.Game.Id, rental.StartDate, rental.EndDate, connection, transaction))
                    throw new Exception("Selected dates fall within the mandatory 48-hour buffer of another rental.");

                _rentalRepository.Add(rental, connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd,
            SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT COUNT(1) FROM Rentals " +
                "WHERE game_id = @game_id " +
                "AND @new_start < DATEADD(HOUR, @buffer, end_date) " +
                "AND @new_end > DATEADD(HOUR, -@buffer, start_date)";
            command.Parameters.AddWithValue("@game_id", gameId);
            command.Parameters.AddWithValue("@new_start", newStart);
            command.Parameters.AddWithValue("@new_end", newEnd);
            command.Parameters.AddWithValue("@buffer", BufferHours);
            return Convert.ToInt32(command.ExecuteScalar()) == 0;
        }

        public ImmutableList<RentalDTO> GetRentalsForRenter(int renterId) =>
            _rentalRepository
                .GetRentalsByRenter(renterId)
                .Select(r => _rentalMapper.ToDTO(r))
                .ToImmutableList();

        public ImmutableList<RentalDTO> GetRentalsForOwner(int ownerId) =>
            _rentalRepository
                .GetRentalsByOwner(ownerId)
                .Select(r => _rentalMapper.ToDTO(r))
                .ToImmutableList();
    }
}
