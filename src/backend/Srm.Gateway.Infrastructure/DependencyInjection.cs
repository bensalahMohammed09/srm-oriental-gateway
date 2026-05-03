using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Srm.Gateway.Application.Commands;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Application.Queries;
using Srm.Gateway.Application.Services;
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
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            services.AddScoped<AuditInterceptor>();


            // Database Configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson(); // 👈 C'EST CETTE LIGNE QUI MANQUE
            var dataSource = dataSourceBuilder.Build();
            services.AddDbContext<SrmDbContext>((sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
                options.UseNpgsql(dataSource)
                       .UseSnakeCaseNamingConvention() // Map C# PascalCase to Postgres snake_case
                       .AddInterceptors(auditInterceptor);
            });

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<OcrIngestionValidator>();

            services.AddScoped<IDocumentQueryService, DocumentQueryService>();
            services.AddScoped<IDocumentCommandService, DocumentCommandService>();
            services.AddScoped<IWorkflowService, WorkflowService>();
            services.AddScoped<IAuthService, AuthService>();

            // 🚀 LA PIÈCE MANQUANTE : On enregistre le ProfileService !
            services.AddScoped<IProfileService, ProfileService>();

            services.AddScoped<IFileStorageService, FileStorageService>();

            services.AddScoped<IDocumentMetadataService, DocumentMetadataService>();

            services.AddScoped<IN8nService, N8nService>();


            return services;
        }
    }
}