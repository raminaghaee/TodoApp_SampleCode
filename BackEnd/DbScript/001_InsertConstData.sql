
/* ۴) Seed نقش‌ها (نام‌های قرارداد فرانت: super_admin / user) ----------------- */

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [Name] = N'super_admin')
	INSERT INTO dbo.Roles ([Name]) VALUES (N'super_admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [Name] = N'user')
	INSERT INTO dbo.Roles ([Name]) VALUES (N'user');