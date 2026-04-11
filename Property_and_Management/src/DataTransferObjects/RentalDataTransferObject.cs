using System;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DataTransferObjects
{
    public class RentalDataTransferObject : IDataTransferObject<Rental>
    {
        public int Identifier { get; set; }
        public GameDataTransferObject Game { get; set; }
        public UserDataTransferObject Renter { get; set; }
        public UserDataTransferObject Owner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string StartDateDisplay => StartDate.ToString("dd/MM");
        public string EndDateDisplay => EndDate.ToString("dd/MM");
        public string StartDateDisplayLong => $"Start: {StartDate:dd/MM/yyyy}";
        public string EndDateDisplayLong => $"End: {EndDate:dd/MM/yyyy}";
        public bool IsExpired => EndDate < DateTime.UtcNow;

        public RentalDataTransferObject() { }
    }
}
