using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Transfers;
using Wallet.Domain.Transfers;

namespace Wallet.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<TransferService>();
            services.AddScoped<TransferMoneyService>();

            return services;
        }
    }
}
