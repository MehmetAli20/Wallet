using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests.Api
{
    public sealed class ConflictOnceApiFixture : ApiFixture
    {
        private int _failNextSave;

        public void FailNextSave() => Interlocked.Exchange(ref _failNextSave, 1);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<UnitOfWork>();
                services.AddScoped<IUnitOfWork>(provider =>
                    new ConflictOnceUnitOfWork(provider.GetRequiredService<UnitOfWork>(), this));
            });
        }

        private bool TakeFailure() => Interlocked.Exchange(ref _failNextSave, 0) == 1;

        private sealed class ConflictOnceUnitOfWork : IUnitOfWork
        {
            private readonly UnitOfWork _inner;
            private readonly ConflictOnceApiFixture _fixture;

            public ConflictOnceUnitOfWork(UnitOfWork inner, ConflictOnceApiFixture fixture)
            {
                _inner = inner;
                _fixture = fixture;
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
                _fixture.TakeFailure()
                    ? throw new ConcurrencyConflictException(
                        "A concurrent modification was detected.",
                        new DbUpdateConcurrencyException("Simulated conflict."))
                    : _inner.SaveChangesAsync(cancellationToken);
        }
    }
}
