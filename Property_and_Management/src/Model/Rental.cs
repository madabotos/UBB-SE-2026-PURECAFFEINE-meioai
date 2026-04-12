using System;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Model
{
    public class Rental : IEntity
    {
        public int Identifier { get; set; }
        public Game Game { get; set; }
        public User Renter { get; set; }
        public User Owner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Rental()
        {
        }

        public Rental(int identifier, Game game, User renter, User owner, DateTime startDate, DateTime endDate)
        {
            Identifier = identifier;
            Game = game;
            Renter = renter;
            Owner = owner;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}

