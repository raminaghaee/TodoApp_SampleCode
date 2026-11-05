
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TodoApp.API.Configuration;
using TodoApp.API.Extensions;
using TodoApp.API.Middlewares;
using TodoApp.API.Services;
using TodoApp.Business.Auth;
using TodoApp.Business.Common;
using TodoApp.Data.BackgroundJobs;


// Bootstrap Logger — قبل از ساخت Host، خطاهای Startup هم لاگ می‌شوند
Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.CreateBootstrapLogger();

try
{
	Log.Information("Starting TodoApp.API...");

	var builder = WebApplication.CreateBuilder(args);

	// Serilog — کل پیکربندی (Sink ها، Log Level) از appsettings.json
	builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext());

	// Enricher های ثبت‌شده در DI
	// فعال‌سازی LogContext برای UserId/RequestId
	// نکته امنیتی (Destructuring): Mask کردن فیلدهای حساس هنگام لاگ کل شیء
	// .Destructure.ByTransforming<LoginRequestDto>(dto => new
	// {
	//     dto.Email,
	//     Password     = "***MASKED***",
	//     Token        = "***MASKED***",
	//     RefreshToken = "***MASKED***"
	// })

	builder.Services.RegisterServices(builder.Configuration);

	builder.Services.AddCorsPolicy(builder.Configuration);

	builder.Services.AddControllers().AddJsonOptions(o =>
		o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
	);
	builder.Services.AddSwaggerDocumentation();

	builder.Services.AddSecurityHeaders();

	var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
		?? throw new InvalidOperationException("بخش 'Jwt' در configuration یافت نشد.");

	if (string.IsNullOrWhiteSpace(jwtSettings.Key))
		throw new InvalidOperationException(
			"کلید JWT تنظیم نشده است. " +
			"Development: dotnet user-secrets set \"Jwt:Key\" \"...\" — " +
			"Production: Environment Variable با نام Jwt__Key");

	if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
		throw new InvalidOperationException("'Jwt:Issuer' و 'Jwt:Audience' باید تنظیم شده باشند.");

	var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);
	if (jwtKeyBytes.Length < 32)
		throw new InvalidOperationException("کلید JWT باید حداقل ۳۲ بایت (۲۵۶ بیت) باشد تا امنیت HMAC-SHA256 حفظ شود.");


	builder.Services
		.AddAuthentication(options =>
		{
			// AddIdentity به‌صورت پیش‌فرض کوکی را DefaultAuthenticate/Challenge قرار می‌دهد؛
			// صراحتاً JWT را تعیین می‌کنیم تا [Authorize] بدون توکن دقیقاً 401 برگرداند (نه 302).
			options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
		.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidIssuer = jwtSettings.Issuer,

				ValidateAudience = true,
				ValidAudience = jwtSettings.Audience,

				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

				// پیش‌فرض ۵ دقیقه است → برای انقضای دقیق صفر می‌شود
				ClockSkew = TimeSpan.Zero
			};
			options.EnableTokenRevocation(); // T-024
		});

	builder.Services.AddAuthorization();

	// Rate Limiting
	// Login: 5/15min per IP | Global: 100/min per UserId/IP | Search: 30/min per UserId
	builder.Services.AddRateLimiting();

	builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
	builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
	builder.Services.AddProblemDetails();

	var app = builder.Build();


	// Security Headers
	// اولین Middleware در Pipeline تا هدرها روی «همه» Response ها
	// (شامل خطاهای Exception Handler و صفحات Swagger) اعمال شوند.
	app.UseSecurityHeaders();

	// HSTS فقط در Production
	// (در Development غیرفعال است تا Debug با HTTP مختل نشود)
	if (!app.Environment.IsDevelopment())
	{
		app.UseHsts();
	}

	// Pipeline — ترتیب حیاتی است:

	// UseAuthentication → UserIdLogging → UseAuthorization → RequestLogging
	// (UserIdLogging بیرونی‌تر از RequestLogging است تا لاگ نهایی درخواست
	//  هم شامل UserId باشد)
	app.UseExceptionHandler();


	// UseCors باید بعد از UseRouting و قبل از UseAuthentication باشد.
	// در .NET 10 با MapControllers، UseRouting به‌صورت ضمنی فراخوانی می‌شود،
	// ولی برای وضوح و اطمینان از ترتیب صحیح Middleware، صریح فراخوانی می‌کنیم.
	// ترتیب نهایی: Routing → CORS → Authentication → RateLimiter → Authorization
	app.UseRouting();
	app.UseCorsPolicy();

	app.UseAuthentication();
	app.UseMiddleware<UserIdLoggingMiddleware>();
	app.UseRateLimiter();
	app.UseAuthorization();


	// Hangfire Dashboard در /hangfire
	// بعد از UseAuthorization تا HttpContext.User پر شده باشد.
	// پیش‌فرض: فعال فقط در Development (با appsettings هم قابل کنترل است)
	if (builder.Configuration.GetValue<bool>("Hangfire:EnableDashboard", app.Environment.IsDevelopment()))
	{
		app.UseHangfireDashboard("/hangfire", new DashboardOptions
		{
			Authorization = new[] { new HangfireDashboardAuthFilter(app.Environment) }
		});
	}

	app.UseSwaggerDocumentation();
	app.UseMiddleware<RequestLoggingMiddleware>();
	app.MapControllers();


	// ثبت RecurringJob (هر ۱۵ دقیقه)
	// API استاتیک RecurringJob استفاده نمی‌کنیم چون JobStorage.Current
	// هنوز مقدار نگرفته؛ به‌جای آن IRecurringJobManager را از DI می‌گیریم.
	// IRecurringJobManager Singleton است و هنگام Resolve، Storage را
	// (SqlServerStorage پیکربندی‌شده در DiContainer) می‌سازد.
	// ============================================================
	var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

	var cleanupCron = builder.Configuration.GetValue<string>("Hangfire:CleanupCron", "0 0 * * *");

	recurringJobManager.AddOrUpdate<CleanupJob>(
		"cleanup-job",
		job => job.HardDeleteExpiredTaskItems(CancellationToken.None),
		cleanupCron,
		new RecurringJobOptions { TimeZone = CleanupJob.GetRunTimeZone() });
	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Host terminated unexpectedly");
	throw;
}
finally
{
	Log.CloseAndFlush();  // Flush بافرهای File/SQL Sink قبل از خروج
}

public partial class Program { } // برای تست Integration با WebApplicationFactory
