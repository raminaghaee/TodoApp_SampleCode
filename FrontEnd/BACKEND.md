# راهنمای اتصال فرانت‌اند Todo به بک‌اند محلی (Local Backend)

> **حساب کاربری:** قرارداد جدید و الزامی تغییر نام نمایشی، ایمیل، رمز و بازیابی رمز در [ACCOUNT-BACKEND.md](./ACCOUNT-BACKEND.md) است. این قرارداد برای حساب کاربری بر بخش‌های قدیمی این سند مقدم است: پیشوند واقعی `/api/v1`، شناسه عددی و احراز هویت با کوکی HttpOnly هستند؛ توکن در localStorage ذخیره نمی‌شود.

فرانت‌اند این پروژه **کاملاً از Supabase جدا شده** و مستقیماً با یک REST API اختصاصی کار می‌کند. این سند توضیح می‌دهد چطور پروژه را روی سیستم محلی اجرا کنید، چطور به بک‌اند خودتان وصلش کنید، و قرارداد کامل API را مشخص می‌کند: هر عملیات چه دیتایی از فرانت به بک‌اند می‌رود و چه دیتایی باید برگردد.

---

## ۱) اجرای پروژه روی سیستم محلی

```bash
git clone <repo-url>
cd <project-folder>
npm install        # یا bun install
npm run dev        # اجرا روی http://localhost:8080
```

پیش‌نیاز: Node.js 20+ (یا Bun).

---

## ۲) اتصال به بک‌اند محلی

تنها تنظیم لازم، آدرس بک‌اند است. در فایل `.env` ریشه پروژه:

```env
VITE_API_BASE_URL=http://localhost:5000
```

- آدرس را **بدون** پسوند `/api` بدهید؛ فرانت خودش `/api` را به مسیرها اضافه می‌کند (مثلاً `http://localhost:5000/api/tasks`).
- اگر متغیر ست نشود، مقدار پیش‌فرض `http://localhost:5000` است.
- بک‌اند شما باید **CORS** را برای origin فرانت (مثلاً `http://localhost:8080`) باز کند.

### نحوه کار کلاینت

- کلاینت REST در `src/lib/api.ts` است: توکن JWT را از `localStorage` می‌خواند و با هدر `Authorization: Bearer <token>` به هر درخواست اضافه می‌کند.
- پاسخ `401` از هر endpoint باعث پاک شدن سشن و بازگشت به صفحه ورود می‌شود.
- پاسخ خطا باید JSON با فیلد `message` باشد تا پیام فارسی/قابل‌فهم به کاربر نشان داده شود.
- لایه‌های داده: `src/lib/auth.ts` (احراز هویت)، `src/lib/todo.ts` (لیست‌ها/کارها/اشتراک)، `src/lib/task-details.ts` (زیرتسک/پیوست)، `src/lib/notifications.ts` (اعلان)، `src/lib/admin.ts` (پنل ادمین).

---

## ۳) موجودیت‌ها (Entities) و فیلدهای هر کدام

تمام ID ها `uuid` هستند. تاریخ‌ها ISO 8601 (`timestamptz`) و `due_date` از نوع `date` (فرمت `YYYY-MM-DD`) است.

### profiles (کاربران)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | شناسه کاربر |
| email | text, nullable | ایمیل کاربر |
| display_name | text, nullable | نام نمایشی |
| is_active | boolean, default true | فعال/غیرفعال (پنل ادمین) |
| created_at | timestamptz | |

### user_roles (نقش‌ها)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| user_id | uuid → profiles | |
| role | enum: `super_admin` \| `user` | |

### lists (لیست کارها)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| owner_id | uuid → profiles | مالک لیست |
| name | text | نام لیست |
| color | text, default `'blue'` | یکی از: `blue, teal, violet, amber, rose` |
| created_at | timestamptz | |

### list_shares (اشتراک‌گذاری لیست)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| list_id | uuid → lists | |
| user_id | uuid → profiles | کاربری که لیست با او به اشتراک گذاشته شده |
| created_at | timestamptz | |

### tasks (کارها)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| list_id | uuid → lists | |
| created_by | uuid → profiles | سازنده |
| assigned_to | uuid → profiles, nullable | مسئول انجام (ارجاع) |
| title | text | عنوان |
| notes | text, nullable | یادداشت |
| is_completed | boolean, default false | انجام‌شده |
| is_important | boolean, default false | مهم (ستاره) |
| is_my_day | boolean, default false | روز من |
| due_date | date, nullable | سررسید |
| completed_at | timestamptz, nullable | زمان تکمیل |
| created_at / updated_at | timestamptz | |

### subtasks (زیرتسک‌ها)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| task_id | uuid → tasks | |
| title | text | |
| is_completed | boolean, default false | |
| position | integer, default 0 | ترتیب نمایش |
| created_by | uuid → profiles | |
| created_at / updated_at | timestamptz | |

