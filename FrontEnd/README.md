# Shared Todo Master

## ورود نمایشی و اجرای محلی

در اجرای توسعه، ورود سریع مدیر و دو کاربر نمایشی فعال است. حساب‌ها و کارهای نمونه فقط در مرورگر ذخیره می‌شوند و به بک‌اند ارسال نمی‌شوند. رمز همه حساب‌های نمایشی `Demo123!` است.

برای اجرای محلی بدون هیچ داده نمایشی، فایل `.env.example` را به `.env.local` کپی کنید و تنظیمات زیر را قرار دهید:

```env
VITE_USE_MOCK_DATA=false
VITE_API_BASE_URL=http://localhost:5000
```

سپس `npm install` و `npm run dev` را اجرا کنید. پس از تغییر تنظیمات، اجرای برنامه را متوقف و دوباره شروع کنید. `.env.local` بر `.env.development` اولویت دارد. برای فعال‌کردن مجدد نمونه‌ها مقدار `VITE_USE_MOCK_DATA=true` قرار دهید. اگر این متغیر تنظیم نشده یا مقدار دیگری داشته باشد، حالت واقعی انتخاب می‌شود؛ نسخه تولیدی پیش‌فرض نمایشی نیست. تنظیمات `VITE_*` عمومی هستند؛ هیچ رمز یا کلید خصوصی در آن‌ها نگذارید.

در حالت واقعی، دکمه‌های ورود نمایشی حذف می‌شوند و نه حساب‌ها، نه نشست و نه داده‌های نمونه خوانده می‌شوند. برای ورود، حساب موجود در بک‌اند خودتان لازم است. حساب مدیر نمایشی هیچ دسترسی واقعی ایجاد نمی‌کند. پیوست‌های نمایشی فقط تا بازخوانی صفحه در دسترس هستند.

**محدودیت اتصال فعلی:** خاموش‌کردن داده نمایشی، فرانت را به کلاینت REST موجود برمی‌گرداند؛ این کلاینت هنوز با Swagger ارسال‌شده شما تطبیق کامل ندارد (مسیر `/api/v1`، کوکی‌های HttpOnly و مدل دسته/کار). این تغییر به‌تنهایی اتصال عملی به آن بک‌اند را تضمین نمی‌کند. قرارداد فعلی فرانت در `BACKEND.md` آمده، نه قرارداد نهایی Swagger شما. بک‌اند باید CORS مناسب برای آدرس فرانت داشته باشد.

یک پروژه ری اکت با نام Todo ایجاد کن
این پروژه باید مدرن و کاربر پسند و با کاربری آسان و شیک باشه
1)مانند برنامه Todo ماکروسافت پیاده کن
2)امکان تعریف کاربران و لاگین هر کاربر باید وجود داشته باشه
3)امکان ارجاع تسک ها و به اشتراک گذاری تسک ها بین کاربران وجود داشته باشه

This project was built with [Lovable](https://lovable.dev).

## Build with Lovable

Continue developing this project in the [Lovable editor](https://lovable.dev/projects/3fe8d87f-f336-40de-82be-93e13ff3040a).

- **Ship faster**: describe what you want to build and Lovable handles the code.
- **Stay in sync**: every change made in Lovable is committed straight to this repository.
- **Full ownership**: this code is yours. Push to `main` on GitHub and your changes sync back into Lovable, ready for your next prompt.

## Development

Prefer working locally? You need Node.js and npm — [install with nvm](https://github.com/nvm-sh/nvm#installing-and-updating).

```sh
git clone <this-repository-url>
cd <repository-name>
npm i
npm run dev
```
