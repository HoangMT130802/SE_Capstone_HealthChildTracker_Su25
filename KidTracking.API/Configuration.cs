using Repositories.Common;
using Repositories.Interfaces;
using Services.Common;

namespace KidTracking.API
{
    public static class Configuration
    {
        public static IServiceCollection AddAPIConfiguration(this IServiceCollection services)
        {
            // Common
            services.AddHttpContextAccessor();
            services.AddAutoMapper(typeof(MapperProfile).Assembly);
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
