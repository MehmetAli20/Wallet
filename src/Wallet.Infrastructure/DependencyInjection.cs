using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Activity;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Settlements;
using Wallet.Application.Abstractions.Transfers;
using Wallet.Application.Abstractions.Users;
using Wallet.Infrastructure.Authentication;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Idempotency;
using Wallet.Infrastructure.Persistence.Repositories;
using Wallet.Infrastructure.Persistence.Repositories.AccountRepository;
using Wallet.Infrastructure.Persistence.Repositories.ExpenseRepository;
using Wallet.Infrastructure.Persistence.Repositories.GroupRepository;
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

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IScheduledTransferRepository, ScheduledTransferRepository>();
            services.AddScoped<ISettlementRepository, SettlementRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
            services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IGroupBalanceRepository, GroupBalanceRepository>();
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

            return services;          
        }
    }
}