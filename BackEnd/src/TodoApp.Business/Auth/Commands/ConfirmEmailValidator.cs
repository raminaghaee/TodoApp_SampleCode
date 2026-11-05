using FluentValidation;

namespace TodoApp.Business.Auth;

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailDto>
{
	public ConfirmEmailValidator()
	{
		RuleFor(x => x.Token)
			.NotEmpty().WithMessage("توکن الزامی است.");
	}
}

