using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAppSDK.Runtime;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class GameMapper : IMapper<Game>
    {
        public Game ToModel(GameDTO gameDTO)
        {
            if (gameDTO == null) return null;

            return new Game
            {
                Id = gameDTO.Id,
                Owner = UserMapper.ToModel(gameDTO.Owner),
                Name = gameDTO.Name,
                Price = gameDTO.Price,
                MinimumPlayerNumber = gameDTO.MinimumPlayerNumber,
                MaximumPlayerNumber = gameDTO.MaximumPlayerNumber,
                Description = gameDTO.Description,
                Image = gameDTO.Image,
                IsActive = gameDTO.IsActive,
            };
        }

        public static IDTO<Game> ToDTO(Game model)
        {
            if (model == null) return null;

            return new GameDTO
            {
                Id = model.Id,
                Owner = UserMapper.ToDTO(model.Owner),
                Name = model.Name,
                Price = model.Price,
                MinimumPlayerNumber = model.MinimumPlayerNumber,
                MaximumPlayerNumber = model.MaximumPlayerNumber,
                Description = model.Description,
                Image = model.Image,
                IsActive = model.IsActive,
            };
        }
    }
}