### task_attachments (پیوست فایل)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| task_id | uuid → tasks | |
| storage_path | text | مسیر فایل در سرور شما |
| file_name | text | نام اصلی فایل |
| mime_type | text, nullable | |
| size_bytes | bigint, nullable | حداکثر ۲۰ مگابایت |
| uploaded_by | uuid → profiles | |
| created_at | timestamptz | |

### notifications (اعلان‌ها)
| فیلد | نوع | توضیح |
|---|---|---|
| id | uuid (PK) | |
| user_id | uuid → profiles | گیرنده |
| type | text, default `'info'` | مثلاً `task_assigned`, `due_soon` |
| title | text | |
| body | text, nullable | |
| task_id | uuid → tasks, nullable | |
| read_at | timestamptz, nullable | null یعنی خوانده‌نشده |
| created_at | timestamptz | |

---

## ۴) قرارداد API — چه چیزی فرانت می‌فرستد و چه چیزی انتظار دارد

همه مسیرها با پیشوند `/api` هستند. همه پاسخ‌ها JSON و همه درخواست‌ها (به‌جز login/register) با هدر `Authorization: Bearer <token>` همراه‌اند.

### احراز هویت
| عملیات | متد و مسیر | بدنه درخواست (فرانت → بک‌اند) | پاسخ مورد انتظار (بک‌اند → فرانت) |
|---|---|---|---|
| ورود | `POST /api/auth/login` | `{ email, password }` | `{ token, user: { id, email, display_name, role } }` |
| ثبت‌نام | `POST /api/auth/register` | `{ email, password, display_name }` | `{ token, user }` — یا اگر تأیید ایمیل لازم است: `{ needs_confirmation: true }` |
| کاربر جاری | `GET /api/auth/me` | — | `{ id, email, display_name, role }` |
| خروج | `POST /api/auth/logout` | — | `204` |

نکته: فیلد `role` در پاسخ login و `/auth/me` برای نمایش لینک پنل سوپرادمین استفاده می‌شود (`super_admin` یا `user`).

### پروفایل‌ها و کاربران
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| لیست کاربران مرتبط | `GET /api/profiles` | — | `Profile[]` (فیلدهای id, email, display_name) |
| جستجوی کاربر با ایمیل دقیق (برای اشتراک‌گذاری) | `GET /api/profiles/lookup?email=...` | query: `email` | `Profile` یا `404` |
| ساخت/به‌روزرسانی پروفایل خودم | `PUT /api/profiles/me` | `{ email, display_name }` | `Profile` |
| (ادمین) همه کاربران + نقش | `GET /api/admin/users` | — | `[{ id, email, display_name, is_active, created_at, role }]` |
| (ادمین) فعال/غیرفعال | `PATCH /api/admin/users/:id` | `{ is_active: boolean }` | `Profile` |
| (ادمین) تغییر نقش | `PUT /api/admin/users/:id/role` | `{ role: "super_admin" \| "user" }` | `{ ok: true }` |

### لیست‌ها
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| لیست‌های من (مالک + اشتراک‌شده) | `GET /api/lists` | — | `List[]` مرتب بر اساس `created_at` صعودی |
| ساخت لیست | `POST /api/lists` | `{ name, color }` | `List` |
| ویرایش لیست | `PATCH /api/lists/:id` | `{ name, color? }` | `List` |
| حذف لیست | `DELETE /api/lists/:id` | — | `204` |
| اشتراک‌گذاری | `POST /api/lists/:id/shares` | `{ user_id }` | `ListShare` |
| حذف اشتراک | `DELETE /api/lists/:id/shares/:userId` | — | `204` |
| همه اشتراک‌های قابل‌مشاهده برای من | `GET /api/shares` | — | `ListShare[]` (برای نمایش نشان «اشتراکی» روی لیست‌ها) |

### کارها (Tasks)
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| همه کارهای قابل‌مشاهده | `GET /api/tasks` | — | `Task[]` مرتب بر اساس `created_at` نزولی |
| ساخت کار | `POST /api/tasks` | `{ list_id, title, is_my_day?, is_important?, due_date?, assigned_to? }` | `Task` |
| ویرایش کار | `PATCH /api/tasks/:id` | هر زیرمجموعه‌ای از فیلدهای tasks (مثل `{ is_completed: true, completed_at }`) | `Task` |
| حذف کار | `DELETE /api/tasks/:id` | — | `204` |

نکته: مرتب‌سازی و صفحه‌بندی فعلاً سمت کلاینت انجام می‌شود؛ برای داده‌های خیلی زیاد می‌توانید query های `?sort=&order=&page=&pageSize=` اضافه کنید و `Toolbar`/`DataPagination` را به آن وصل کنید.

### زیرتسک‌ها
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| زیرتسک‌های یک کار | `GET /api/tasks/:id/subtasks` | — | `Subtask[]` مرتب بر اساس `position` |
| ساخت | `POST /api/tasks/:id/subtasks` | `{ title, position }` | `Subtask` |
| ویرایش/تیک | `PATCH /api/subtasks/:id` | `{ title?, is_completed?, position? }` | `Subtask` |
| حذف | `DELETE /api/subtasks/:id` | — | `204` |

