using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DataTransferObjects
{
    public class UserDataTransferObject : IDataTransferObject<User>
    {
        public int Identifier { get; set; }
        public string DisplayName { get; set; }

        public UserDataTransferObject() { }
    }
}
