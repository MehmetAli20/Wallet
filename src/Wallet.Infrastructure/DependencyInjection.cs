using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Idempotency;
using Wallet.Infrastructure.Persistence.Repositories;

namespace Wallet.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddDbContext<WalletDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("WalletDb")));

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();  

            return services;          
        }
    }
}