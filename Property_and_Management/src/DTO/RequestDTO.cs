using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class RequestDTO : IDTO<Request>
    {
        public int Id { get; set; }
        public GameDTO Game { get; set; }
        public UserDTO Renter { get; set; }
        public UserDTO Owner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string StartDateDisplay => StartDate.ToString("dd/MM");
        public string EndDateDisplay => EndDate.ToString("dd/MM");
        public string StartDateDisplayLong => $"Start: {StartDate:dd/MM/yyyy}";
        public string EndDateDisplayLong => $"End: {EndDate:dd/MM/yyyy}";

        public RequestDTO() { }
    }
}
