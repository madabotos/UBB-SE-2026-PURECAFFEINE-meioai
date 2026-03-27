using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class UserDTO : IDTO<User>
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }

        public UserDTO() { }
    }
}
