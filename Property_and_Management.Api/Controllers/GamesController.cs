using Microsoft.AspNetCore.Mvc;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameRepository gameRepository;

    public GamesController(IGameRepository gameRepository)
    {
        this.gameRepository = gameRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Game>> GetAll() => Ok(gameRepository.GetAll());
}
