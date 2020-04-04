using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace AIService
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<FormOptions>(x => {
                x.MultipartBodyLengthLimit = 300_000_00;//不到30M
            });

            services.AddTransient(typeof(Lazy<>));//注册Lazy

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddMvc().AddWebApiConventions();
            services.AddMvc().AddJsonOptions(opt =>
            {
                opt.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();//json字符串原样输出
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //    app.UseHsts();
            //}
            FileServerOptions fileServerOptions = new FileServerOptions();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html");
            app.UseFileServer(fileServerOptions);

            app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
            app.UseHttpsRedirection();
            app.UseCookiePolicy();
            app.UseStaticFiles(new StaticFileOptions()//自定义自己的文件路径,例如提供访问D盘下的Study目录，http://localhost:52723/MyStudy/README.md将访问D盘的Study目录中的README.md文件
            {
#if DEBUG
                RequestPath = new PathString("/UploadFiles"),//对外的访问路径
#else
                FileProvider = new PhysicalFileProvider(@"C:\inetpub\wwwroot\UploadFiles"),//指定实际物理路径
                RequestPath = new PathString("/UploadFiles")//对外的访问路径
#endif

            });
            app.UseStaticFiles(new StaticFileOptions()
            {
#if DEBUG
                RequestPath = new PathString("/Files"),//对外的访问路径
#else
                FileProvider = new PhysicalFileProvider(@"C:\inetpub\wwwroot\Files"),//指定实际物理路径
                RequestPath = new PathString("/Files")//对外的访问路径
#endif
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
