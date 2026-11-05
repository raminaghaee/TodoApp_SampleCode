namespace TodoApp.Business.Auth;


/// <summary>
/// redirect_url به‌صورت آزاد قبول نمی‌شود (محافظت در برابر Open Redirect).
/// قواعد: scheme فقط http/https، بدون userinfo (user:pass@host)، بدون fragment ورودی،
/// origin باید دقیقاً با یکی از اعضای فهرست مجاز یکی باشد و مسیر باید مسیر مجازِ همان جریان باشد.
/// </summary>
public static class RedirectUrlValidator
{
	/// <summary>
	/// در صورت اعتبار، origin مجاز را برمی‌گرداند تا لینک ایمیل با origin مجاز + مسیر مجازِ سمت سرور ساخته شود؛
	/// در غیر این صورت false.
	/// </summary>
	public static bool TryGetAllowedBase(string? redirectUrl,
										 IReadOnlyCollection<string> allowedOrigins,
										 string allowedPath,
										 out string allowedOrigin)
	{
		allowedOrigin = string.Empty;

		if (string.IsNullOrWhiteSpace(redirectUrl) || allowedOrigins.Count == 0)
			return false;

		if (!Uri.TryCreate(redirectUrl, UriKind.Absolute, out var uri))
			return false;

		if (uri.Scheme is not ("http" or "https"))
			return false;

		if (!string.IsNullOrEmpty(uri.UserInfo))   // user:pass@host — ممنوع
			return false;

		if (!string.IsNullOrEmpty(uri.Fragment))   // fragment ورودی بی‌معناست؛ fragment را خود سرور می‌سازد
			return false;

		var origin = $"{uri.Scheme}://{uri.Authority}";
		var isAllowed = allowedOrigins
			.Select(o => o.Trim().TrimEnd('/'))
			.Contains(origin, StringComparer.OrdinalIgnoreCase);

		if (!isAllowed)
			return false;

		// مسیر باید با مسیر مجاز جریان یکی باشد (بدون در نظر گرفتن query)
		if (!string.Equals(uri.AbsolutePath.TrimEnd('/'), allowedPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
			return false;

		allowedOrigin = origin.ToLowerInvariant();
		return true;
	}
}
