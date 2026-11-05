using System.Net;

namespace TodoApp.Business.Auth;

/// <summary>
/// قالب ایمیل‌های حساب کاربری (فارسی/RTL).
/// لینک‌ها مطابق قرارداد فرانت با fragment ساخته می‌شوند:
///   /reset-password#type=recovery&amp;token=ENCODED_TOKEN
///   /confirm-email#token=ENCODED_TOKEN
/// ⚠ توکن/لینک در لاگ سرور، تحلیل‌گرها یا پاسخ API قرار نمی‌گیرد — فقط در خود ایمیل.
/// </summary>
public static class AccountEmailTemplates
{
	private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);

	private static string Wrap(string title, string bodyHtml) => $$"""
		<!doctype html>
		<html dir="rtl" lang="fa">
		<body style="margin:0;padding:24px;background:#f4f5f7;font-family:Tahoma,'Segoe UI',sans-serif;">
		  <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:12px;padding:28px;line-height:2;color:#1f2937;">
			<h2 style="margin:0 0 12px;color:#111827;font-size:20px;">{{title}}</h2>
			{{bodyHtml}}
			<p style="margin-top:28px;padding-top:16px;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;">
			  اگر این درخواست را شما انجام نداده‌اید، می‌توانید این ایمیل را نادیده بگیرید؛ انجام هر تغییر نیازمند رمز عبور فعلی یا کلیک روی لینک از سوی شماست.
			</p>
		  </div>
		</body>
		</html>
		""";

	/// <summary>ایمیل بازیابی رمز — لینک فقط به ایمیل واقعیِ ثبت‌شده ارسال می‌شود.</summary>
	public static string PasswordReset(string displayName, string resetLink, string lifetimeText) => Wrap("بازیابی رمز عبور", $"""
		<p>سلام {HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? "کاربر عزیز" : displayName)}،</p>
		<p>برای بازنشانی رمز عبور روی دکمه زیر بزنید. این لینک تا <b>{lifetimeText}</b> اعتبار دارد و فقط یک‌بار قابل استفاده است.</p>
		<p style="text-align:center;margin:24px 0;">
		  <a href="{resetLink}" style="background:#2563eb;color:#ffffff;padding:10px 28px;border-radius:8px;text-decoration:none;display:inline-block;">بازنشانی رمز عبور</a>
		</p>
		<p style="font-size:12px;color:#6b7280;word-break:break-all;">اگر دکمه کار نکرد، این نشانی را در مرورگر باز کنید:<br/>{resetLink}</p>
		""");

	/// <summary>ایمیل تأیید تغییر ایمیل — به آدرس «جدید» ارسال می‌شود.</summary>
	public static string EmailChangeConfirmation(string displayName, string confirmLink, string lifetimeText) => Wrap("تأیید تغییر ایمیل", $"""
		<p>سلام {HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? "کاربر عزیز" : displayName)}،</p>
		<p>درخواستی برای تغییر ایمیل حساب شما ثبت شده است. برای تأیید آدرس جدید روی دکمه زیر بزنید. این لینک تا <b>{lifetimeText}</b> اعتبار دارد و فقط یک‌بار قابل استفاده است.</p>
		<p style="text-align:center;margin:24px 0;">
		  <a href="{confirmLink}" style="background:#059669;color:#ffffff;padding:10px 28px;border-radius:8px;text-decoration:none;display:inline-block;">تأیید ایمیل جدید</a>
		</p>
		<p style="font-size:12px;color:#6b7280;word-break:break-all;">اگر دکمه کار نکرد، این نشانی را در مرورگر باز کنید:<br/>{confirmLink}</p>
		""");

	/// <summary>هشدار درخواست تغییر ایمیل — به آدرس «قبلی» ارسال می‌شود؛ خطای ارسالش جریان اصلی را نمی‌شکند.</summary>
	public static string EmailChangeNoticeToOldAddress(string currentEmail, string pendingEmail) => Wrap("درخواست تغییر ایمیل حساب کاربری", $"""
		<p>درخواستی برای تغییر ایمیل حساب <b>{HtmlEncode(currentEmail)}</b> به آدرس <b>{HtmlEncode(pendingEmail)}</b> ثبت شده است.</p>
		<p>لینک تأیید به آدرس جدید ارسال شده است؛ تا پیش از تأیید، همین آدرس فعلی و ورود با آن همچنان فعال می‌ماند.</p>
		<p>اگر این درخواست را شما انجام نداده‌اید، لازم نیست کاری کنید؛ بدون رمز عبور فعلی هیچ تغییری قابل تأیید نیست.</p>
		""");
}

