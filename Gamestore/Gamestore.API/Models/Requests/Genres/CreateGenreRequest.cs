namespace Gamestore.API.Models.Requests.Genres;

public class CreateGenreRequest
{
    public required GenreCreateInfoRequest Genre { get; set; }
}
