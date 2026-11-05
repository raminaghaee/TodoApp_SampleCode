using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business;
using TodoApp.Business.AccountService;
using TodoApp.Business.Auth;
using TodoApp.Business.Authentication;
using TodoApp.Business.BackgroundJobs;
using TodoApp.Business.Common;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Business.Services;
using TodoApp.Data;
using TodoApp.Data.BackgroundJobs;
using TodoApp.Data.Common;
using TodoApp.Data.Contexts;
using TodoApp.Data.Repositories;


namespace TodoApp.API.Configuration;

public static class DiContainer
{
	public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<TodoAppDbContext>(options =>
			options.UseSqlServer(configuration.GetConnectionString("TodoAppConnectionString")));

		services.AddHttpContextAccessor();
		#region Identity
		// ASP.NET Identity: مدیریت کاربران، هش رمز (PBKDF2)، قفل حساب پس از تلاش‌های ناموفق
		services.AddIdentity<AppUser, AppRole>(options =>
		{
			options.Password.RequiredLength = 8;
			options.Password.RequireDigit = true;
			options.Password.RequireUppercase = true;
			options.Password.RequireLowercase = true;
			options.Password.RequireNonAlphanumeric = false;

			options.Lockout.AllowedForNewUsers = true;
			options.Lockout.MaxFailedAccessAttempts = 5;
			options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

			options.User.RequireUniqueEmail = true;
		})
		.AddEntityFrameworkStores<TodoAppDbContext>()
		.AddDefaultTokenProviders();

		// Bind کردن JwtSettings (مقدار Key از User Secrets / Environment Variable تزریق می‌شود)
		services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

		// سرویس تولید Access Token (برای تسک‌های بعدی Login)
		services.AddScoped<IJwtTokenService, JwtTokenService>();
		#endregion
		#region Hangfire (T-025)
		// Storage روی SQL Server (همان Connection String پروژه).
		// جدول‌های HangFire.* در اولین اجرا به‌صورت خودکار ساخته می‌شوند (PrepareSchemaIfNecessary).
		// AddHangfire پیکربندی را بلافاصله روی GlobalConfiguration اعمال می‌کند،
		// بنابراین ثبت RecurringJob در Program.cs بعد از Build امن است.
		services.AddHangfire(config => config
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UseSqlServerStorage(
				configuration.GetConnectionString("TodoAppConnectionString"),
				new SqlServerStorageOptions
				{
					SchemaName = "HangFire",
					PrepareSchemaIfNecessary = true,
					// پاکسازی دوره‌ای Jobs منقضی‌شده تا جداول Hangfire بی‌رون رشد نکنند
					JobExpirationCheckInterval = TimeSpan.FromHours(1)
				}));

		// اجراکننده Job ها (Hosted Service)
		services.AddHangfireServer();
		#endregion
		#region Repositories
		services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
		services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
		services.AddScoped<ITaskListRepository, TaskListRepository>();
		services.AddScoped<ITaskListShareRepository, TaskListShareRepository>();
		services.AddScoped<ITaskItemAttachmentRepository, TaskItemAttachmentRepository>();
		services.AddScoped<ITaskItemRepository, TaskItemRepository>();
		services.AddScoped<ISubtaskRepository, SubtaskRepository>();
		services.AddScoped<INotificationRepository, NotificationRepository>();
		services.AddScoped<IAccountTokenRepository, AccountTokenRepository>();
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		#endregion
		#region BackgroundJobs
		services.AddScoped<CleanupJob>();
		services.AddScoped<DueSoonNotificationWorker>();
		#endregion
		#region Services
		services.AddScoped<AuthService>();
		services.AddScoped<AccountService>();

		services.AddScoped<Business.Users.IUserDirectory, Business.Users.UserDirectory>();
		services.AddScoped<Business.Profiles.IProfileService, Business.Profiles.ProfileService>();
		services.AddScoped<Business.TaskLists.ITaskListService, Business.TaskLists.TaskListService>();
		services.AddScoped<Business.TaskItems.ITaskItemService, Business.TaskItems.TaskItemService>();
		services.AddScoped<Business.Subtasks.ISubtaskService, Business.Subtasks.SubtaskService>();
		services.AddScoped<Business.TaskItemAttachments.ITaskItemAttachmentService, Business.TaskItemAttachments.TaskItemAttachmentService>();
		services.AddScoped<Business.Notifications.INotificationService, Business.Notifications.NotificationService>();
		services.AddScoped<Business.Admin.IAdminService, Business.Admin.AdminService>();
		#endregion
		#region Mapper
		//services.AddAutoMapper(cfg => { },typeof(TodoItemMappingProfile).Assembly);
		#endregion
		#region Validator
		services.AddValidatorsFromAssembly(typeof(LoginDtoValidator).Assembly);
		#endregion
		// قرارداد خطای فرانت برای خطاهای بایند [ApiController]:
		// به‌جای ValidationProblemDetails، پاسخ { message, errors } با کد 400
		services.Configure<ApiBehaviorOptions>(options =>
		{
			options.InvalidModelStateResponseFactory = context =>
			{
				var errors = context.ModelState
					.Where(kvp => kvp.Value is { Errors.Count: > 0 })
					.ToDictionary(
						kvp => kvp.Key,
						kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

				return new BadRequestObjectResult(new { message = "اطلاعات ارسالی معتبر نیست.", errors });
			};
		});


		services.Configure<SecuritySettings>(
			configuration.GetSection(SecuritySettings.SectionName));
		return services;
	}
}
