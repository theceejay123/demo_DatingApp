using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      IHost host = CreateHostBuilder(args).Build();
      using IServiceScope scope = host.Services.CreateScope();
      IServiceProvider services = scope.ServiceProvider;
      try
      {
        DataContext context = services.GetRequiredService<DataContext>();
        UserManager<AppUser> userManager = services.GetRequiredService<UserManager<AppUser>>();
        RoleManager<AppRole> roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        await context.Database.MigrateAsync();
        await Seed.SeedUsers(userManager, roleManager);
      }
      catch (Exception ex)
      {
        ILogger logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error has occured during migration");
      }

      await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
            });
  }
}
