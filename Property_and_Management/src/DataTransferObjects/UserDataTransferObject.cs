using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.DataTransferObjects
{
    public class UserDataTransferObject : IDataTransferObject<User>
    {
        public int Identifier { get; set; }
        public string DisplayName { get; set; }

        public UserDataTransferObject()
        {
        }
    }
}
