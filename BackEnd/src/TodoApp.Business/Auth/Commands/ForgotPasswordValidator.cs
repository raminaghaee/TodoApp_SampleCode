using FluentValidation;

namespace TodoApp.Business.Auth;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
	public ForgotPasswordValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("ایمیل الزامی است.")
			.EmailAddress().WithMessage("قالب ایمیل صحیح نیست.");

		RuleFor(x => x.RedirectUrl)
			.NotEmpty().WithMessage("آدرس بازگشت (redirect_url) الزامی است.");
	}
}

