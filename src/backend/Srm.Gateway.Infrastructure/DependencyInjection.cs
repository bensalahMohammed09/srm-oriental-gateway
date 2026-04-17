using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Application.Validators;
using Srm.Gateway.Infrastructure.Data;
using Srm.Gateway.Infrastructure.Interceptors;
using Srm.Gateway.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure
{
    public static class DependencyInjection
    {
         public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
         {

            services.AddSingleton<AuditInterceptor>();
            // Database Configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<SrmDbContext>((sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention() // Map C# PascalCase to Postgres snake_case
                       .AddInterceptors(auditInterceptor);
            });
           
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<OcrIngestionValidator>();


            return services;
         } 
    }
}
