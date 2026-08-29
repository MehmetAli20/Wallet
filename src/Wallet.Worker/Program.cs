using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Wallet.Application;
using Wallet.Application.Abstractions.Users;
using Wallet.Infrastructure;
using Wallet.Worker.Authentication;
using Wallet.Worker.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("WalletDb"))));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<ReconciliationJob>();
builder.Services.AddScoped<RecurringExpensesJob>();
builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalRequestsOnlyAuthorizationFilter() }
});

RecurringJob.AddOrUpdate<ReconciliationJob>(
    "reconciliation-job",
    job => job.RunAsync(),
    Cron.Minutely);

RecurringJob.AddOrUpdate<RecurringExpensesJob>(
    "recurring-expenses-job",
    job => job.RunAsync(),
    Cron.Hourly);

app.Run();