using FluentValidation;

namespace TodoApp.Business.Auth;

public sealed class ChangeEmailValidator : AbstractValidator<ChangeEmailDto>
{
	public ChangeEmailValidator()
	{
		RuleFor(x => x.NewEmail)
			.NotEmpty().WithMessage("ایمیل جدید الزامی است.")
			.EmailAddress().WithMessage("قالب ایمیل جدید صحیح نیست.")
			.MaximumLength(256).WithMessage("ایمیل حداکثر ۲۵۶ کاراکتر است.");

		RuleFor(x => x.CurrentPassword)
			.NotEmpty().WithMessage("رمز فعلی الزامی است.");

		RuleFor(x => x.RedirectUrl)
			.NotEmpty().WithMessage("آدرس بازگشت (redirect_url) الزامی است.");
	}
}

