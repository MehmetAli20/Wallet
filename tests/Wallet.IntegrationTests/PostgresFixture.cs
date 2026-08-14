using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;
using Wallet.Application.Abstractions.Users;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
            .Build();

        public string ConnectionString => _container.GetConnectionString();
        
        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            await using var context = CreateContext(TestCurrentUser.System);
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public WalletDbContext CreateContext(ICurrentUser currentUser)
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new WalletDbContext(options, currentUser);
        }   
    }
}