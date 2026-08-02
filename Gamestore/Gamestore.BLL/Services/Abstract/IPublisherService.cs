using Gamestore.BLL.DTOs.Publishers;

namespace Gamestore.BLL.Services.Abstract;

public interface IPublisherService : IService<PublisherDto>
{
    Task<PublisherDto> GetByCompanyNameAsync(string companyName);

    Task<PublisherDto> GetByGameKeyAsync(string gameKey);

    Task<PublisherDto> CreateAsync(CreatePublisherDto dto);

    Task<PublisherDto> UpdateAsync(UpdatePublisherDto dto);
}
