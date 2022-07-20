using DanTech.Data;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Controllers;

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
            services.AddDbContext<dgdb>(options => options.UseMySQL(Configuration.GetConnectionString("dg")));
            services.AddControllersWithViews();
            services.AddScoped<DTAuthenticate>();
            services.AddSingleton<IConfiguration>(Configuration);
  /*          services.AddAuthentication(o =>
            {
                o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie()
                .AddGoogleOpenIdConnect(options =>
               {
                   //options.ClientId = Configuration["Authentication:Google:ClientId"].ToString();
                   //options.ClientSecret = Configuration["Authentication:Google:ClientSecret"].ToString();
                   options.ClientId = "849195656550-mi3286esf9mrgk8gkacu2712c1qghi3m.apps.googleusercontent.com";
                   options.ClientSecret = "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";
                   options.Scope.Add("email");
                   options.Scope.Add(Google.Apis.Oauth2.v2.Oauth2Service.Scope.UserinfoProfile);
                   options.Scope.Add(Google.Apis.Oauth2.v2.Oauth2Service.Scope.UserinfoEmail);
                });
  */
 

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
            app.UseHttpsRedirection();
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
        }
    }
}