### پیوست‌ها
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| پیوست‌های یک کار | `GET /api/tasks/:id/attachments` | — | `TaskAttachment[]` |
| آپلود | `POST /api/tasks/:id/attachments` | `multipart/form-data` با فیلد `file` (حداکثر ۲۰MB) | `TaskAttachment` |
| دانلود | `GET /api/attachments/:id/download` | — | خود فایل (stream) **یا** JSON به شکل `{ url }` با لینک امضاشده |
| حذف | `DELETE /api/attachments/:id` | — | `204` |

### اعلان‌ها
| عملیات | متد و مسیر | ورودی | خروجی |
|---|---|---|---|
| **(جدید)** اعلان‌های خوانده‌نشده (آیکون زنگ) | `GET /api/v1/notifications/unread` | — | `Notification[]` فقط `read_at == null`، مرتب `created_at` نزولی |
| **(جدید)** تاریخچه کامل (تب اعلان‌های پروفایل) | `GET /api/v1/notifications/all` | — | `Notification[]` خوانده‌شده و خوانده‌نشده، مرتب `created_at` نزولی |
| اعلان‌های من (قدیمی، دیگر استفاده نمی‌شود) | `GET /api/notifications` | — | `Notification[]` |

**تغییرات جدید لازم در بک‌اند:**
1. دو اکشن `[HttpGet("unread")]` و `[HttpGet("all")]` در `NotificationsController` با `[Authorize]`؛ شناسه کاربر فقط از هویت تأییدشده خوانده شود. این دو را **قبل** از `[HttpGet("{id:int}")]` تعریف کنید یا از محدودیت `:int` استفاده کنید تا با شناسه تداخل نکنند.
2. `DELETE /api/v1/notifications/{id}` و `PATCH /api/v1/notifications/{id}/read` فقط برای اعلان متعلق به همان کاربر مجاز باشند؛ در غیر این صورت `404` (اعلان کاربر دیگر افشا نشود).
3. در صورت حجم زیاد، `all` می‌تواند `?page=&pageSize=` بپذیرد؛ فرانت فعلاً کل آرایه را می‌خواند.
4. خطاها به شکل ProblemDetails با `detail` باشند تا در پیام فرانت نمایش داده شوند.
| خوانده‌شدن یک اعلان | `PATCH /api/notifications/:id/read` | — | `Notification` |
| خوانده‌شدن همه | `POST /api/notifications/read-all` | — | `204` |
| حذف اعلان | `DELETE /api/notifications/:id` | — | `204` |
| ساخت (داخلی) | `POST /api/notifications` | `{ type, title, body?, task_id? }` | `Notification` (گیرنده = کاربر جاری) |

قواعدی که بک‌اند شما باید سمت سرور انجام دهد:
- هنگام **ارجاع کار** (`assigned_to` ست یا تغییر کند) → ساخت اعلان `task_assigned` برای گیرنده.
- اعلان `due_soon` برای کارهایی که سررسیدشان کمتر از ۲ روز مانده، فعلاً سمت کلاینت ساخته می‌شود (`src/lib/notifications.ts`)؛ بهتر است به سرور منتقل شود.

---

## ۵) قوانین دسترسی (که بک‌اند شما باید اعمال کند)

- **lists**: کاربر فقط لیست‌هایی را می‌بیند/ویرایش می‌کند که مالک است یا با او share شده. حذف فقط برای مالک.
- **tasks**: دسترسی اگر به لیستِ کار دسترسی داری یا `assigned_to = تو`. ساخت فقط با `created_by = تو`.
- **subtasks / task_attachments**: دسترسی از طریق دسترسی به کارِ والد.
- **list_shares**: ساخت/حذف فقط توسط مالک لیست؛ مشاهده توسط مالک یا خود کاربر share‌شده.
- **notifications**: هر کاربر فقط اعلان‌های خودش؛ ساخت فقط با `user_id = خودش`.
- **profiles**: هر کاربر خودش + هم‌تیمی‌هایش (کسانی که لیست/کار مشترک دارند) را می‌بیند؛ `super_admin` همه را می‌بیند و می‌تواند `is_active` و نقش‌ها را مدیریت کند.
- **user_roles**: خواندن نقشِ خودت برای همه؛ مدیریت نقش‌ها فقط `super_admin`.

---

## ۶) چک‌لیست راه‌اندازی

1. پروژه را clone و اجرا کنید (بخش ۱).
2. بک‌اند خودتان را طبق قرارداد بخش ۴ پیاده کنید و روی لوکال بالا بیاورید.
3. جدول‌ها را طبق بخش ۳ بسازید.
4. `VITE_API_BASE_URL` را در `.env` به آدرس بک‌اند تنظیم کنید.
5. CORS را برای origin فرانت باز کنید.
6. یک کاربر `super_admin` بسازید (درج دستی در `user_roles`) تا پنل مدیریت فعال شود.
