using BCPUtilityDownloadFiles.Models;
using BCPUtilityDownloadFiles.Models.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BCPUtilityDownloadFiles
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionstring = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<BCPUtilityDBContext>(x => x.UseSqlServer(connectionstring));
            services.AddControllers();
            
            services.AddSingleton(Configuration.GetSection("SDxConfig").Get<SdxConfig>());
            services.AddSingleton(Configuration.GetSection("StorageUrl").Get<string>());
            services.AddSingleton(Configuration.GetSection("StorageAccountConfig").Get<StorageAccountConfig>());
            services.AddAutoMapper(typeof(SdxConfig));
            services.AddAutoMapper(typeof(string));
            services.AddAutoMapper(typeof(StorageAccountConfig));
            services.AddSingleton<Services.AuthenticationService>();
            services.AddSingleton<IHostedService, Services.AuthenticationService>(serviceProvider => serviceProvider.GetService<Services.AuthenticationService>());

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
