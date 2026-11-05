using FluentValidation;

namespace TodoApp.Business.Auth;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
	public LoginDtoValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("ایمیل الزامی است.")
			.EmailAddress().WithMessage("فرمت ایمیل معتبر نیست.");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("رمز عبور الزامی است.");

		// نکته امنیتی: پیچیدگی رمز فقط در Register اعتبارسنجی می‌شود؛
		// در Login رمزِ ضعیف صرفاً «اشتباه» است و 401 با پیام کلی برمی‌گردد.
	}
}
