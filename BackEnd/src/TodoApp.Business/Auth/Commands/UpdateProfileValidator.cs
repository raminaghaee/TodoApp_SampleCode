using FluentValidation;

namespace TodoApp.Business.Auth;

// ⚠ سیاست رمز و طول نام را مطابق Validatorهای فعلی پروژه (RegisterDtoValidator و قواعد AppUser) هماهنگ کنید.
// پیش‌فرض این‌جا بر اساس پاسخ شما: حداقل ۸ کاراکتر + حداقل یک حرف و یک رقم.

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
	public UpdateProfileValidator()
	{
		RuleFor(x => x.DisplayName)
			.NotEmpty().WithMessage("نام نمایشی الزامی است.")
			.MinimumLength(2).WithMessage("نام نمایشی حداقل ۲ کاراکتر است.")
			.MaximumLength(100).WithMessage("نام نمایشی حداکثر ۱۰۰ کاراکتر است.");
	}
}
