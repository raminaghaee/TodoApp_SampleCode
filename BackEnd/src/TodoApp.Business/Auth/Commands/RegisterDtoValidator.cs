using System.Text.RegularExpressions;
using FluentValidation;

namespace TodoApp.Business.Auth;

public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
	// Regex ساده و مقاوم در برابر ReDoS
	private static readonly Regex EmailRegex = new(
		@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase,
		TimeSpan.FromMilliseconds(250));

	public RegisterDtoValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("ایمیل الزامی است.")
			.MaximumLength(256).WithMessage("ایمیل نمی‌تواند بیش از ۲۵۶ کاراکتر باشد.")
			.Must(e => EmailRegex.IsMatch(e))
				.WithMessage("فرمت ایمیل معتبر نیست.");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("رمز عبور الزامی است.")
			.MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.")
			.Matches("[A-Z]").WithMessage("رمز عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.")
			.Matches("[a-z]").WithMessage("رمز عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.")
			.Matches("[0-9]").WithMessage("رمز عبور باید حداقل یک عدد داشته باشد.");
	}
}
