using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CLReader
{
    class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDirectoryBrowser();
            services.AddMvc();
        }

        //public void Configure(IApplicationBuilder app)
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();
            app.UseFileServer(enableDirectoryBrowsing: true);
            app.UseStaticFiles();
            app.UseMvc();


            //if all else fails
            app.Run(async context => 
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("Probably not what you wanted....<p>");
                return;
            });
        }
    }
}
