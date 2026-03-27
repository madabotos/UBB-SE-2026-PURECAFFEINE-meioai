using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Mapper;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class GameDTO : IDTO<Game>
    {
        public int Id { get; set; }
        public UserDTO Owner { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int MinimumPlayerNumber { get; set; }
        public int MaximumPlayerNumber { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public bool IsActive { get; set; }

        public GameDTO() { }
    }
}
