using coco;
using coco.Services;
using coco.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost _host = Host.CreateDefaultBuilder().ConfigureServices(
    services =>
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase("BankDb").
            UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.EnableSensitiveDataLogging(true);
           
        });

        services.AddScoped<IMain, Main>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<DbContext, AppDbContext>();
    })
    .Build();
var app = _host.Services.GetRequiredService<IMain>();
 app.Run();