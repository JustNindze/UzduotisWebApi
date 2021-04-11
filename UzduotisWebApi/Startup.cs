using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace UzduotisWebApi
{
    public class Startup
    {
        // Loads values into ram, runs method FillUserData which loads values from Database.db into ram and also runs RemoveOldUserData asyncronously
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            UserData.MaxExpirationPeriod = int.Parse(Configuration["MaxExpirationPeriod"]);
            UserData.DefaultExpirationPeriod = int.Parse(Configuration["DefaultExpirationPeriod"]);

            if (UserData.DefaultExpirationPeriod > UserData.MaxExpirationPeriod)
            {
                throw new Exception("DefaultExpirationPeriod value is greater than MaxExpirationPeriod value");
            }

            UserData.FillUserData();

            Task.Run(() => UserData.RemoveOldUserData(int.Parse(Configuration["DictionaryCleanupPeriod"])));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dictionary API");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
