using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Requests.Publishers;
using Gamestore.API.Validators.Games;
using Gamestore.API.Validators.Publishers;

namespace Gamestore.Tests.Validators;

public class Epic04RequestValidatorTests
{
    [Fact]
    public async Task CreateGameValidatorShouldRejectInvalidEpicFourFieldsAndRelations()
    {
        var request = new CreateGameRequest
        {
            Game = new GameCreateInfoRequest
            {
                Name = string.Empty,
                Price = -1,
                UnitInStock = -1,
                Discount = 101,
            },
            Genres = [],
            Platforms = [],
            Publisher = Guid.Empty,
        };
        var validator = new CreateGameRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Game.Price");
        Assert.Contains(result.Errors, error => error.PropertyName == "Game.UnitInStock");
        Assert.Contains(result.Errors, error => error.PropertyName == "Game.Discount");
        Assert.Contains(result.Errors, error => error.PropertyName == "Genres");
        Assert.Contains(result.Errors, error => error.PropertyName == "Platforms");
        Assert.Contains(result.Errors, error => error.PropertyName == "Publisher");
    }

    [Fact]
    public async Task CreateGameValidatorShouldRejectReservedMongoKeyPrefix()
    {
        var request = new CreateGameRequest
        {
            Game = new GameCreateInfoRequest
            {
                Name = "SQL Game",
                Key = "mongo-reserved-key",
            },
            Genres = [Guid.NewGuid()],
            Platforms = [Guid.NewGuid()],
            Publisher = Guid.NewGuid(),
        };
        var validator = new CreateGameRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Game.Key");
    }

    [Fact]
    public async Task UpdateGameValidatorShouldAcceptValidEpicFourFieldsAndRelations()
    {
        var request = new UpdateGameRequest
        {
            Game = new GameUpdateInfoRequest
            {
                Id = Guid.NewGuid(),
                Name = "Portal 2",
                Key = "portal-2",
                Price = 29.99,
                UnitInStock = 50,
                Discount = 15,
            },
            Genres = [Guid.NewGuid()],
            Platforms = [Guid.NewGuid()],
            Publisher = Guid.NewGuid(),
        };
        var validator = new UpdateGameRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreatePublisherValidatorShouldRejectEmptyCompanyName()
    {
        var request = new CreatePublisherRequest
        {
            Publisher = new PublisherCreateInfoRequest { CompanyName = string.Empty },
        };
        var validator = new CreatePublisherRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Publisher.CompanyName");
    }

    [Fact]
    public async Task UpdatePublisherValidatorShouldAcceptValidPublisher()
    {
        var request = new UpdatePublisherRequest
        {
            Publisher = new PublisherUpdateInfoRequest
            {
                Id = Guid.NewGuid(),
                CompanyName = "Valve",
                HomePage = "https://www.valvesoftware.com",
                Description = "Game publisher",
            },
        };
        var validator = new UpdatePublisherRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
