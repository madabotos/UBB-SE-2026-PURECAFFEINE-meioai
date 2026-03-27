using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class UserDTO : IDTO<User>
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }

        public UserDTO(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public UserDTO(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            Id = user.Id;
            DisplayName = user.DisplayName;
        }
    }
}
