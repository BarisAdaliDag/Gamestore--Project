using Gamestore.BLL.DTOs.Abstract;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public abstract class BaseService<TDto, TEntity>(IUnitOfWork unitOfWork, IMapper mapper)
    : IService<TDto>
    where TDto : class, IDto
    where TEntity : class
{
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;

    protected IMapper Mapper { get; } = mapper;

    protected abstract IRepository<TEntity> Repository { get; }

    public virtual async Task<IEnumerable<TDto>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();
        return Mapper.Map<IEnumerable<TDto>>(entities);
    }

    public virtual async Task<TDto> GetByIdAsync(Guid id)
    {
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);
        return Mapper.Map<TDto>(entity);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);

        Repository.Delete(entity);
        await UnitOfWork.SaveChangesAsync();
    }
}