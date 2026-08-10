using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Transfers;
using Wallet.Application.Abstractions.Users;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Idempotency;
using Wallet.Infrastructure.Persistence.Repositories;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;
using Wallet.Infrastructure.Persistence.Repositories.TransferRepository;
using Wallet.Infrastructure.Persistence.Repositories.UserRepository;

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
            services.AddScoped<IScheduledTransferRepository, ScheduledTransferRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
            services.AddScoped<IReconciliationRepository, ReconciliationRepository>();

            return services;          
        }
    }
}