namespace Gamestore.API.Models.Requests.Genres;

public class UpdateGenreRequest
{
    public required GenreUpdateInfoRequest Genre { get; set; }
}
