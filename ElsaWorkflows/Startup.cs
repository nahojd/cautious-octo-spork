using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Dashboard.Extensions;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElsaWorkflow
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddElsa(elsa => elsa.AddEntityFrameworkStores<SqliteContext>(options => options.UseSqlite(Configuration.GetConnectionString("Sqlite"))))
				.AddHttpActivities(options => options.Bind(Configuration.GetSection("Elsa:Http")))
				.AddEmailActivities(options => options.Bind(Configuration.GetSection("Elsa:Smtp")))
				.AddTimerActivities(options => options.Bind(Configuration.GetSection("Elsa:Timers")))
				.AddConsoleActivities()

				.AddWorkflow<TestWorkflow>()

				.AddActivity<CalculateSquare>()

				.AddElsaDashboard()
				;
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app
				.UseStaticFiles()
				.UseHttpActivities()
				.UseRouting()
				.UseEndpoints(endpoints => endpoints.MapControllers())
				.UseWelcomePage();
		}
	}
}
