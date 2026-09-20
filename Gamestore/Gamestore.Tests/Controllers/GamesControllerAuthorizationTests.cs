using System.Security.Claims;
using Gamestore.API.Controllers;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.BLL.Constants;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gamestore.Tests.Controllers;

public class GamesControllerAuthorizationTests
{
    private readonly Mock<ICommentService> _commentService = new();
    private readonly Mock<IMapper> _mapper = new();

    [Fact]
    public async Task AddCommentShouldUseAuthenticatedUserName()
    {
        var request = CreateRequest("ImpersonatedUser");
        var dto = CreateDto("ImpersonatedUser");
        _mapper.Setup(mapper => mapper.Map<CreateCommentDto>(request)).Returns(dto);
        var controller = CreateController("Administrator");

        var result = await controller.AddComment("test-game", request);

        Assert.IsType<OkResult>(result);
        Assert.Equal("Administrator", dto.Comment.Name);
        _commentService.Verify(
            service => service.AddAsync(
                "test-game",
                It.Is<CreateCommentDto>(comment => comment.Comment.Name == "Administrator")),
            Times.Once);
    }

    [Fact]
    public async Task AddCommentShouldReturnUnauthorizedWhenNameClaimIsMissing()
    {
        var request = CreateRequest("ImpersonatedUser");
        var controller = CreateController(userName: null);

        var result = await controller.AddComment("test-game", request);

        Assert.IsType<UnauthorizedResult>(result);
        _commentService.Verify(
            service => service.AddAsync(It.IsAny<string>(), It.IsAny<CreateCommentDto>()),
            Times.Never);
    }

    private static CreateCommentRequest CreateRequest(string name)
    {
        return new CreateCommentRequest
        {
            Comment = new CommentCreateInfoRequest
            {
                Name = name,
                Body = "Comment body",
            },
        };
    }

    private static CreateCommentDto CreateDto(string name)
    {
        return new CreateCommentDto
        {
            Comment = new CommentCreateInfoDto
            {
                Name = name,
                Body = "Comment body",
            },
        };
    }

    private GamesController CreateController(string? userName)
    {
        var controller = new GamesController(
            Mock.Of<IGameService>(),
            Mock.Of<IOrderService>(),
            _commentService.Object,
            Mock.Of<IGameLookupService>(),
            _mapper.Object);
        var claims = string.IsNullOrWhiteSpace(userName)
            ? []
            : new[] { new Claim(JwtClaimNames.UniqueName, userName) };
        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Test",
            nameType: JwtClaimNames.UniqueName,
            roleType: JwtClaimNames.Role);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };

        return controller;
    }
}
