using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DataTransferObjects
{
    public class GameDataTransferObject : IDataTransferObject<Game>
    {
        public int Id { get; set; }
        public UserDataTransferObject Owner { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int MinimumPlayerNumber { get; set; }
        public int MaximumPlayerNumber { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public bool IsActive { get; set; }

        public GameDataTransferObject() { }
    }
}
