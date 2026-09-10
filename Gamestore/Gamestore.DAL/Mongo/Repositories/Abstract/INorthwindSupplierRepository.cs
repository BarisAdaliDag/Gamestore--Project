using Gamestore.DAL.Mongo.Documents;

namespace Gamestore.DAL.Mongo.Repositories.Abstract;

public interface INorthwindSupplierRepository
{
    IQueryable<NorthwindSupplierDocument> Query();
}
