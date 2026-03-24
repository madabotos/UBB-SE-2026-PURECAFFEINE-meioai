using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class GameDTO : IDTO<Game>
    {
        public int Id { get; set; }
        public User Owner { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int MinimumPlayerNumber { get; set; }
        public int MaximumPlayerNumber { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public bool IsActive { get; set; }

        public GameDTO(int id, User owner, string name,
            double price, int minimumPlayerNumber, int maximumPlayerNumaber,
            string description, byte[] image, bool isActive)
        {
            Id = id;
            Owner = owner;
            Name = name;
            Price = price;
            MinimumPlayerNumber = minimumPlayerNumber;
            MaximumPlayerNumber = maximumPlayerNumaber;
            Description = description;
            Image = image;
            IsActive = isActive;
        }

        public GameDTO(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            Id = game.Id;
            Owner = game.Owner;
            Name = game.Name;
            Price = game.Price;
            MinimumPlayerNumber = game.MinimumPlayerNumber;
            MaximumPlayerNumber = game.MaximumPlayerNumber;
            Description = game.Description;
            Image = game.Image;
            IsActive = game.IsActive;
        }

        public Game ToModel()
        {
            return new Game(Id, Owner, Name, Price, MinimumPlayerNumber,
                MaximumPlayerNumber, Description, Image, IsActive);
        }

        public static IDTO<Game> FromModel(Game model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            return new GameDTO(model);
        }
    }
}
