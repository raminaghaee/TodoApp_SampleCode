using FluentValidation;
using Microsoft.Extensions.Options;

namespace TodoApp.Business.Auth;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
	public ChangePasswordValidator(IOptions<SecuritySettings> securityOptions)
	{
		var minLength = securityOptions.Value.PasswordMinLength;

		RuleFor(x => x.CurrentPassword)
			.NotEmpty().WithMessage("رمز فعلی الزامی است.");

		RuleFor(x => x.NewPassword)
			.NotEmpty().WithMessage("رمز جدید الزامی است.")
			.MinimumLength(minLength).WithMessage($"رمز جدید حداقل {minLength} کاراکتر است.")
			.Matches(@"[A-Za-z]").WithMessage("رمز جدید باید حداقل شامل یک حرف باشد.")
			.Matches(@"\d").WithMessage("رمز جدید باید حداقل شامل یک رقم باشد.");
	}
}

