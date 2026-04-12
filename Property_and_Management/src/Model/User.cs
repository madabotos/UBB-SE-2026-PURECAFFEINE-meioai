using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Model
{
    public class User : IEntity
    {
        public int Identifier { get; set; }
        public string DisplayName { get; set; }

        public User()
        {
        }

        public User(int identifier)
        {
            Identifier = identifier;
        }

        public User(int identifier, string displayName)
        {
            Identifier = identifier;
            DisplayName = displayName;
        }
    }
}

