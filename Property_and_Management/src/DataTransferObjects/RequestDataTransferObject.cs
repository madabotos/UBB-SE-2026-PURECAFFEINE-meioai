using System;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.DataTransferObjects
{
    public class RequestDataTransferObject : IDataTransferObject<Request>
    {
        public int Identifier { get; set; }
        public GameDataTransferObject Game { get; set; }
        public UserDataTransferObject Renter { get; set; }
        public UserDataTransferObject Owner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Open;
        public UserDataTransferObject? OfferingUser { get; set; }

        public string StartDateDisplay => StartDate.ToString("dd/MM");
        public string EndDateDisplay => EndDate.ToString("dd/MM");
        public string StartDateDisplayLong => $"Start: {StartDate:dd/MM/yyyy}";
        public string EndDateDisplayLong => $"End: {EndDate:dd/MM/yyyy}";
        public bool CanOffer => Status == RequestStatus.Open;

        public RequestDataTransferObject()
        {
        }
    }
}
