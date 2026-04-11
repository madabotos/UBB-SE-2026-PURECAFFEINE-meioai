using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Model
{
    public class User : IEntity
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }

        public User() { }

        public User(int id)
        {
            Id = id;
        }

        public User(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }
}
