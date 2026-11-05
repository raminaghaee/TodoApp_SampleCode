using System.Reflection;
using Microsoft.OpenApi;

namespace TodoApp.API.Extensions;

public static class SwaggerExtensions
{
	public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		services.AddSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "TodoApp API",
				Version = "v1",
				Description = "REST API برای مدیریت تسک‌ها"
			});

			var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), true);

			options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				Name = "Authorization",
				Description = "توکن JWT را بدون کلمه Bearer وارد کنید.",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT"
			});

			// Factory pattern: سند در زمان تولید OpenAPI به اینجا پاس داده می‌شود
			options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecuritySchemeReference("Bearer", document),
					[]
				}
			});
		});

		return services;
	}

	public static WebApplication UseSwaggerDocumentation(this WebApplication app)
	{
		app.UseSwagger();

		app.UseSwaggerUI(options => { /* ... */ });

		// در Production مسیر /swagger نیازمند احراز هویت است
		if (app.Environment.IsProduction())
		{
			app.Use(async (context, next) =>
			{
				if (context.Request.Path.StartsWithSegments("/swagger"))
				{
					if (!context.User.Identity?.IsAuthenticated ?? true)
					{
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						return;
					}
				}
				await next();
			});
		}

		return app;
	}
}
