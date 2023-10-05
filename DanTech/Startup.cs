using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using DanTech.Controllers;
using DanTech.Services;

namespace DanTech
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
            //services.AddDbContext<dtdb>(options => options.UseMySQL(Configuration.GetConnectionString("DG")));
            services.AddControllersWithViews();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IDTDBDataService, DTDBDataService>();
            services.AddScoped<DTAuthenticate>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:4200", "https://danhass-github-io.vercel.com").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    });     
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || 1==1)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");  
            });
            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.Strict
            });
            app.UseCors(); app.ApplicationServices.GetServices<IConfiguration>().ToList();
        }
    }
}
