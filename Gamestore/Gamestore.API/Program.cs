using Gamestore.BLL.DependencyResolvers;
using Gamestore.DAL;

namespace Gamestore.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Custom Services (Sıralama önemli - hocanın yöntemi)
        builder.Services.AddDataAccessLayer(builder.Configuration);  // 1. DAL (DbContext + Repos + UoW)
        builder.Services.AddServiceLayer();                          // 2. BLL Services
        builder.Services.AddDtoMapperService();                      // 3. AutoMapper

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
