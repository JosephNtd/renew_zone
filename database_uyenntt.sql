-- ============================================================================
--  CÁCH CHẠY
-- ============================================================================
--
--  Cách 1 — SQL Server Management Studio (SSMS):
--     1. Mở file này bằng SSMS.
--     2. Nhấn F5 (Execute). Không cần chọn database trước — script tự tạo và tự chuyển vào.
--
--  Cách 2 — Dòng lệnh:
--     sqlcmd -S localhost -E -C -i database_full.sql
--
--  Sau đó chạy website:
--     dotnet run --project src/RenewZone.Web
--     Web khách:  http://localhost:5216
--     Quản trị:   http://localhost:5216/admin
--
-- ============================================================================
--  TÀI KHOẢN ĐĂNG NHẬP
-- ============================================================================
--
--     Quản trị viên     tài khoản: admin          mật khẩu: Admin@123
--     Khách hàng demo   tài khoản: 0912345678     mật khẩu: Demo@123
--
--  Tài khoản khách hàng demo đã có sẵn 15 đơn hàng ở đủ mọi trạng thái và 2 đánh giá
--  sản phẩm, dùng để xem ngay các màn hình đơn hàng, lịch sử xử lý và đánh giá.
--
-- ============================================================================
--  LƯU Ý QUAN TRỌNG
-- ============================================================================
--
--  1. CHẠY TRÊN DATABASE TRỐNG. Nếu database "RenewZoneClone" đã có dữ liệu, phần
--     schema sẽ được bỏ qua (script idempotent) nhưng phần INSERT sẽ báo lỗi trùng
--     khóa chính. Muốn nạp lại từ đầu thì xóa sạch trước:
--
--         DROP DATABASE [RenewZoneClone];
--
--  2. ẢNH SẢN PHẨM NẰM Ở ĐĨA, KHÔNG NẰM TRONG DATABASE. Bảng ProductImages chỉ lưu
--     đường dẫn dạng /uploads/products/... Muốn thấy ảnh thật thì chép kèm thư mục
--     src/RenewZone.Web/wwwroot/uploads/ (khoảng 7,7 MB). Thiếu thư mục này website
--     vẫn chạy bình thường, chỉ hiển thị ảnh SVG mặc định thay cho ảnh thật.
--
--  3. Toàn bộ thông tin liên hệ trong dữ liệu là GIẢ LẬP phục vụ học tập
--     (số điện thoại 09xx, email @demo.local). Đây là dự án học tập, không dùng
--     thông tin thật của bất kỳ cá nhân hay doanh nghiệp nào.
--
--  4. Muốn đổi tên database: sửa chữ RenewZoneClone ở khối "BƯỚC 1" ngay bên dưới,
--     rồi sửa chuỗi kết nối trong src/RenewZone.Web/appsettings.json cho khớp.
--
--  5. Trên hosting dùng chung (không có quyền tạo database): xóa hẳn khối "BƯỚC 1",
--     tự tạo database trong bảng điều khiển của nhà cung cấp rồi chạy từ "BƯỚC 2".
--
-- ============================================================================


-- ============================================================================
--  BƯỚC 0 — TÙY CHỌN PHIÊN LÀM VIỆC (BẮT BUỘC, ĐỪNG XÓA)
-- ============================================================================
--  Bảng AspNetUsers có filtered index, mà tạo filtered index đòi hỏi QUOTED_IDENTIFIER
--  phải BẬT. SSMS mặc định bật sẵn, nhưng sqlcmd thì mặc định TẮT — thiếu hai dòng này
--  thì chạy bằng sqlcmd sẽ lỗi "Msg 1934" và toàn bộ phần tạo bảng bị hủy bỏ.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


-- ============================================================================
--  BƯỚC 1 — TẠO DATABASE
-- ============================================================================
IF DB_ID(N'RenewZoneClone') IS NULL
BEGIN
    CREATE DATABASE [RenewZoneClone];
    PRINT N'Da tao database RenewZoneClone.';
END
ELSE
    PRINT N'Database RenewZoneClone da ton tai - dung lai database nay.';
GO

USE [RenewZoneClone];
GO

-- Đặt lại một lần nữa sau khi đã vào đúng database, phòng trường hợp công cụ chạy
-- script mở kết nối mới cho mỗi lô lệnh.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


-- ============================================================================
--  BƯỚC 2 — TẠO CẤU TRÚC BẢNG (sinh từ EF Core migrations, chạy lại được nhiều lần)
-- ============================================================================
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [Banners] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [LinkUrl] nvarchar(500) NULL,
        [Position] int NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Slug] nvarchar(150) NOT NULL,
        [ParentId] int NULL,
        [ImageUrl] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Address] nvarchar(500) NULL,
        [Email] nvarchar(150) NULL,
        [Subject] nvarchar(250) NULL,
        [Content] nvarchar(4000) NOT NULL,
        [AttachmentPath] nvarchar(500) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [NewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NOT NULL,
        [Slug] nvarchar(250) NOT NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [Excerpt] nvarchar(500) NULL,
        [ContentHtml] nvarchar(max) NOT NULL,
        [CategoryTag] nvarchar(100) NULL,
        [IsFeatured] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [PublishedAt] datetime2 NOT NULL,
        [ViewCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_NewsArticles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [CustomerName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Email] nvarchar(150) NULL,
        [Address] nvarchar(500) NOT NULL,
        [Note] nvarchar(1000) NULL,
        [PaymentMethod] int NOT NULL,
        [Status] int NOT NULL,
        [TotalAmount] decimal(18,0) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Key])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [StaticPages] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NOT NULL,
        [Slug] nvarchar(250) NOT NULL,
        [ContentHtml] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_StaticPages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [Testimonials] (
        [Id] int NOT NULL IDENTITY,
        [CustomerName] nvarchar(150) NOT NULL,
        [AvatarUrl] nvarchar(500) NULL,
        [Content] nvarchar(2000) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Slug] nvarchar(250) NOT NULL,
        [OriginalPrice] decimal(18,0) NOT NULL,
        [SalePrice] decimal(18,0) NULL,
        [IsContactPrice] bit NOT NULL,
        [ShortDescription] nvarchar(500) NULL,
        [DescriptionHtml] nvarchar(max) NULL,
        [SpecsHtml] nvarchar(max) NULL,
        [IsInstallment0Percent] bit NOT NULL,
        [IsFeatured] bit NOT NULL,
        [IsBestSeller] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [ViewCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NULL,
        [ProductName] nvarchar(250) NOT NULL,
        [VariantDescription] nvarchar(500) NULL,
        [UnitPrice] decimal(18,0) NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductOptionGroups] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ProductOptionGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductOptionGroups_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductOptionValues] (
        [Id] int NOT NULL IDENTITY,
        [GroupId] int NOT NULL,
        [Value] nvarchar(100) NOT NULL,
        [PriceAdjustment] decimal(18,0) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsDefault] bit NOT NULL,
        CONSTRAINT [PK_ProductOptionValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductOptionValues_ProductOptionGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [ProductOptionGroups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_IsRead] ON [ContactMessages] ([IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_NewsArticles_PublishedAt] ON [NewsArticles] ([PublishedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NewsArticles_Slug] ON [NewsArticles] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_Code] ON [Orders] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductOptionGroups_ProductId] ON [ProductOptionGroups] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductOptionValues_GroupId] ON [ProductOptionValues] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId_IsActive] ON [Products] ([CategoryId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaticPages_Slug] ON [StaticPages] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720034348_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720034348_InitialCreate', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NewsArticles]') AND [c].[name] = N'CategoryTag');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [NewsArticles] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [NewsArticles] DROP COLUMN [CategoryTag];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    ALTER TABLE [NewsArticles] ADD [CategoryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    ALTER TABLE [Categories] ADD [IconClass] nvarchar(60) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE TABLE [ContentBlocks] (
        [Id] int NOT NULL IDENTITY,
        [GroupKey] nvarchar(60) NOT NULL,
        [Icon] nvarchar(60) NULL,
        [Title] nvarchar(200) NOT NULL,
        [Subtitle] nvarchar(300) NULL,
        [LinkUrl] nvarchar(300) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ContentBlocks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [Group] int NOT NULL,
        [Type] int NOT NULL,
        [Text] nvarchar(100) NOT NULL,
        [Url] nvarchar(300) NULL,
        [Icon] nvarchar(60) NULL,
        [ParentId] int NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [OpenInNewTab] bit NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE TABLE [NewsCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_NewsCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE INDEX [IX_NewsArticles_CategoryId] ON [NewsArticles] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE INDEX [IX_ContentBlocks_GroupKey_DisplayOrder] ON [ContentBlocks] ([GroupKey], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE INDEX [IX_MenuItems_Group_DisplayOrder] ON [MenuItems] ([Group], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NewsCategories_Slug] ON [NewsCategories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    ALTER TABLE [NewsArticles] ADD CONSTRAINT [FK_NewsArticles_NewsCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [NewsCategories] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722050409_Phase5Content'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722050409_Phase5Content', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Testimonials] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Testimonials] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [StaticPages] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [StaticPages] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Products] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Products] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [NewsCategories] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [NewsCategories] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [NewsArticles] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [NewsArticles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [MenuItems] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [MenuItems] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [ContentBlocks] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [ContentBlocks] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Categories] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Categories] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Banners] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    ALTER TABLE [Banners] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722143701_Phase6SoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722143701_Phase6SoftDelete', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723061948_Phase7OrderHistory'
)
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [FromStatus] int NULL,
        [ToStatus] int NOT NULL,
        [Note] nvarchar(500) NULL,
        [ChangedBy] nvarchar(150) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723061948_Phase7OrderHistory'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId_ChangedAt] ON [OrderStatusHistories] ([OrderId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723061948_Phase7OrderHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723061948_Phase7OrderHistory', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093150_Phase8Reviews'
)
BEGIN
    ALTER TABLE [Products] ADD [AvgRating] decimal(3,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093150_Phase8Reviews'
)
BEGIN
    ALTER TABLE [Products] ADD [ReviewCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093150_Phase8Reviews'
)
BEGIN
    CREATE TABLE [ProductReviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [AuthorName] nvarchar(150) NOT NULL,
        [Rating] int NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [AdminReply] nvarchar(2000) NULL,
        [RepliedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductReviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093150_Phase8Reviews'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_ProductId_Status] ON [ProductReviews] ([ProductId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093150_Phase8Reviews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724093150_Phase8Reviews', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011539_Phase8ReviewPhone'
)
BEGIN
    ALTER TABLE [ProductReviews] ADD [Phone] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011539_Phase8ReviewPhone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725011539_Phase8ReviewPhone', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [ProductReviews] ADD [IsVerifiedPurchase] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [ProductReviews] ADD [UserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [Orders] ADD [UserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DateOfBirth] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Gender] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE TABLE [CustomerAddresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Label] nvarchar(50) NULL,
        [ReceiverName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [AddressLine] nvarchar(500) NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerAddresses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE TABLE [UserCarts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ItemsJson] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserCarts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserCarts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE TABLE [WishlistItems] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WishlistItems_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_UserId_ProductId] ON [ProductReviews] ([UserId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId_CreatedAt] ON [Orders] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_UserId] ON [CustomerAddresses] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserCarts_UserId] ON [UserCarts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems] ([UserId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    ALTER TABLE [ProductReviews] ADD CONSTRAINT [FK_ProductReviews_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728071549_Phase9Accounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728071549_Phase9Accounts', N'9.0.13');
END;

COMMIT;
GO

PRINT N'BUOC 2 hoan tat: da tao du 29 bang.';
GO


-- ============================================================================
--  BƯỚC 3 — NẠP DỮ LIỆU
-- ============================================================================
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------------
-- [AspNetRoles] - 2 dong
-- ----------------------------------------------------------------------
INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES
(N'c8d0b987-e1b0-468f-98d9-46f842725440', N'Customer', N'CUSTOMER', NULL),
(N'e93e123f-b5ca-4387-86fd-235c213cb018', N'Admin', N'ADMIN', NULL);
GO

-- ----------------------------------------------------------------------
-- [AspNetUsers] - 2 dong
-- ----------------------------------------------------------------------
INSERT INTO [dbo].[AspNetUsers] ([Id], [FullName], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [CreatedAt], [DateOfBirth], [Gender]) VALUES
(N'2cc8628c-5df4-465b-a6e5-d8d061c72859', N'Quản trị viên', N'admin', N'ADMIN', N'admin@renewzone-demo.local', N'ADMIN@RENEWZONE-DEMO.LOCAL', 1, N'AQAAAAIAAYagAAAAEIiAX7v3e65xq6aYi7QqnokFgqPCAKNRV+DjypdcN/sVzF5MzwISeLlH896zea2nHg==', N'63C8ECD3-5D06-41B2-81B5-7678084A92DC', N'c9650539-f9d1-479d-a280-15a842146469', NULL, 0, 0, NULL, 1, 0, '2026-07-28T15:08:20.320', NULL, NULL),
(N'b5fef7df-336a-4f5b-8d74-13017610d958', N'Dat', N'0912345678', N'0912345678', N'khachhang@demo.local', N'KHACHHANG@DEMO.LOCAL', 0, N'AQAAAAIAAYagAAAAEKYaBcGMNfRjMVNXI1z7Mu8mc51nS4K5PYYzktaE5JiK65hxWWrBFDtRod/4NxvKwg==', N'BM6MEMMNWBOWPYJATHTXGNTYC5RSAG2Q', N'4d6138f3-9974-43cf-ba02-3acdcd41ea57', N'0912345678', 0, 0, NULL, 1, 0, '2026-07-28T15:29:06.885', NULL, NULL);
GO

-- ----------------------------------------------------------------------
-- [AspNetUserRoles] - 2 dong
-- ----------------------------------------------------------------------
INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES
(N'b5fef7df-336a-4f5b-8d74-13017610d958', N'c8d0b987-e1b0-468f-98d9-46f842725440'),
(N'2cc8628c-5df4-465b-a6e5-d8d061c72859', N'e93e123f-b5ca-4387-86fd-235c213cb018');
GO

-- [AspNetUserClaims]: bang rong, khong co du lieu

-- [AspNetUserLogins]: bang rong, khong co du lieu

-- [AspNetUserTokens]: bang rong, khong co du lieu

-- [AspNetRoleClaims]: bang rong, khong co du lieu

-- ----------------------------------------------------------------------
-- [Categories] - 18 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [ParentId], [ImageUrl], [DisplayOrder], [IsActive], [CreatedAt], [UpdatedAt], [IconClass], [DeletedAt], [IsDeleted]) VALUES
(1, N'iPhone', N'iphone', NULL, N'/uploads/editor/2026-07/417425f196704abfbdc0eb83ffc4ee6e.jpg', 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:27:42.310', N'bi-phone', NULL, 0),
(2, N'MacBook', N'macbook', NULL, N'/uploads/editor/2026-07/3a9a0aeb89c4492eb8e496271293b0b0.jpg', 2, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:28:33.123', N'bi-laptop', NULL, 0),
(3, N'iPad', N'ipad', NULL, N'/uploads/editor/2026-07/af4b9d334bf246aebbbf72b7be0fa367.jpg', 3, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:30:58.621', N'bi-tablet', NULL, 0),
(4, N'Apple Watch', N'apple-watch', NULL, N'/uploads/editor/2026-07/b563dce25db04abdb421a1d05959ba71.jpg', 4, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:32:28.562', N'bi-smartwatch', NULL, 0),
(5, N'AirPods', N'airpods', NULL, N'/uploads/editor/2026-07/f1ddf4de8846470f97ee29021070ff1a.jpg', 5, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:33:48.885', N'bi-earbuds', NULL, 0),
(6, N'Hàng thanh lý', N'hang-thanh-ly', NULL, N'/uploads/editor/2026-07/c0b7d44e86c645d9b88b4a24ea23ace6.jpg', 6, 1, '2026-07-20T10:44:03.123', '2026-07-24T11:35:12.456', N'bi-box-seam', NULL, 0),
(12, N'iPhone 11 Series', N'iphone-11-series', 1, NULL, 0, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:16.641', NULL, NULL, 0),
(13, N'iPhone 12 Series', N'iphone-12-series', 1, NULL, 1, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:17.894', NULL, NULL, 0),
(14, N'iPhone 13 Series', N'iphone-13-series', 1, NULL, 2, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:17.894', NULL, NULL, 0),
(15, N'iPhone 14 Series', N'iphone-14-series', 1, NULL, 3, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:02.758', NULL, NULL, 0),
(16, N'iPhone 15 Series', N'iphone-15-series', 1, NULL, 4, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:02.758', NULL, NULL, 0),
(17, N'iPhone 16 Series', N'iphone-16-series', 1, NULL, 5, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:02.758', NULL, NULL, 0),
(18, N'iPhone 17 Series', N'iphone-17-series', 1, NULL, 6, 1, '2026-07-20T10:44:03.123', '2026-07-25T11:21:02.758', NULL, NULL, 0),
(7, N'MacBook Air', N'macbook-air', 2, NULL, 1, 1, '2026-07-20T10:44:03.123', NULL, NULL, NULL, 0),
(8, N'MacBook Pro', N'macbook-pro', 2, NULL, 2, 1, '2026-07-20T10:44:03.123', NULL, NULL, NULL, 0),
(9, N'iPad Pro', N'ipad-pro', 3, NULL, 1, 1, '2026-07-20T10:44:03.123', NULL, NULL, NULL, 0),
(10, N'iPad Air', N'ipad-air', 3, NULL, 2, 1, '2026-07-20T10:44:03.123', NULL, NULL, NULL, 0),
(11, N'iPad Mini', N'ipad-mini', 3, NULL, 3, 1, '2026-07-20T10:44:03.123', NULL, NULL, NULL, 0);
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

-- ----------------------------------------------------------------------
-- [Products] - 23 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Products] ON;
INSERT INTO [dbo].[Products] ([Id], [CategoryId], [Name], [Slug], [OriginalPrice], [SalePrice], [IsContactPrice], [ShortDescription], [DescriptionHtml], [SpecsHtml], [IsInstallment0Percent], [IsFeatured], [IsBestSeller], [IsActive], [ViewCount], [CreatedAt], [UpdatedAt], [DeletedAt], [IsDeleted], [AvgRating], [ReviewCount]) VALUES
(1, 3, N'iPad Gen 10 10.9 inch WiFi 64GB', N'ipad-gen-10-wifi-64gb', 12990000, 9000000, 0, N'iPad Gen 10 WiFi 64GB - thiết kế phẳng cạnh hiện đại, cổng USB-C, phù hợp học tập và giải trí hàng ngày.', N'<p><strong>iPad thế hệ 10</strong> l&agrave;m mới ho&agrave;n to&agrave;n thiết kế với viền phẳng, m&agrave;u sắc trẻ trung v&agrave; cổng sạc USB-C thay cho Lightning, mang lại trải nghiệm hiện đại hơn hẳn c&aacute;c đời iPad phổ th&ocirc;ng trước đ&oacute;.</p>
<p>Với mức gi&aacute; dễ tiếp cận, đ&acirc;y l&agrave; lựa chọn ph&ugrave; hợp cho học sinh, sinh vi&ecirc;n cần m&aacute;y t&iacute;nh bảng học tập, xem phim, đọc s&aacute;ch hay lướt web mượt m&agrave;.</p>
<p>M&aacute;y ch&iacute;nh h&atilde;ng, kiểm tra kỹ, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina 10.9"</li>
<li><strong>Chip:</strong> Apple A14 Bionic</li>
<li><strong>Bộ nhớ:</strong> 64GB, kết nối Wi-Fi</li>
<li><strong>Camera:</strong> 12MP sau, 12MP trước Ultra Wide (Center Stage)</li>
<li><strong>Kh&aacute;c:</strong> Cổng USB-C, hỗ trợ Apple Pencil (thế hệ 1 qua adapter)</li>
</ul>', 1, 0, 1, 1, 19, '2026-07-20T10:44:03.123', '2026-07-28T15:12:15.577', NULL, 0, 0.00, 0),
(2, 4, N'Apple Watch Series 10 46mm GPS', N'apple-watch-series-10-46mm-gps', 11900000, 900000, 0, N'Apple Watch Series 10 46mm GPS - màn hình lớn hơn, mỏng hơn, theo dõi sức khỏe toàn diện.', N'<p><strong>Apple Watch Series 10</strong> c&oacute; thiết kế mỏng nhất từ trước đến nay, m&agrave;n h&igrave;nh OLED lớn hơn nh&igrave;n r&otilde; ở mọi g&oacute;c độ, c&ugrave;ng loạt t&iacute;nh năng theo d&otilde;i sức khỏe như đo nồng độ oxy trong m&aacute;u, điện t&acirc;m đồ ECG v&agrave; ph&aacute;t hiện ngưng thở khi ngủ.</p>
<p>Ph&ugrave; hợp cho người tập luyện thể thao h&agrave;ng ng&agrave;y lẫn nhu cầu theo d&otilde;i sức khỏe chủ động, đồng bộ mượt m&agrave; với iPhone.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> LTPO OLED lu&ocirc;n bật (Always-On), lớn hơn v&agrave; mỏng hơn thế hệ trước</li>
<li><strong>Chip:</strong> Apple S10</li>
<li><strong>Sức khỏe:</strong> Đo oxy m&aacute;u, điện t&acirc;m đồ ECG, ph&aacute;t hiện ngưng thở khi ngủ, đo nhiệt độ</li>
<li><strong>Kết nối:</strong> GPS, Wi-Fi, Bluetooth 5.3</li>
<li><strong>Kh&aacute;c:</strong> Chống nước 50m, sạc nhanh, d&acirc;y đeo tương th&iacute;ch rộng</li>
</ul>', 1, 0, 1, 1, 2, '2026-07-20T10:44:03.123', '2026-07-24T12:26:18.195', NULL, 0, 0.00, 0),
(3, 4, N'Apple Watch Ultra 2 49mm', N'apple-watch-ultra-2-49mm', 21990000, 18400000, 0, N'Apple Watch Ultra 2 49mm - màn hình sáng nhất, pin bền nhất, dành cho thể thao mạo hiểm và lặn biển.', N'<p><strong>Apple Watch Ultra 2</strong> l&agrave; chiếc đồng hồ bền bỉ nhất của Apple, khung Titan chống va đập, m&agrave;n h&igrave;nh s&aacute;ng tới 3000 nits nh&igrave;n r&otilde; ngay cả dưới &aacute;nh nắng gắt, thời lượng pin l&ecirc;n đến 36 giờ cho c&aacute;c chuyến đi d&agrave;i ng&agrave;y.</p>
<p>Trang bị GPS k&eacute;p độ ch&iacute;nh x&aacute;c cao v&agrave; khả năng chịu độ s&acirc;u lặn tới 100m, đ&acirc;y l&agrave; lựa chọn h&agrave;ng đầu cho người chơi thể thao mạo hiểm, chạy trail, lặn biển hoặc leo n&uacute;i.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> LTPO OLED, độ s&aacute;ng tối đa 3000 nits, k&iacute;nh Sapphire</li>
<li><strong>Chip:</strong> Apple S9</li>
<li><strong>Pin:</strong> L&ecirc;n đến 36 giờ sử dụng b&igrave;nh thường, tới 72 giờ ở chế độ tiết kiệm pin</li>
<li><strong>Kết nối:</strong> GPS k&eacute;p ch&iacute;nh x&aacute;c cao, Wi-Fi, Bluetooth 5.3, Cellular (t&ugrave;y bản)</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan, chống nước đến 100m, n&uacute;t Action t&ugrave;y chỉnh</li>
</ul>', 1, 0, 0, 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T12:27:36.092', NULL, 0, 0.00, 0),
(4, 5, N'AirPods Pro 2 (USB-C)', N'airpods-pro-2-usb-c', 6790000, 5190000, 0, N'AirPods Pro 2 (USB-C) - chống ồn chủ động thế hệ mới, âm thanh không gian, sạc nhanh qua USB-C.', N'<p><strong>AirPods Pro 2</strong> bản USB-C mang chống ồn chủ động (ANC) mạnh gấp đ&ocirc;i thế hệ trước, chế độ Adaptive Audio tự động điều chỉnh &acirc;m lượng v&agrave; tiếng ồn xung quanh theo thời gian thực.</p>
<p>&Acirc;m thanh kh&ocirc;ng gian c&aacute; nh&acirc;n h&oacute;a (Personalized Spatial Audio) mang lại trải nghiệm nghe nhạc, xem phim sống động như rạp h&aacute;t, trong khi hộp sạc hỗ trợ USB-C tiện lợi hơn khi d&ugrave;ng chung d&acirc;y với iPhone, iPad, MacBook đời mới.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>Chống ồn:</strong> Chủ động (ANC) thế hệ 2, chế độ Xuy&ecirc;n &acirc;m th&iacute;ch ứng</li>
<li><strong>&Acirc;m thanh:</strong> Kh&ocirc;ng gian c&aacute; nh&acirc;n h&oacute;a, tự động điều chỉnh theo chuyển động đầu</li>
<li><strong>Chống nước:</strong> IP54 (tai nghe v&agrave; hộp sạc)</li>
<li><strong>Pin:</strong> L&ecirc;n đến 6 giờ nghe nhạc (ANC bật), 30 giờ k&egrave;m hộp sạc</li>
<li><strong>Kh&aacute;c:</strong> Hộp sạc USB-C, loa b&aacute;o định vị, cảm ứng lực nhấn điều khiển</li>
</ul>', 1, 0, 1, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T12:29:44.910', NULL, 0, 0.00, 0),
(5, 5, N'AirPods 4', N'airpods-4', 399000000, 329000000, 0, N'AirPods 4 - thiết kế hoàn toàn mới, âm thanh cải thiện rõ rệt, thoải mái đeo cả ngày.', N'<p><strong>AirPods 4</strong> được thiết kế lại từ đầu với d&aacute;ng tai nghe &ocirc;m s&aacute;t hơn, mang lại cảm gi&aacute;c thoải m&aacute;i hơn khi đeo trong thời gian d&agrave;i, đồng thời cải thiện chất lượng &acirc;m thanh v&agrave; khả năng thu giọng khi gọi điện.</p>
<p>L&agrave; lựa chọn hợp l&yacute; cho người d&ugrave;ng cần một chiếc tai nghe true wireless gọn nhẹ, dễ d&ugrave;ng, tương th&iacute;ch tốt với hệ sinh th&aacute;i Apple.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>Thiết kế:</strong> Kiểu d&aacute;ng mới, &ocirc;m tai thoải m&aacute;i hơn thế hệ trước</li>
<li><strong>&Acirc;m thanh:</strong> Driver mới, khử tiếng ồn khi đ&agrave;m thoại</li>
<li><strong>Pin:</strong> L&ecirc;n đến 5 giờ nghe nhạc, 30 giờ k&egrave;m hộp sạc</li>
<li><strong>Kết nối:</strong> Bluetooth 5.3, gh&eacute;p nối nhanh với thiết bị Apple</li>
<li><strong>Kh&aacute;c:</strong> Cảm ứng điều khiển tr&ecirc;n th&acirc;n tai nghe, hộp sạc USB-C</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T12:30:50.271', NULL, 0, 0.00, 0),
(6, 6, N'iPhone 12 64GB - Thanh lý 97%', N'iphone-12-64gb-thanh-ly', 8990000, 4990000, 0, N'iPhone 12 64GB - hàng thanh lý 97%, đã kiểm tra kỹ pin và ngoại hình, giá cực tốt cho người dùng tiết kiệm.', N'<p><strong>iPhone 12 64GB</strong> h&agrave;ng thanh l&yacute; được Apple Store Uyên NTT kiểm tra kỹ về pin, m&agrave;n h&igrave;nh v&agrave; c&aacute;c cảm biến trước khi l&ecirc;n kệ, ngoại h&igrave;nh đạt t&igrave;nh trạng 97% - vết xước rất nhỏ kh&oacute; nhận thấy khi sử dụng b&igrave;nh thường.</p>
<p>M&aacute;y vẫn hỗ trợ MagSafe, camera k&eacute;p chụp ảnh tốt v&agrave; chip A14 Bionic đủ mượt cho c&aacute;c nhu cầu h&agrave;ng ng&agrave;y, l&agrave; lựa chọn tiết kiệm nếu bạn muốn d&ugrave;ng iPhone m&agrave;n OLED với chi ph&iacute; thấp.</p>
<p>H&agrave;ng thanh l&yacute; c&oacute; bảo h&agrave;nh ngắn hạn tại cửa h&agrave;ng, hỗ trợ đổi trả trong thời gian quy định nếu ph&aacute;t sinh lỗi kỹ thuật.</p>', N'<ul>
<li><strong>T&igrave;nh trạng:</strong> Thanh l&yacute; 97%, đ&atilde; kiểm tra pin &amp; ngoại h&igrave;nh</li>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.1"</li>
<li><strong>Chip:</strong> Apple A14 Bionic</li>
<li><strong>Camera sau:</strong> K&eacute;p 12MP (Wide + Ultra Wide)</li>
<li><strong>Kh&aacute;c:</strong> Hỗ trợ MagSafe, chống nước IP68, cổng Lightning</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(7, 6, N'MacBook Air M1 - Thanh lý 96%', N'macbook-air-m1-thanh-ly', 13990000, 10490000, 0, N'MacBook Air M1 - hàng thanh lý 96%, pin đã kiểm tra dung lượng, hiệu năng M1 vẫn mượt cho công việc văn phòng.', N'<p><strong>MacBook Air M1</strong> h&agrave;ng thanh l&yacute; được kiểm tra kỹ t&igrave;nh trạng pin (dung lượng pin c&ograve;n lại được đo v&agrave; c&ocirc;ng bố), b&agrave;n ph&iacute;m, bản lề v&agrave; ngoại h&igrave;nh trước khi b&aacute;n, đạt t&igrave;nh trạng thẩm mỹ 96%.</p>
<p>Hiệu năng chip M1 vẫn đ&aacute;p ứng tốt c&aacute;c t&aacute;c vụ văn ph&ograve;ng, học tập, lướt web mượt m&agrave; nhờ tối ưu phần cứng - phần mềm của Apple, l&agrave; lựa chọn tiết kiệm để sở hữu MacBook ch&iacute;nh h&atilde;ng.</p>
<p>H&agrave;ng thanh l&yacute; c&oacute; bảo h&agrave;nh ngắn hạn tại cửa h&agrave;ng, hỗ trợ đổi trả nếu ph&aacute;t sinh lỗi kỹ thuật trong thời gian quy định.</p>', N'<ul>
<li><strong>T&igrave;nh trạng:</strong> Thanh l&yacute; 96%, đ&atilde; đo dung lượng pin c&ograve;n lại</li>
<li><strong>M&agrave;n h&igrave;nh:</strong> Retina 13.3"</li>
<li><strong>Chip:</strong> Apple M1, 8-nh&acirc;n CPU / 7-nh&acirc;n GPU</li>
<li><strong>RAM &amp; lưu trữ:</strong> 8GB hợp nhất / 256GB SSD</li>
<li><strong>Kh&aacute;c:</strong> Thiết kế fanless, Touch ID, 2 cổng Thunderbolt/USB4</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T13:41:40.009', NULL, 0, 0.00, 0),
(8, 17, N'iPhone 16 Pro Max 256GB', N'iphone-16-pro-max-256gb', 34990000, 30490000, 0, N'iPhone 16 Pro Max 256GB - chip A18 Pro, camera Telephoto 5x, khung Titan bền bỉ. Bảo hành dài hạn, hỗ trợ trả góp 0%.', N'<p><strong>iPhone 16 Pro Max</strong> l&agrave; flagship mạnh nhất của Apple hiện tại, sở hữu khung Titan cấp 5 nhẹ v&agrave; bền hơn thế hệ trước, c&ugrave;ng m&agrave;n h&igrave;nh Super Retina XDR 6.9 inch lớn nhất từ trước đến nay tr&ecirc;n d&ograve;ng Pro.</p>
<p>Chip A18 Pro cho hiệu năng vượt trội, xử l&yacute; mượt c&aacute;c t&aacute;c vụ AI tr&ecirc;n m&aacute;y (Apple Intelligence), trong khi cụm camera 48MP với ống Telephoto 5x gi&uacute;p chụp xa sắc n&eacute;t hơn hẳn. N&uacute;t Camera Control mới cho ph&eacute;p chụp ảnh nhanh chỉ với một chạm.</p>
<p>M&aacute;y tại Apple Store Uyên NTT được kiểm tra 90 điểm trước khi giao, đi k&egrave;m bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0% v&agrave; thu cũ đổi mới trợ gi&aacute; cao cho kh&aacute;ch n&acirc;ng cấp.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.9", ProMotion 120Hz, Always-On, độ s&aacute;ng tối đa 2000 nits</li>
<li><strong>Chip:</strong> Apple A18 Pro, Neural Engine 16 nh&acirc;n, hỗ trợ Apple Intelligence</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 48MP + Telephoto 12MP (zoom quang 5x)</li>
<li><strong>Camera trước:</strong> TrueDepth 12MP</li>
<li><strong>Pin &amp; sạc:</strong> Sử dụng cả ng&agrave;y, sạc nhanh 20W, MagSafe 25W, sạc kh&ocirc;ng d&acirc;y Qi2</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan cấp 5, chống nước IP68, n&uacute;t Action + Camera Control, cổng USB-C</li>
</ul>', 1, 1, 1, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 4.50, 2),
(9, 16, N'iPhone 15 Pro Max 256GB', N'iphone-15-pro-max-256gb', 29990000, 20990000, 0, N'iPhone 15 Pro Max 256GB - camera Telephoto tetraprism 5x, khung Titan, chip A17 Pro. Đã qua kiểm định kỹ, giá tốt.', N'<p><strong>iPhone 15 Pro Max</strong> l&agrave; chiếc iPhone đầu ti&ecirc;n d&ugrave;ng khung Titan, nhẹ hơn thế hệ th&eacute;p kh&ocirc;ng gỉ trước đ&oacute; nhưng vẫn giữ độ bền cao, kết hợp m&agrave;n h&igrave;nh Super Retina XDR 6.7 inch ProMotion 120Hz sắc n&eacute;t.</p>
<p>Chip A17 Pro c&ugrave;ng GPU 6 nh&acirc;n mang lại hiệu năng chơi game v&agrave; dựng h&igrave;nh mạnh mẽ, trong khi ống k&iacute;nh Telephoto tetraprism cho zoom quang học 5x - lần đầu xuất hiện tr&ecirc;n iPhone.</p>
<p>M&aacute;y được ph&acirc;n loại t&igrave;nh trạng minh bạch (99%/98%), test pin v&agrave; c&aacute;c cảm biến trước khi l&ecirc;n kệ, bảo h&agrave;nh d&agrave;i hạn k&egrave;m hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.7", ProMotion 120Hz, Dynamic Island</li>
<li><strong>Chip:</strong> Apple A17 Pro, GPU 6 nh&acirc;n</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 12MP + Telephoto 12MP (tetraprism, zoom quang 5x)</li>
<li><strong>Camera trước:</strong> TrueDepth 12MP</li>
<li><strong>Pin &amp; sạc:</strong> Cả ng&agrave;y sử dụng, sạc nhanh 20W, MagSafe, cổng USB-C</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan, chống nước IP68, n&uacute;t Action Button</li>
</ul>', 1, 0, 1, 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T13:49:17.374', NULL, 0, 0.00, 0),
(10, 18, N'iPhone 17 Pro Max 256GB', N'iphone-17-pro-max-256gb', 0, NULL, 1, N'iPhone 17 Pro Max - thế hệ mới nhất, thiết kế và camera nâng cấp toàn diện. Liên hệ để nhận giá tốt nhất và ưu đãi đặt trước.', N'<p><strong>iPhone 17 Pro Max</strong> l&agrave; sản phẩm mới nhất trong d&ograve;ng iPhone Pro, được n&acirc;ng cấp to&agrave;n diện về thiết kế, hiệu năng v&agrave; hệ thống camera so với thế hệ trước. Đ&acirc;y l&agrave; model đang được Apple Store Uyên NTT ưu ti&ecirc;n nhập h&agrave;ng sớm.</p>
<p>Do mới ra mắt, th&ocirc;ng tin cấu h&igrave;nh chi tiết v&agrave; gi&aacute; b&aacute;n sẽ được cập nhật li&ecirc;n tục theo nguồn h&agrave;ng ch&iacute;nh h&atilde;ng. Qu&yacute; kh&aacute;ch quan t&acirc;m vui l&ograve;ng để lại th&ocirc;ng tin hoặc gọi hotline để được tư vấn gi&aacute; tốt nhất v&agrave; giữ chỗ đặt trước.</p>
<p>Kh&aacute;ch đặt trước tại Apple Store Uyên NTT được ưu ti&ecirc;n nhận m&aacute;y sớm, hỗ trợ trả g&oacute;p 0% v&agrave; trợ gi&aacute; thu cũ đổi mới ngay khi l&ecirc;n đời.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR, ProMotion 120Hz, thiết kế viền mỏng hơn thế hệ trước</li>
<li><strong>Chip:</strong> Thế hệ chip Apple mới nhất, hiệu năng AI vượt trội</li>
<li><strong>Camera:</strong> Hệ thống camera n&acirc;ng cấp to&agrave;n diện, cải thiện chụp xa v&agrave; chụp đ&ecirc;m</li>
<li><strong>Pin:</strong> Dung lượng lớn hơn, tối ưu thời lượng sử dụng cả ng&agrave;y</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan, chống nước IP68, cổng USB-C</li>
<li><em>Th&ocirc;ng số đầy đủ sẽ được cập nhật khi c&oacute; h&agrave;ng ch&iacute;nh h&atilde;ng.</em></li>
</ul>', 1, 1, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(11, 17, N'iPhone 16 Pro 128GB', N'iphone-16-pro-128gb', 28990000, 25990000, 0, N'iPhone 16 Pro 128GB - màn hình 6.3 inch nhỏ gọn, chip A18 Pro mạnh mẽ, camera Telephoto 5x.', N'<p><strong>iPhone 16 Pro</strong> mang to&agrave;n bộ sức mạnh của d&ograve;ng Pro v&agrave;o th&acirc;n m&aacute;y 6.3 inch nhỏ gọn hơn, dễ cầm một tay nhưng vẫn đầy đủ camera Telephoto 5x, chip A18 Pro v&agrave; khung Titan cao cấp.</p>
<p>Ph&ugrave; hợp với kh&aacute;ch h&agrave;ng th&iacute;ch m&aacute;y nhỏ gọn nhưng kh&ocirc;ng muốn đ&aacute;nh đổi hiệu năng hay chất lượng camera so với bản Pro Max.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, kiểm tra kỹ trước khi giao, bảo h&agrave;nh d&agrave;i hạn v&agrave; hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.3", ProMotion 120Hz, Always-On</li>
<li><strong>Chip:</strong> Apple A18 Pro</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 48MP + Telephoto 12MP (zoom quang 5x)</li>
<li><strong>Pin &amp; sạc:</strong> Cả ng&agrave;y sử dụng, MagSafe, USB-C</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan, chống nước IP68, n&uacute;t Camera Control</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T13:56:23.609', NULL, 0, 0.00, 0),
(12, 16, N'iPhone 15 Pro 128GB', N'iphone-15-pro-128gb', 25990000, 16490000, 0, N'iPhone 15 Pro 128GB - khung Titan nhẹ bền, chip A17 Pro, camera Telephoto 3x zoom quang học.', N'<p><strong>iPhone 15 Pro</strong> l&agrave; phi&ecirc;n bản 6.1 inch nhỏ gọn của d&ograve;ng Pro, d&ugrave;ng khung Titan nhẹ hơn th&eacute;p kh&ocirc;ng gỉ, trang bị chip A17 Pro v&agrave; cụm camera zoom quang 3x linh hoạt cho nhu cầu chụp ảnh h&agrave;ng ng&agrave;y.</p>
<p>M&aacute;y đ&atilde; qua sử dụng được Apple Store Uyên NTT kiểm tra to&agrave;n diện, ph&acirc;n loại t&igrave;nh trạng minh bạch, ph&ugrave; hợp kh&aacute;ch muốn trải nghiệm d&ograve;ng Pro với chi ph&iacute; hợp l&yacute; hơn m&aacute;y mới.</p>
<p>Đi k&egrave;m bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0% v&agrave; trợ gi&aacute; thu cũ đổi mới.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.1", ProMotion 120Hz, Dynamic Island</li>
<li><strong>Chip:</strong> Apple A17 Pro</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 12MP + Telephoto 12MP (zoom quang 3x)</li>
<li><strong>Pin &amp; sạc:</strong> Cả ng&agrave;y sử dụng, MagSafe, USB-C</li>
<li><strong>Kh&aacute;c:</strong> Khung Titan, chống nước IP68</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(13, 16, N'iPhone 15 128GB', N'iphone-15-128gb', 19990000, 13490000, 0, N'iPhone 15 128GB - Dynamic Island, cổng USB-C, camera chính 48MP. Lựa chọn cân bằng giữa giá và trải nghiệm.', N'<p><strong>iPhone 15</strong> mang Dynamic Island v&agrave; cổng USB-C - hai n&acirc;ng cấp được mong chờ nhất - xuống ph&acirc;n kh&uacute;c phổ th&ocirc;ng, c&ugrave;ng camera ch&iacute;nh 48MP cho ảnh chụp chi tiết hơn hẳn thế hệ trước.</p>
<p>Đ&acirc;y l&agrave; mẫu b&aacute;n chạy nhất tại Apple Store Uyên NTT nhờ mức gi&aacute; hợp l&yacute; m&agrave; vẫn đầy đủ trải nghiệm iPhone hiện đại, ph&ugrave; hợp số đ&ocirc;ng người d&ugrave;ng.</p>
<p>M&aacute;y được kiểm tra kỹ, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0% v&agrave; thu cũ đổi mới.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.1", Dynamic Island</li>
<li><strong>Chip:</strong> Apple A16 Bionic</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 12MP</li>
<li><strong>Pin &amp; sạc:</strong> Cả ng&agrave;y sử dụng, MagSafe, cổng USB-C</li>
<li><strong>Kh&aacute;c:</strong> Chống nước IP68, mặt k&iacute;nh cường lực Ceramic Shield</li>
</ul>', 1, 0, 1, 1, 3, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(14, 15, N'iPhone 14 Pro Max 128GB', N'iphone-14-pro-max-128gb', 26990000, 15990000, 0, N'iPhone 14 Pro Max 128GB - Dynamic Island đầu tiên, camera 48MP, màn hình luôn bật Always-On.', N'<p><strong>iPhone 14 Pro Max</strong> l&agrave; chiếc iPhone đầu ti&ecirc;n giới thiệu Dynamic Island v&agrave; camera ch&iacute;nh 48MP, c&ugrave;ng m&agrave;n h&igrave;nh Always-On hiển thị th&ocirc;ng tin li&ecirc;n tục m&agrave; kh&ocirc;ng cần chạm mở kh&oacute;a.</p>
<p>Sau thời gian sử dụng, m&aacute;y vẫn giữ hiệu năng ổn định nhờ chip A16 Bionic mạnh mẽ, l&agrave; lựa chọn tốt cho kh&aacute;ch muốn trải nghiệm d&ograve;ng Pro Max với chi ph&iacute; thấp hơn đ&aacute;ng kể so với đời mới.</p>
<p>Apple Store Uyên NTT kiểm tra pin, m&agrave;n h&igrave;nh, camera kỹ trước khi b&aacute;n, bảo h&agrave;nh d&agrave;i hạn k&egrave;m trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.7", ProMotion 120Hz, Always-On, Dynamic Island</li>
<li><strong>Chip:</strong> Apple A16 Bionic</li>
<li><strong>Camera sau:</strong> Ch&iacute;nh 48MP + Ultra Wide 12MP + Telephoto 12MP (zoom quang 3x)</li>
<li><strong>Pin &amp; sạc:</strong> Cả ng&agrave;y sử dụng, MagSafe, Lightning</li>
<li><strong>Kh&aacute;c:</strong> Khung th&eacute;p kh&ocirc;ng gỉ, chống nước IP68</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(15, 14, N'iPhone 13 128GB', N'iphone-13-128gb', 15990000, 9490000, 0, N'iPhone 13 128GB - hiệu năng bền bỉ với A15 Bionic, camera kép cải tiến, giá cực tốt cho nhu cầu phổ thông.', N'<p><strong>iPhone 13</strong> vẫn l&agrave; lựa chọn được y&ecirc;u th&iacute;ch nhờ chip A15 Bionic mạnh mẽ, camera k&eacute;p 12MP cải thiện đ&aacute;ng kể khả năng chụp thiếu s&aacute;ng so với thế hệ trước, c&ugrave;ng thời lượng pin cải thiện r&otilde; rệt.</p>
<p>Với mức gi&aacute; phải chăng, đ&acirc;y l&agrave; chiếc iPhone l&yacute; tưởng cho học sinh, sinh vi&ecirc;n hoặc người d&ugrave;ng cần một chiếc m&aacute;y ch&iacute;nh h&atilde;ng ổn định, d&ugrave;ng bền trong nhiều năm.</p>
<p>M&aacute;y đ&atilde; qua kiểm định pin v&agrave; ngoại h&igrave;nh, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Super Retina XDR OLED 6.1", 60Hz</li>
<li><strong>Chip:</strong> Apple A15 Bionic</li>
<li><strong>Camera sau:</strong> K&eacute;p 12MP (Wide + Ultra Wide)</li>
<li><strong>Pin &amp; sạc:</strong> Cải thiện so với iPhone 12, MagSafe, Lightning</li>
<li><strong>Kh&aacute;c:</strong> Chống nước IP68, Face ID</li>
</ul>', 1, 0, 1, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T14:03:03.452', NULL, 0, 0.00, 0),
(16, 12, N'iPhone 11 64GB', N'iphone-11-64gb', 10990000, 5290000, 0, N'iPhone 11 64GB - máy nhập môn giá tốt, camera kép, chip A13 Bionic vẫn đủ mượt cho nhu cầu cơ bản.', N'<p><strong>iPhone 11</strong> l&agrave; lựa chọn tiết kiệm nhất để bắt đầu trải nghiệm hệ sinh th&aacute;i Apple, với camera k&eacute;p 12MP chụp ảnh tốt trong tầm gi&aacute; v&agrave; chip A13 Bionic vẫn đ&aacute;p ứng tốt c&aacute;c t&aacute;c vụ h&agrave;ng ng&agrave;y, lướt web, mạng x&atilde; hội.</p>
<p>Ph&ugrave; hợp l&agrave;m m&aacute;y phụ, m&aacute;y cho người lớn tuổi hoặc trẻ em, hay đơn giản l&agrave; người d&ugrave;ng kh&ocirc;ng đặt nặng cấu h&igrave;nh mạnh.</p>
<p>M&aacute;y đ&atilde; qua kiểm tra kỹ tại Apple Store Uyên NTT, bảo h&agrave;nh d&agrave;i hạn, gi&aacute; cực kỳ cạnh tranh.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina LCD 6.1"</li>
<li><strong>Chip:</strong> Apple A13 Bionic</li>
<li><strong>Camera sau:</strong> K&eacute;p 12MP (Wide + Ultra Wide)</li>
<li><strong>Pin &amp; sạc:</strong> D&ugrave;ng cả ng&agrave;y, cổng Lightning</li>
<li><strong>Kh&aacute;c:</strong> Chống nước IP68, Face ID</li>
</ul>', 1, 0, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-27T16:14:42.916', NULL, 0, 0.00, 0),
(17, 7, N'MacBook Air M3 15 inch 16GB/256GB', N'macbook-air-m3-15-inch-16gb-256gb', 32990000, 27490000, 0, N'MacBook Air M3 15 inch 16GB/256GB - màn hình lớn, hiệu năng M3 mạnh mẽ, pin 18 giờ, thiết kế fanless im lặng.', N'<p><strong>MacBook Air M3 15 inch</strong> mang m&agrave;n h&igrave;nh Liquid Retina lớn 15.3 inch hiếm c&oacute; tr&ecirc;n d&ograve;ng Air, ph&ugrave; hợp cho c&ocirc;ng việc văn ph&ograve;ng, học tập lẫn giải tr&iacute; với kh&ocirc;ng gian hiển thị rộng r&atilde;i.</p>
<p>Chip M3 8-nh&acirc;n CPU cho hiệu năng vượt trội so với M1/M2, xử l&yacute; tốt c&aacute;c t&aacute;c vụ chỉnh sửa ảnh, dựng video nhẹ, trong khi thiết kế fanless giữ m&aacute;y vận h&agrave;nh ho&agrave;n to&agrave;n im lặng.</p>
<p>Cấu h&igrave;nh RAM 16GB rộng r&atilde;i, thoải m&aacute;i cho đa nhiệm nhiều năm tới. M&aacute;y ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina 15.3", 500 nits, dải m&agrave;u rộng P3</li>
<li><strong>Chip:</strong> Apple M3, 8-nh&acirc;n CPU / 10-nh&acirc;n GPU</li>
<li><strong>RAM &amp; lưu trữ:</strong> 16GB hợp nhất / 256GB SSD</li>
<li><strong>Pin:</strong> L&ecirc;n đến 18 giờ sử dụng, sạc nhanh MagSafe 3</li>
<li><strong>Kết nối:</strong> 2 cổng Thunderbolt/USB4, Wi-Fi 6E</li>
<li><strong>Kh&aacute;c:</strong> Thiết kế fanless kh&ocirc;ng quạt, ho&agrave;n to&agrave;n im lặng</li>
</ul>', 1, 1, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T14:13:52.508', NULL, 0, 0.00, 0),
(18, 7, N'MacBook Air M1 13 inch 8GB/256GB 2020', N'macbook-air-m1-13-inch-8gb-256gb-2020', 18990000, 13990000, 0, N'MacBook Air M1 13 inch 2020 - chiếc Mac đầu tiên dùng chip Apple Silicon, bền bỉ, pin trâu, giá hợp lý.', N'<p><strong>MacBook Air M1</strong> l&agrave; cột mốc quan trọng khi Apple chuyển từ chip Intel sang Apple Silicon, mang lại bước nhảy vọt về hiệu năng tr&ecirc;n watt v&agrave; thời lượng pin so với c&aacute;c thế hệ Air trước đ&oacute;.</p>
<p>D&ugrave; ra mắt từ 2020, chip M1 vẫn đủ mượt cho c&ocirc;ng việc văn ph&ograve;ng, lướt web, chỉnh ảnh cơ bản nhờ tối ưu phần mềm tốt của Apple, l&agrave; lựa chọn tiết kiệm để trải nghiệm hệ sinh th&aacute;i macOS.</p>
<p>M&aacute;y đ&atilde; qua sử dụng được kiểm tra pin, b&agrave;n ph&iacute;m, m&agrave;n h&igrave;nh kỹ lưỡng, ph&acirc;n loại t&igrave;nh trạng minh bạch, bảo h&agrave;nh d&agrave;i hạn.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Retina 13.3", 400 nits</li>
<li><strong>Chip:</strong> Apple M1, 8-nh&acirc;n CPU / 7-nh&acirc;n GPU</li>
<li><strong>RAM &amp; lưu trữ:</strong> 8GB hợp nhất / 256GB SSD</li>
<li><strong>Pin:</strong> L&ecirc;n đến 18 giờ sử dụng</li>
<li><strong>Kh&aacute;c:</strong> Thiết kế fanless, Touch ID, 2 cổng Thunderbolt/USB4</li>
</ul>', 1, 0, 1, 1, 7, '2026-07-20T10:44:03.123', '2026-07-24T14:14:37.303', NULL, 0, 0.00, 0),
(19, 8, N'MacBook Pro M4 14 inch 16GB/512GB', N'macbook-pro-m4-14-inch-16gb-512gb', 39990000, 36490000, 0, N'MacBook Pro M4 14 inch 16GB/512GB - màn hình Liquid Retina XDR, hiệu năng M4 đỉnh cao cho dân sáng tạo nội dung.', N'<p><strong>MacBook Pro M4 14 inch</strong> l&agrave; lựa chọn cho người d&ugrave;ng chuy&ecirc;n nghiệp cần hiệu năng mạnh: dựng video 4K, lập tr&igrave;nh, thiết kế đồ họa - nhờ chip M4 thế hệ mới c&ugrave;ng m&agrave;n h&igrave;nh Liquid Retina XDR độ tương phản cao, hỗ trợ ProMotion 120Hz mượt m&agrave;.</p>
<p>Hệ thống cổng kết nối đầy đủ (Thunderbolt 4, HDMI, khe thẻ SDXC) gi&uacute;p l&agrave;m việc kh&ocirc;ng cần th&ecirc;m hub chuyển đổi, tản nhiệt chủ động cho ph&eacute;p duy tr&igrave; hiệu năng cao trong thời gian d&agrave;i.</p>
<p>M&aacute;y ch&iacute;nh h&atilde;ng nguy&ecirc;n seal, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina XDR 14.2", ProMotion 120Hz, 1000 nits (1600 nits HDR)</li>
<li><strong>Chip:</strong> Apple M4</li>
<li><strong>RAM &amp; lưu trữ:</strong> 16GB hợp nhất / 512GB SSD</li>
<li><strong>Pin:</strong> L&ecirc;n đến 18 giờ sử dụng</li>
<li><strong>Kết nối:</strong> 3 cổng Thunderbolt 4, HDMI, khe thẻ SDXC, MagSafe 3</li>
<li><strong>Kh&aacute;c:</strong> Camera FaceTime HD 12MP, loa 6 củ hỗ trợ Spatial Audio</li>
</ul>', 1, 1, 0, 1, 0, '2026-07-20T10:44:03.123', '2026-07-24T14:16:17.301', NULL, 0, 0.00, 0),
(20, 9, N'iPad Pro M4 11 inch WiFi 256GB', N'ipad-pro-m4-11-inch-wifi-256gb', 28990000, 24990000, 0, N'iPad Pro M4 11 inch WiFi 256GB - màn hình Ultra Retina XDR OLED siêu mỏng, hiệu năng đỉnh cao cho công việc sáng tạo.', N'<p><strong>iPad Pro M4</strong> l&agrave; chiếc iPad mỏng nhẹ nhất từ trước đến nay, trang bị m&agrave;n h&igrave;nh Ultra Retina XDR c&ocirc;ng nghệ OLED xếp lớp (tandem) cho độ tương phản v&agrave; độ s&aacute;ng vượt trội, l&yacute; tưởng để vẽ, dựng phim, chỉnh ảnh chuy&ecirc;n nghiệp.</p>
<p>Chip M4 mang hiệu năng ngang ngửa laptop cao cấp, kết hợp Apple Pencil Pro với c&aacute;c cử chỉ mới gi&uacute;p trải nghiệm s&aacute;ng tạo tự nhi&ecirc;n hơn.</p>
<p>M&aacute;y ch&iacute;nh h&atilde;ng, kiểm tra kỹ, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Ultra Retina XDR OLED 11", ProMotion 120Hz</li>
<li><strong>Chip:</strong> Apple M4</li>
<li><strong>Bộ nhớ:</strong> 256GB, kết nối Wi-Fi</li>
<li><strong>Camera:</strong> 12MP sau, TrueDepth 12MP trước</li>
<li><strong>Kh&aacute;c:</strong> Hỗ trợ Apple Pencil Pro, Magic Keyboard, cổng Thunderbolt/USB4</li>
</ul>', 1, 1, 0, 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T14:19:12.903', NULL, 0, 0.00, 0),
(21, 10, N'iPad Air M2 11 inch WiFi 128GB', N'ipad-air-m2-11-inch-wifi-128gb', 16990000, 14490000, 0, N'iPad Air M2 11 inch WiFi 128GB - hiệu năng M2 mạnh mẽ trong thân máy mỏng nhẹ, cân bằng giữa giá và trải nghiệm.', N'<p><strong>iPad Air M2</strong> mang hiệu năng chip M2 từng c&oacute; tr&ecirc;n d&ograve;ng Pro xuống ph&acirc;n kh&uacute;c phổ th&ocirc;ng hơn, đủ sức xử l&yacute; c&aacute;c t&aacute;c vụ đa nhiệm, chỉnh sửa ảnh/video nhẹ nh&agrave;ng v&agrave; chơi game đồ họa cao.</p>
<p>M&aacute;y hỗ trợ Apple Pencil Pro v&agrave; Magic Keyboard, ph&ugrave; hợp cho học tập, l&agrave;m việc di động m&agrave; kh&ocirc;ng cần đầu tư v&agrave;o d&ograve;ng Pro đắt tiền hơn.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina 11", dải m&agrave;u rộng P3</li>
<li><strong>Chip:</strong> Apple M2</li>
<li><strong>Bộ nhớ:</strong> 128GB, kết nối Wi-Fi</li>
<li><strong>Camera:</strong> 12MP sau, 12MP trước (Ultra Wide, hỗ trợ Center Stage)</li>
<li><strong>Kh&aacute;c:</strong> Touch ID t&iacute;ch hợp n&uacute;t nguồn, hỗ trợ Apple Pencil Pro</li>
</ul>', 1, 0, 0, 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T14:21:58.097', NULL, 0, 0.00, 0),
(22, 11, N'iPad Mini 7 WiFi 128GB', N'ipad-mini-7-wifi-128gb', 14990000, 12490000, 0, N'iPad Mini 7 WiFi 128GB - nhỏ gọn bỏ túi, chip A17 Pro mạnh mẽ, hỗ trợ Apple Pencil Pro.', N'<p><strong>iPad Mini 7</strong> giữ th&acirc;n h&igrave;nh nhỏ gọn 8.3 inch dễ mang theo nhưng được n&acirc;ng cấp mạnh về hiệu năng với chip A17 Pro, đủ sức xử l&yacute; c&aacute;c ứng dụng nặng v&agrave; game đồ họa cao.</p>
<p>Lần đầu ti&ecirc;n iPad Mini hỗ trợ Apple Pencil Pro, ph&ugrave; hợp cho việc ghi ch&uacute; nhanh, vẽ ph&aacute;c thảo hay đọc s&aacute;ch, xem phim khi di chuyển.</p>
<p>Sản phẩm ch&iacute;nh h&atilde;ng, bảo h&agrave;nh d&agrave;i hạn, hỗ trợ trả g&oacute;p 0%.</p>', N'<ul>
<li><strong>M&agrave;n h&igrave;nh:</strong> Liquid Retina 8.3"</li>
<li><strong>Chip:</strong> Apple A17 Pro</li>
<li><strong>Bộ nhớ:</strong> 128GB, kết nối Wi-Fi</li>
<li><strong>Camera:</strong> 12MP sau, 12MP trước</li>
<li><strong>Kh&aacute;c:</strong> Hỗ trợ Apple Pencil Pro, cổng USB-C</li>
</ul>', 1, 0, 0, 1, 1, '2026-07-20T10:44:03.123', '2026-07-24T14:23:37.401', NULL, 0, 0.00, 0),
(24, 13, N'iPhone 12 Pro Max 128GB', N'iphone-12-pro-max-128gb', 28990000, 23490000, 0, NULL, N'<h2>Đặc điểm nổi bật của iPhone 12 Pro Max 128GB I Ch&iacute;nh h&atilde;ng VN/A</h2>
<div class="ksp-content">
<p><strong>Sản phẩm iPhone 12 Pro Max sắp hết h&agrave;ng, bạn c&oacute; thể tham khảo th&ecirc;m<span>&nbsp;</span><a href="https://cellphones.com.vn/iphone-13-pro-max.html" target="_blank" rel="noopener">iPhone 13 Pro Max</a><span>&nbsp;</span>với nhiều n&acirc;ng cấp về cấu h&igrave;nh, camera, m&agrave;n h&igrave;nh đang c&oacute; mức gi&aacute; cực hấp dẫn tại hệ thống CellphoneS.</strong></p>
</div>
<div class="table-content" data-v-1bddbad4="">
<div class="table-content__title" data-v-1bddbad4="">
<p data-v-1bddbad4="">Nội dung ch&iacute;nh</p>
</div>
<div class="table-content__wrapper" data-v-1bddbad4="">
<div class="table-content__list" data-v-1bddbad4=""><a href="https://cellphones.com.vn/iphone-12-pro-max.html#dien-thoai-iphone-12-pro-max-nang-tam-dang-cap-su-dung" data-v-1bddbad4="">1. Điện thoại iPhone 12 Pro Max: N&acirc;ng tầm đẳng cấp sử dụng</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#man-hinh-6-7-inches-super-retina-xdr" data-v-1bddbad4="">1.1. M&agrave;n h&igrave;nh 6.7 inches&nbsp;Super Retina XDR</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#ram-6gb-da-nhiem-thoai-mai-bo-nho-trong-dung-luong-lon" data-v-1bddbad4="">1.2. RAM 6GB đa nhiệm thoải m&aacute;i, bộ nhớ trong dung lượng lớn</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#con-chip-apple-a14-soc-5nm-ram-6gb-manh-me" data-v-1bddbad4="">1.3. Con chip Apple A14 SoC 5nm, RAM 6GB mạnh mẽ</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#cum-3-camera-sau-voi-do-phan-giai-12mp" data-v-1bddbad4="">1.4. Cụm 3 camera sau với độ ph&acirc;n giải 12MP&nbsp;&nbsp;</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#camera-truoc-12mp-ho-tro-mo-faceid-cung-cong-nghe-chong-nuoc-ip68" data-v-1bddbad4="">1.5. Camera trước 12MP hỗ trợ mở FaceiD c&ugrave;ng c&ocirc;ng nghệ chống nước IP68</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#vien-pin-lien-cho-thoi-luong-su-dung-len-den-ca-ngay-cung-cong-nghe-sac-nhanh" data-v-1bddbad4="">1.6. Vi&ecirc;n pin liền cho thời lượng sử dụng l&ecirc;n đến cả ng&agrave;y c&ugrave;ng c&ocirc;ng nghệ sạc nhanh&nbsp;</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#iphone-12-pro-max-gia-bao-nhieu-tien" data-v-1bddbad4="">2. iPhone 12 Pro Max gi&aacute; bao nhi&ecirc;u tiền?</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#gia-iphone-12-pro-max-128gb-tai-thoi-diem-ra-mat-la-bao-nhieu" data-v-1bddbad4="">2.1. Gi&aacute; iPhone 12 Pro Max 128GB tại thời điểm ra mắt l&agrave; bao nhi&ecirc;u?</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#gia-iphone-12-pro-max-128gb-chinh-hang-vn-a-moi-nhat-07-2026" data-v-1bddbad4="">2.2. Gi&aacute; iPhone 12 Pro Max 128GB ch&iacute;nh h&atilde;ng VN/A mới nhất&nbsp;07/2026</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#gia-dien-thoai-iphone-12-pro-max-128gb-cu-cu-dep-tray-xuoc-doi-bao-hanh-da-kich-hoat" data-v-1bddbad4="">2.3. Gi&aacute; điện thoại iPhone 12 Pro Max 128GB cũ ( cũ đẹp, trầy xước, đổi bảo h&agrave;nh, đ&atilde; k&iacute;ch hoạt)</a><a href="https://cellphones.com.vn/iphone-12-pro-max.html#mua-iphone-12-pro-max-chinh-hang-vn-a-gia-tot-tai-cellphones" data-v-1bddbad4="">3. Mua iPhone 12 Pro Max&nbsp;ch&iacute;nh h&atilde;ng VN/A gi&aacute; tốt tại CellphoneS</a></div>
</div>
</div>
<div>
<h2><strong>Điện thoại iPhone 12 Pro Max: N&acirc;ng tầm đẳng cấp sử dụng</strong></h2>
<p>Cứ mỗi năm, đến dạo cuối th&aacute;ng 8 v&agrave; gần đầu th&aacute;ng 9 th&igrave; mọi th&ocirc;ng tin s&ocirc;i sục mới về chiếc iPhone mới lại xuất hiện. Apple năm nay lại ra th&ecirc;m một chiếc iPhone mới với t&ecirc;n gọi mới l&agrave;<span>&nbsp;</span><strong>iPhone 12 Pro Max</strong>, đ&acirc;y l&agrave; một d&ograve;ng điện thoại mới v&agrave; mạnh mẽ nhất của nh&agrave; Apple năm nay. Mời bạn tham khảo th&ecirc;m một số m&ocirc; tả sản phẩm b&ecirc;n dưới để bạn c&oacute; thể ra quyết định mua sắm.</p>
<blockquote>
<p>Năm 2023, Apple vừa cho ra mắt series iPhone 15 với nhiều n&acirc;ng cấp từ c&aacute;c thế hệ iPhone 12, 13, 14 trước đ&oacute;. V&igrave; vậy nếu bạn đang t&igrave;m một phi&ecirc;n bản mới nhất của d&ograve;ng điện thoại Apple, tham khảo ngay<span>&nbsp;</span><a href="https://cellphones.com.vn/iphone-15-pro-max.html" title="gi&aacute; iPhone 15 Pro Max" target="_blank" rel="noopener"><strong>gi&aacute; iPhone 15 Pro Max</strong></a><span>&nbsp;</span>tại CellphoneS c&ugrave;ng nhiều ưu đ&atilde;i hấp dẫn. Đặc biệt iPhone 15 Pro Max c&ograve;n được trợ gi&aacute; l&ecirc;n đời tối đa 4 triệu đồng. Xem ngay!</p>
</blockquote>
<h3><strong>M&agrave;n h&igrave;nh 6.7 inches&nbsp;Super Retina XDR</strong></h3>
<p>Năm nay, c&ocirc;ng nghệ m&agrave;n h&igrave;nh tr&ecirc;n 12 Pro Max cũng được đổi mới v&agrave; trang bị tốt hơn c&ugrave;ng k&iacute;ch thước l&ecirc;n đến 6.7 inch, lớn hơn so với<span>&nbsp;</span><strong><a href="https://cellphones.com.vn/hang-cu/iphone/iphone-12.html">điện thoại iPhone 12</a></strong><span>&nbsp;</span>thường. Với c&ocirc;ng nghệ m&agrave;n h&igrave;nh OLED cho khả năng hiển thị h&igrave;nh ảnh l&ecirc;n đến 2778 x 1284&nbsp;pixels. B&ecirc;n cạnh đ&oacute;, m&agrave;n h&igrave;nh n&agrave;y c&ograve;n cho độ s&aacute;ng tối đa cao nhất l&ecirc;n đến 800 nits, lu&ocirc;n đảm bảo cho bạn một độ s&aacute;ng cao v&agrave; dễ nh&igrave;n nhất ngo&agrave;i nắng.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-1.jpg" alt="M&agrave;n h&igrave;nh 6.7 inches Super Retina XDR" loading="lazy"></p>
<p>Một điểm đổi mới nữa tr&ecirc;n m&agrave;n h&igrave;nh của chiếc&nbsp;điện thoại Apple iPhone 12 năm nay l&agrave; việc ch&uacute;ng được thiết kế với khung viền vu&ocirc;ng vức, viền th&eacute;p kh&ocirc;ng gỉ mang đến vẻ đẹp sang trọng cho điện thoại. M&aacute;y cũng được trang bị nhiều phi&ecirc;n bản m&agrave;u sắc đặc biệt cho người d&ugrave;ng lựa chọn.</p>
<h3><strong>RAM 6GB đa nhiệm thoải m&aacute;i, bộ nhớ trong dung lượng lớn</strong></h3>
<p>Về trang bị phần cứng b&ecirc;n trong th&igrave; iPhone 12 Pro Max c&oacute; một thanh RAM l&ecirc;n đến 6GB. Điều n&agrave;y cho thấy rằng Apple ng&agrave;y đang lắng nghe người d&ugrave;ng nhiều hơn khi trang bị một dung lượng RAM lớn hơn để việc đa nhiệm ng&agrave;y c&agrave;ng được cải thiện hơn. Việc thanh ram lớn gi&uacute;p cho bạn trải nghiệm c&aacute;c tựa game v&agrave; đa nhiệm mượt m&agrave; hơn.</p>
<p>Apple tiếp tục ch&uacute; trọng v&agrave;o thiết kế tinh tế tr&ecirc;n<span>&nbsp;</span><a href="https://cellphones.com.vn/mobile/apple/iphone-17.html" title="dt iPhone 17" target="_blank" rel="noopener"><strong>dt iPhone 17</strong></a>, với đường n&eacute;t gọn g&agrave;ng, sang trọng nhưng vẫn bền bỉ. Khung viền được tối ưu gi&uacute;p m&aacute;y cầm nắm thoải m&aacute;i v&agrave; chắc tay hơn trong nhiều t&igrave;nh huống.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-2.jpg" alt="RAM 6GB đa nhiệm thoải m&aacute;i, bộ nhớ trong dung lượng lớn" loading="lazy"></p>
<p>Năm nay, 12 Pro Max cũng sẽ c&oacute; ba phi&ecirc;n bản bộ nhớ trong kh&aacute;c nhau, với bộ nhớ trong nhỏ nhất bắt đầu từ 128GB, 256GB v&agrave; cao nhất sẽ l&agrave; 512GB. Một chiếc điện thoại<span>&nbsp;</span><a href="https://cellphones.com.vn/mobile/apple.html" title="Apple iphone" target="_blank" rel="noopener"><strong>Apple iphone</strong></a><span>&nbsp;</span>m&agrave; c&oacute; một bộ nhớ trong lớn ngang ngửa một chiếc laptop l&agrave; điều m&agrave; Apple muốn mang lại cho người d&ugrave;ng để c&oacute; thể san sẻ bớt bộ nhớ cho c&aacute;c thiết bị kh&aacute;c.</p>
<h3><strong>Con chip Apple A14 SoC 5nm, RAM 6GB mạnh mẽ</strong></h3>
<p>iPhone 12 Pro Max kh&ocirc;ng những chỉ c&oacute; c&aacute;c cải tiến tr&ecirc;n, m&agrave; ch&uacute;ng c&ograve;n c&oacute; một thứ cải tiến được coi l&agrave; nguồn l&otilde;i v&agrave; l&agrave; tr&aacute;i tim để vận h&agrave;nh chiếc điện thoại si&ecirc;u phẩm 2020, đ&oacute; l&agrave; con chip Apple A14 SoC 5nm. Trang bị n&agrave;y gi&uacute;p cho điện thoại c&oacute; một sức mạnh đ&aacute;ng gờm hơn c&aacute;c đối thủ hơn về hiệu năng, hiệu suất sử dụng pin.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-1_1.jpg" alt="Con chip Apple A14 SoC 5nm c&ugrave;ng khả năng kết nối 5G hiện đại" loading="lazy"></p>
<p>M&aacute;y cũng được trang bị chuẩn kết nối wifi v&agrave; mạng di động gi&uacute;p cho bạn c&oacute; thể cải thiện hiệu suất sử dụng mạng v&agrave; ch&uacute;ng c&ograve;n gi&uacute;p c&aacute;c đường truyền t&iacute;n hiệu lu&ocirc;n được đảm bảo kh&ocirc;ng rớt kết nối v&agrave; tăng chất lượng hiển thị h&igrave;nh ảnh tr&ecirc;n mạng.</p>
<h3><strong>Cụm 3 camera sau với độ ph&acirc;n giải 12MP&nbsp;&nbsp;</strong></h3>
<p>C&oacute; thể n&oacute;i camera cũng l&agrave; một bước tiến mới tr&ecirc;n<strong><span>&nbsp;</span>iPhone 12 Pro Max</strong><span>&nbsp;</span>khi ch&uacute;ng c&oacute; một bộ 3 camera với chung một độ ph&acirc;n giải l&agrave; 12MP. Tuy nhi&ecirc;n ch&uacute;ng c&oacute; khẩu độ lớn v&agrave; mật độ điểm ảnh tr&ecirc;n một panel cũng lớn hơn, do đ&oacute; ch&uacute;ng cho h&igrave;nh ảnh chi tiết hơn, bắt s&aacute;ng tốt hơn. Ngo&agrave;i ra, kết hợp chống rung quang học OIS th&igrave; m&aacute;y c&ograve;n c&oacute; thể quay phim 4K tốt.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-4.jpg" alt="Cụm 3 camera sau với độ ph&acirc;n giải 12MP  " loading="lazy"></p>
<p>Camera tr&ecirc;n iPhone 12 Pro Max c&oacute; chức năng qu&eacute;t chiều s&acirc;u v&agrave; đảm bảo h&igrave;nh ảnh c&oacute; một độ s&acirc;u nhất định. C&ugrave;ng với đ&oacute; chức năng ch&iacute;nh của chiếc ống k&iacute;nh n&agrave;y l&agrave; khả năng thể hiện h&igrave;nh ảnh 3D khi qu&eacute;t ch&uacute;ng v&agrave;o một căn ph&ograve;ng nhất định. Gi&uacute;p phục vụ cho c&ocirc;ng việc x&acirc;y dựng cũng như định dạng h&igrave;nh ảnh trước khi x&acirc;y.</p>
<h3><strong>Camera trước 12MP hỗ trợ mở FaceiD c&ugrave;ng c&ocirc;ng nghệ chống nước IP68</strong></h3>
<p>Camera trước 12MP cũng c&oacute; c&ugrave;ng khẩu độ v&agrave; k&iacute;ch thước điểm ảnh trong panel giống như camera. Điều n&agrave;y gi&uacute;p cho việc sử dụng ch&uacute;ng cho chụp ảnh selfie tốt hơn v&agrave; ch&acirc;n thực hơn. C&ugrave;ng với đ&oacute; một t&iacute;nh năng m&agrave; Apple lu&ocirc;n giữ ch&uacute;ng từ đời iPhone X đến giờ l&agrave; khả năng qu&eacute;t khu&ocirc;n mặt 3D FaceiD.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-6.jpg" alt="Camera trước 12MP hỗ trợ mở FaceiD c&ugrave;ng c&ocirc;ng nghệ chống nước IP68" loading="lazy"></p>
<p>C&ocirc;ng nghệ chống nước l&agrave; kh&ocirc;ng thể thiếu tr&ecirc;n c&aacute;c d&ograve;ng điện thoại cao cấp v&agrave; đặc biệt l&agrave; đối với iPhone 12 Pro Max. Ch&uacute;ng được trang bị c&ocirc;ng nghệ chống nước v&agrave; chống bụi tốt nhất hiện nay tr&ecirc;n c&aacute;c d&ograve;ng smartphone đ&oacute; l&agrave; ti&ecirc;u chuẩn IP68. Gi&uacute;p bạn lu&ocirc;n c&oacute; thể y&ecirc;n t&acirc;m hơn trong việc sử dụng quay phim dưới nước hay ở m&ocirc;i trường khắc nghiệt.</p>
<h3><strong>Vi&ecirc;n pin liền cho thời lượng sử dụng l&ecirc;n đến cả ng&agrave;y c&ugrave;ng c&ocirc;ng nghệ sạc nhanh&nbsp;</strong></h3>
<p>Một vi&ecirc;n pin liền với dung lượng lớn tr&ecirc;n iPhone 12 Pro Max gi&uacute;p cho chiếc điện thoại bạn c&oacute; thể hoạt động được một c&aacute;ch ổn thoả hơn v&agrave; thời gian sử dụng được l&acirc;u d&agrave;i hơn. Cụ thể, m&aacute;y cho thời gian nghe nhạc l&ecirc;n tới 80 giờ hoặc 12 giờ xem video trực truyến.</p>
<p>Vi&ecirc;n pin t&iacute;ch hợp tr&ecirc;n iPhone 12 Pro Max mang đến thời lượng sử dụng ấn tượng, đ&aacute;p ứng tốt nhu cầu l&agrave;m việc, giải tr&iacute; v&agrave; kết nối xuy&ecirc;n suốt cả ng&agrave;y. Nếu muốn c&oacute; trải nghiệm liền mạch hơn th&igrave;<span>&nbsp;</span><a href="https://cellphones.com.vn/iphone-17-pro-max.html" title="iPhone 17 Pro Max" target="_blank" rel="noopener"><strong>iPhone 17 Pro Max</strong></a><span>&nbsp;</span>sẽ l&agrave; c&aacute;i t&ecirc;n đ&aacute;ng c&acirc;n nhắc. Thiết bị c&oacute; hệ thống quản l&yacute; năng lượng th&ocirc;ng minh, cho ph&eacute;p duy tr&igrave; hiệu suất cao m&agrave; vẫn tiết kiệm điện năng.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/mobile/apple/iphone-12-pro-max-5.jpg" alt="Vi&ecirc;n pin liền cho thời lượng sử dụng l&ecirc;n đến cả ng&agrave;y c&ugrave;ng c&ocirc;ng nghệ sạc nhanh " loading="lazy"></p>
<p>C&ocirc;ng nghệ sạc l&agrave; tr&ecirc;n 12 Pro Max l&agrave; c&ocirc;ng nghệ sạc nhanh kh&ocirc;ng d&acirc;y l&ecirc;n đến 15W. Điều n&agrave;y c&oacute; thể gi&uacute;p bạn hạn chế được c&aacute;c việc phải ngồi đợi chiếc&nbsp;điện thoại của m&igrave;nh sạc xong khi m&aacute;y c&oacute; thể bổ sung 50% dung lượng chỉ trong v&ograve;ng 30 ph&uacute;t.</p>
<h2><strong>iPhone 12 Pro Max gi&aacute; bao nhi&ecirc;u tiền?</strong></h2>
<p><strong>iPhone 12 Pro Max</strong><span>&nbsp;</span>hiện<span>&nbsp;</span><strong>(th&aacute;ng 07/2026)</strong><span>&nbsp;</span>chủ yếu c&ograve;n c&aacute;c d&ograve;ng m&aacute;y cũ như<span>&nbsp;</span><a href="https://cellphones.com.vn/hang-cu/iphone/iphone-12/12-pro-max.html" title="iPhone 12 Pro Max cũ" target="_blank" rel="noopener"><strong>iPhone 12 Pro Max cũ</strong></a><span>&nbsp;</span>giao động khoảng từ 9 triệu đến hơn 12 triệu đồng t&ugrave;y mức dung lượng v&agrave; t&igrave;nh trạng m&aacute;y. Cụ thể tại CellphoneS, iPhone 12 Pro Max thường sẽ được b&aacute;n ở mức 8,9 triệu cho biển bản 128GB cũ.</p>
<p>D&ugrave; đ&atilde; ra mắt &iacute;t năm nhưng iPhone 12 Pro Max vẫn l&agrave; sự lựa chọn của nhiều người d&ugrave;ng. C&ugrave;ng t&igrave;m hiểu ngay gi&aacute; sản phẩm mới mở b&aacute;n, gi&aacute; sản phẩm ch&iacute;nh h&atilde;ng VN/A v&agrave; gi&aacute; hiện tại để xem c&oacute; sự ch&ecirc;nh lệch nhiều hay kh&ocirc;ng nh&eacute;!</p>
<h3><strong>Gi&aacute; iPhone 12 Pro Max 128GB tại thời điểm ra mắt l&agrave; bao nhi&ecirc;u?</strong></h3>
<p>Ở thời điểm si&ecirc;u phẩm ra mắt kh&ocirc;ng l&acirc;u,&nbsp;gi&aacute; iPhone 12 Pro Max 128gb&nbsp;được tiết lộ với mức khởi điểm l&agrave; 1.099 USD (tương đương khoảng 24.5 triệu đồng). Gi&aacute;&nbsp;12 pro max sẽ tương đồng với c&aacute;c phi&ecirc;n bản m&agrave;u sắc kh&aacute;c nhau: Pacific blue, Silver, Gold, Graphite Black.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/Phone/Apple/iphone-12/iPhone-12-pro-max-3.jpg" alt="iPhone 12 Pro Max 128GB I Ch&iacute;nh h&atilde;ng VN/A" loading="lazy"></p>
<p>Khi về Việt Nam, gi&aacute; iPhone 12 pro max 128GB ch&iacute;nh h&atilde;ng VN/A ở thời điểm mở b&aacute;n tại CellphoneS từ 28.990.000 đồng. Bởi chi ph&iacute; vận chuyển v&agrave; nhiều khoản ph&iacute; kh&aacute;c n&ecirc;n gi&aacute; ch&iacute;nh h&atilde;ng VN/A sẽ cao hơn một ch&uacute;t.</p>
<p>Chi tiết&nbsp;gi&aacute; iPhone 12 Pro Max 128gb c&aacute;c m&agrave;u khi vừa ra mắt như sau:</p>
<table class="table table-bordered">
<tbody>
<tr>
<td>
<p><strong>Phi&ecirc;n bản iPhone 12 Pro max</strong></p>
</td>
<td>
<p><strong>Gi&aacute; b&aacute;n tham khảo</strong></p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB V&agrave;ng</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 28.9 triệu đồng</p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB Xanh</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 28.9 triệu đồng</p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB Đen</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 28.9 triệu đồng</p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB Trắng</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 28.9 triệu đồng</p>
</td>
</tr>
</tbody>
</table>
<h3><strong>Gi&aacute; iPhone 12 Pro Max 128GB ch&iacute;nh h&atilde;ng VN/A mới nhất&nbsp;07/2026</strong></h3>
<p>Tuy c&aacute;c thế hệ mới lần lượt ra đời nhưng iPhone 12 Pro Max vẫn được nhiều người ưa chuộng v&agrave; đ&aacute;nh gi&aacute; cao. C&aacute;c mẫu sản phẩm ch&iacute;nh h&atilde;ng kh&ocirc;ng c&oacute; dấu hiệu hạ nhiệt m&agrave; c&agrave;ng ng&agrave;y được đ&ocirc;ng đảo t&iacute;n đồ c&ocirc;ng nghệ t&igrave;m kiếm v&agrave; sở hữu.</p>
<p>Gi&aacute; b&aacute;n iPhone 12 Pro Max 128GB ch&iacute;nh h&atilde;ng VN/A hiện tại ở CellphoneS từ 25.590.000 VND cho c&aacute;c phi&ecirc;n bản m&agrave;u sắc (gi&aacute; c&oacute; thể thay đổi tuỳ v&agrave;o khuyến m&atilde;i v&agrave; thời điểm kh&aacute;c nhau). Như vậy, người d&ugrave;ng đ&atilde; c&oacute; thể tiết kiệm l&ecirc;n đến 12% so với gi&aacute; gốc, việc c&oacute; trong tay si&ecirc;u phẩm thế hệ 12 dễ d&agrave;ng hơn.</p>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/Phone/Apple/iphone-12/iPhone-12-pro-max-1.jpg" alt="iPhone 12 Pro Max 128GB I Ch&iacute;nh h&atilde;ng VN/A" loading="lazy"></p>
<h3><strong>Gi&aacute; điện thoại iPhone 12 Pro Max 128GB cũ ( cũ đẹp, trầy xước, đổi bảo h&agrave;nh, đ&atilde; k&iacute;ch hoạt)</strong></h3>
<p>Khi c&aacute;c si&ecirc;u phẩm iPhone 13 v&agrave; iPhone 14 li&ecirc;n tục ra mắt, việc trade-in iPhone 12 ProMax để l&ecirc;n đời ng&agrave;y c&agrave;ng tăng. V&igrave; thế, số lượng iPhone 12 Pro Max 128GB cũ tr&ecirc;n thị trường chiếm đa số v&agrave; hầu hết b&aacute;n ch&aacute;y h&agrave;ng. Cụ thể: iPhone 12 Pro Max 128GB ch&iacute;nh h&atilde;ng VN/A đ&atilde; k&iacute;ch hoạt c&oacute; gi&aacute; từ 25.600.000đ, phi&ecirc;n bản cũ đẹp chỉ từ 24.000.000đ, bản đ&atilde; k&iacute;ch hoạt gi&aacute; chỉ từ 25.600.000đ (lưu &yacute;, gi&aacute; sẽ c&oacute; sự thay đổi tuỳ thuộc v&agrave;o chương tr&igrave;nh khuyến m&atilde;i v&agrave; thời điểm kh&aacute;c nhau). Tham khảo ngay gi&aacute; sau đ&acirc;y:</p>
<table class="table table-bordered">
<tbody>
<tr>
<td>
<p><strong>IPhone 12 PRM</strong></p>
</td>
<td>
<p><strong>Gi&aacute; b&aacute;n tham khảo</strong></p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB | Ch&iacute;nh h&atilde;ng VN/A - Đ&atilde; K&iacute;ch Hoạt</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 25.6 triệu đồng</p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB - Cũ Đẹp</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 24 triệu đồng</p>
</td>
</tr>
<tr>
<td>
<p>iPhone 12 Pro Max 128GB - Đ&atilde; K&iacute;ch Hoạt</p>
</td>
<td>
<p>Gi&aacute; chỉ từ 25.6 triệu đồng</p>
</td>
</tr>
</tbody>
</table>
<p><img src="https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/Phone/Apple/iphone-12/iPhone-12-pro-max-2.jpg" alt="iPhone 12 Pro Max 128GB I Ch&iacute;nh h&atilde;ng VN/A" loading="lazy"></p>
<h2><strong>Mua iPhone 12 Pro Max</strong><strong>&nbsp;ch&iacute;nh h&atilde;ng VN/A gi&aacute; tốt tại CellphoneS</strong></h2>
<p>Bạn l&agrave; một iFan v&agrave; bạn đang mong chờ để được trải nghiệm thử chiếc&nbsp;<strong>điện thoại&nbsp;iPhone 12 Pro Max</strong><span>&nbsp;</span>mới nhất của Apple? Mời bạn đến ngay với CellphoneS để được tư vấn v&agrave; trải nghiệm nhanh ch&oacute;ng nhất c&oacute; thể.&nbsp;</p>
<p>C&ugrave;ng với đ&oacute;, bạn c&oacute; thể xem th&ecirc;m c&aacute;c d&ograve;ng<span>&nbsp;</span><a href="https://cellphones.com.vn/mobile/apple/iphone-13.html" title="iPhone 13" target="_blank" rel="noopener"><strong>iPhone 13</strong></a><span>&nbsp;</span>hiện nay đang được mở b&aacute;n, liệu n&ecirc;n chọn d&ograve;ng kế nhiệm kh&ocirc;ng. C&ugrave;ng kh&aacute;m ph&aacute; ngay nh&eacute;!</p>
</div>', N'<p class="title my-2">M&agrave;n h&igrave;nh</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/kich-thuoc-man-hinh-dien-thoai" target="_blank" rel="nofollow noopener">K&iacute;ch thước m&agrave;n h&igrave;nh</a></td>
<td>
<p>6.7 inches</p>
</td>
</tr>
<tr>
<td>C&ocirc;ng nghệ m&agrave;n h&igrave;nh</td>
<td>
<p>Super Retina XDR OLED, HDR10, Dolby Vision, Wide color gamut, True-tone</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/man-hinh-dien-thoai" target="_blank" rel="nofollow noopener">C&ocirc;ng nghệ m&agrave;n h&igrave;nh</a></td>
<td>
<p><a href="https://cellphones.com.vn/sforum/man-hinh-oled" target="_blank" rel="nofollow noopener">OLED</a></p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/do-phan-giai-man-hinh" target="_blank" rel="nofollow noopener">Độ ph&acirc;n giải m&agrave;n h&igrave;nh</a></td>
<td>
<p>1284 x 2778 pixels</p>
</td>
</tr>
<tr>
<td>T&iacute;nh năng m&agrave;n h&igrave;nh</td>
<td>
<p>HDR10<br>Dolby Vision<br>True-tone<br>Độ s&aacute;ng 800 nits</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/tan-so-quet-man-hinh-la-gi" target="_blank" rel="nofollow noopener">Tần số qu&eacute;t</a></td>
<td>
<p>60Hz</p>
</td>
</tr>
<tr>
<td>Kiểu m&agrave;n h&igrave;nh</td>
<td>
<p><a href="https://cellphones.com.vn/sforum/man-hinh-tai-tho-la-gi" target="_blank" rel="nofollow noopener">Tai thỏ</a></p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Camera sau</p>
<table class="technical-content">
<tbody>
<tr>
<td>Camera sau</td>
<td>
<p>Camera ch&iacute;nh: 12 MP, f/1.6, 26mm, 1.4&micro;m, dual pixel PDAF, OIS<br>Camera tele: 12 MP, f/2.0, 52mm, 1/3.4", 1.0&micro;m, PDAF, OIS, 2x optical zoom<br>Camera g&oacute;c si&ecirc;u rộng: 12 MP, f/2.4, 120˚, 13mm, 1/3.6"<br>Cảm biến: TOF 3D LiDAR scanner</p>
</td>
</tr>
<tr>
<td>Quay video</td>
<td>
<p>Trước: 4K@24/30/60fps, 1080p@30/60/120fps, gyro-EIS<br>Sau: 4K@24/30/60fps, 1080p@30/60/120/240fps, HDR, Dolby Vision HDR (up to 60fps), stereo sound rec.</p>
</td>
</tr>
<tr>
<td>T&iacute;nh năng camera</td>
<td>
<p>Ban đ&ecirc;m (Night Mode)<br>Tr&ocirc;i nhanh thời gian (Time Lapse)<br>Quay chậm (Slow Motion)<br>X&oacute;a ph&ocirc;ng Zoom quang học<br>To&agrave;n cảnh (Panorama)<br>Chống rung quang học (OIS)<br>Tự động lấy n&eacute;t (AF)<br>Nhận diện khu&ocirc;n mặt<br>HDR<br>Ảnh Raw</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Camera trước</p>
<table class="technical-content">
<tbody>
<tr>
<td>Camera trước</td>
<td>
<p>12 MP, f/2.2, 23mm (wide), 1/3.6"<br>SL 3D, (depth/biometrics sensor)</p>
</td>
</tr>
<tr>
<td>Quay video trước</td>
<td>
<p>4K@24/30/60fps, 1080p@30/60/120fps, gyro-EIS</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Vi xử l&yacute; &amp; đồ họa</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/chipset-la-gi" target="_blank" rel="nofollow noopener">Chipset</a></td>
<td>
<p>Apple A14 Bionic (5 nm)</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/gpu-la-gi" target="_blank" rel="nofollow noopener">GPU</a></td>
<td>
<p>Apple GPU (4-core graphics)</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Giao tiếp &amp; kết nối</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/nfc-la-gi" target="_blank" rel="nofollow noopener">C&ocirc;ng nghệ NFC</a></td>
<td>
<p>C&oacute;</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/sim-la-gi" target="_blank" rel="nofollow noopener">Thẻ SIM</a></td>
<td>
<p>2 SIM (nano‑SIM v&agrave; eSIM)</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/cam-bien-hong-ngoai-la-gi" target="_blank" rel="nofollow noopener">Hồng ngoại</a></td>
<td>
<p>Kh&ocirc;ng</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/jack-tai-nghe-35mm-la-gi" target="_blank" rel="nofollow noopener">Jack tai nghe 3.5</a></td>
<td>
<p>Kh&ocirc;ng</p>
</td>
</tr>
<tr>
<td>Hỗ trợ mạng</td>
<td>
<p><a href="https://cellphones.com.vn/sforum/mang-5g-la-gi" target="_blank" rel="nofollow noopener">5G</a></p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/gps-la-gi" target="_blank" rel="nofollow noopener">GPS</a></td>
<td>
<p>A-GPS, GLONASS, GALILEO, QZSS</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">RAM &amp; lưu trữ</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/ram-la-gi" target="_blank" rel="nofollow noopener">Dung lượng RAM</a></td>
<td>
<p>6 GB</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/bo-nho-trong" target="_blank" rel="nofollow noopener">Bộ nhớ trong</a></td>
<td>
<p>128 GB</p>
</td>
</tr>
<tr>
<td>Khe cắm thẻ nhớ</td>
<td>
<p>Kh&ocirc;ng</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Pin &amp; c&ocirc;ng nghệ sạc</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/mah-la-gi-don-vi-mah-la-gi" target="_blank" rel="nofollow noopener">Pin</a></td>
<td>
<p>Li-Ion, sạc nhanh 20W, sạc kh&ocirc;ng d&acirc;y 15W, USB Power Delivery 2.0</p>
</td>
</tr>
<tr>
<td>C&ocirc;ng nghệ sạc</td>
<td>
<p>Sạc nhanh 20W<br>Sạc kh&ocirc;ng d&acirc;y 15W<br>Power Delivery 2.0</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/cac-loai-cong-sac" target="_blank" rel="nofollow noopener">Cổng sạc</a></td>
<td>
<p><a href="https://cellphones.com.vn/sforum/cong-lightning-la-gi" target="_blank" rel="nofollow noopener">Lightning</a></p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">T&iacute;nh năng kh&aacute;c</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/he-dieu-hanh-la-gi-nhung-he-dieu-hanh-pho-bien-tren-may-tinh-dien-thoai" target="_blank" rel="nofollow noopener">Hệ điều h&agrave;nh</a></td>
<td>
<p>iOS 14.1 hoặc cao hơn (T&ugrave;y v&agrave;o phi&ecirc;n bản ph&aacute;t h&agrave;nh)</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Bộ xử l&yacute; &amp; Đồ họa</p>
<table class="technical-content">
<tbody>
<tr>
<td>Loại CPU</td>
<td>
<p>Hexa-core</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">K&iacute;ch thước &amp; Trọng lượng</p>
<table class="technical-content">
<tbody>
<tr>
<td>K&iacute;ch thước</td>
<td>
<p>160.8 x 78.1 x 7.4 mm</p>
</td>
</tr>
<tr>
<td>Trọng lượng</td>
<td>
<p>228</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Thiết kế &amp; Trọng lượng</p>
<table class="technical-content">
<tbody>
<tr>
<td>Chất liệu mặt lưng</td>
<td>
<p>K&iacute;nh</p>
</td>
</tr>
<tr>
<td>Chất liệu khung viền</td>
<td>
<p>Kim loại</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Th&ocirc;ng số kh&aacute;c</p>
<table class="technical-content">
<tbody>
<tr>
<td>Chỉ số kh&aacute;ng nước, bụi</td>
<td>
<p>IP68</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Tiện &iacute;ch kh&aacute;c</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/cam-bien-van-tay" target="_blank" rel="nofollow noopener">Cảm biến v&acirc;n tay</a></td>
<td>
<p>Kh&ocirc;ng</p>
</td>
</tr>
<tr>
<td>C&aacute;c loại cảm biến</td>
<td>
<p><a href="https://cellphones.com.vn/sforum/cam-bien-gia-toc-la-gi" target="_blank" rel="nofollow noopener">Cảm biến gia tốc</a>,<span>&nbsp;</span><a href="https://cellphones.com.vn/sforum/cam-bien-tiem-can-dien-thoai" target="_blank" rel="nofollow noopener">Cảm biến tiệm cận</a>,<span>&nbsp;</span><a href="https://cellphones.com.vn/sforum/cam-bien-anh-sang-la-gi" target="_blank" rel="nofollow noopener">Cảm biến &aacute;nh s&aacute;ng</a>,<span>&nbsp;</span><a href="https://cellphones.com.vn/sforum/cam-bien-la-ban-la-gi" target="_blank" rel="nofollow noopener">La b&agrave;n</a>,<span>&nbsp;</span><a href="https://cellphones.com.vn/sforum/con-quay-hoi-chuyen" target="_blank" rel="nofollow noopener">Con quay hồi chuyển</a>, Cảm biến &aacute;p kế</p>
</td>
</tr>
<tr>
<td>T&iacute;nh năng đặc biệt</td>
<td>
<p>Hỗ trợ 5G, Sạc kh&ocirc;ng d&acirc;y, Nhận diện khu&ocirc;n mặt, Kh&aacute;ng nước, kh&aacute;ng bụi</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Cổng kết nối</p>
<table class="technical-content">
<tbody>
<tr>
<td><a href="https://cellphones.com.vn/sforum/wifi-la-gi-song-wi-fi-hoat-dong-va-co-chuc-nang-nhu-the-nao" target="_blank" rel="nofollow noopener">Wi-Fi</a></td>
<td>
<p>Wi-Fi 802.11 a/b/g/n/ac/6, dual-band, hotspot</p>
</td>
</tr>
<tr>
<td><a href="https://cellphones.com.vn/sforum/ket-noi-bluetooth-khai-niem-va-nhung-cong-dung-co-ban" target="_blank" rel="nofollow noopener">Bluetooth</a></td>
<td>
<p>5.0, A2DP, LE</p>
</td>
</tr>
</tbody>
</table>
<p class="title my-2">Cấu h&igrave;nh</p>
<table class="technical-content">
<tbody>
<tr>
<td>Cảm biến</td>
<td>
<p>Face ID, cảm biến gia tốc, cảm biến tiệm cận, con quay hồi chuyển, cảm bi&ecirc;n s&aacute;ng</p>
</td>
</tr>
</tbody>
</table>
<p></p>', 1, 0, 0, 1, 2, '2026-07-25T11:28:15.043', NULL, NULL, 0, 0.00, 0);
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

-- ----------------------------------------------------------------------
-- [ProductImages] - 84 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ProductImages] ON;
INSERT INTO [dbo].[ProductImages] ([Id], [ProductId], [Url], [DisplayOrder], [IsMain]) VALUES
(1, 1, N'/images/placeholder-product.svg', 1, 0),
(2, 1, N'/images/placeholder-product.svg', 2, 0),
(3, 2, N'/images/placeholder-product.svg', 1, 0),
(4, 2, N'/images/placeholder-product.svg', 2, 0),
(9, 5, N'/images/placeholder-product.svg', 0, 0),
(10, 5, N'/images/placeholder-product.svg', 1, 0),
(47, 1, N'/uploads/products/2026-07/9725bfd635a046ad99753528e8efd243.jpg', 0, 1),
(48, 2, N'/uploads/products/2026-07/14afa32ad915400b94298a02d1f44c39.jpg', 0, 1),
(49, 3, N'/uploads/products/2026-07/e0bb212b09c042eda5f021dd893ec711.jpg', 1, 0),
(50, 3, N'/uploads/products/2026-07/9b55012ec25246788b7d66f18717576d.jpg', 0, 1),
(51, 3, N'/uploads/products/2026-07/edd4f5744d554e55b73c7ca0db43172f.jpg', 2, 0),
(52, 4, N'/uploads/products/2026-07/f32c98fb371c4b93a6e5c6b35c285f69.jpg', 0, 1),
(53, 4, N'/uploads/products/2026-07/86a667068d3148cca8de10d452c92ca7.jpg', 1, 0),
(54, 4, N'/uploads/products/2026-07/65761c9a85b14f9eb5bcf5801d8a20de.jpg', 2, 0),
(55, 5, N'/uploads/products/2026-07/2ec58e72309545739f7a66133b4e8940.jpg', 2, 1),
(56, 6, N'/uploads/products/2026-07/ff24a8ec34ae43b1b75c7bdac56ef791.jpg', 0, 1),
(57, 7, N'/uploads/products/2026-07/09e47e7184644f19b3a9a9e82ceff613.jpg', 0, 1),
(58, 7, N'/uploads/products/2026-07/e19729f80c7b40e1b909ccc7ebb2c4a7.jpg', 1, 0),
(59, 8, N'/uploads/products/2026-07/5d277b5e8bef42bcb2e9f6fb8bb22731.jpg', 0, 1),
(60, 8, N'/uploads/products/2026-07/407bd052404c4ec6a97b235255da7350.jpg', 1, 0),
(61, 8, N'/uploads/products/2026-07/6d78cc44d996459e95b86e856454b532.jpg', 2, 0),
(62, 8, N'/uploads/products/2026-07/717d61a8a87e422db6c4f45bbd94d8dc.jpg', 3, 0),
(63, 9, N'/uploads/products/2026-07/559524e7f99146cda04e258329d80367.jpg', 0, 1),
(64, 9, N'/uploads/products/2026-07/e6fdadd7e4f94d67bdfb863477aea0c8.jpg', 1, 0),
(65, 9, N'/uploads/products/2026-07/c15d880f305943bda050fa3633dcccc2.jpg', 2, 0),
(66, 9, N'/uploads/products/2026-07/9feb79cbefa940b4a7a9dc52476bbed0.jpg', 3, 0),
(67, 9, N'/uploads/products/2026-07/89bb78d06791431cacd7131c5faffa68.jpg', 4, 0),
(69, 10, N'/uploads/products/2026-07/1211241200fc42fe92d73386cb1914f7.jpg', 1, 0),
(70, 10, N'/uploads/products/2026-07/47021725796d42c99952367e873b3c5c.jpg', 2, 0),
(71, 10, N'/uploads/products/2026-07/bdc24fdc14604903bd780433d0d3c0ca.jpg', 3, 0),
(72, 10, N'/uploads/products/2026-07/f1b4192ab95c4560ad844889e68238ac.jpg', 4, 0),
(73, 10, N'/uploads/products/2026-07/458a02769e4a4caab8a3d32fe990b03e.jpg', 0, 1),
(74, 11, N'/uploads/products/2026-07/18bfc4f8b0df4c61b6d09104c634d76b.jpg', 2, 1),
(75, 11, N'/uploads/products/2026-07/e6d307fca83a44b5b0f193183c470fb3.jpg', 3, 0),
(76, 11, N'/uploads/products/2026-07/8f23b73040414d11b3bf896c3d500aaa.jpg', 4, 0),
(77, 11, N'/uploads/products/2026-07/d966344cd84e420ba417a908c8fc5afe.jpg', 5, 0),
(78, 11, N'/uploads/products/2026-07/1757060671664fb886a6de3666f3a0b9.jpg', 6, 0),
(79, 12, N'/uploads/products/2026-07/dff5755645b7484ba7b413efbbf2c2df.jpg', 0, 1),
(80, 12, N'/uploads/products/2026-07/7684bf2bac454485af4bd12647b8e466.jpg', 1, 0),
(81, 12, N'/uploads/products/2026-07/8e5142a463f2431488bf33f8219d875c.jpg', 2, 0),
(82, 13, N'/uploads/products/2026-07/6c97d33b82c74961a205274e6a78d908.jpg', 2, 1),
(83, 13, N'/uploads/products/2026-07/5c8a342dbb354da0ad561ee6d9b73719.jpg', 3, 0),
(84, 13, N'/uploads/products/2026-07/0a3c22bde2be4700957d49d8cdbb326b.jpg', 4, 0),
(85, 14, N'/uploads/products/2026-07/03ca59c3ec72467098fbb4baa8a58370.jpg', 0, 1),
(86, 14, N'/uploads/products/2026-07/f50f24a727354f80bad3009ed61cb3a9.jpg', 1, 0),
(87, 14, N'/uploads/products/2026-07/14b5c71afe4b4b38a17f7826bef6191d.jpg', 2, 0),
(88, 15, N'/uploads/products/2026-07/1902023ea5f94bdfacc35754d9e88183.png', 0, 1),
(89, 15, N'/uploads/products/2026-07/ebd11c1b8388447d9e0883d47bfd421d.jpg', 1, 0),
(90, 15, N'/uploads/products/2026-07/d15964bce6ef4847b72bf848c0b399a6.jpg', 2, 0),
(91, 15, N'/uploads/products/2026-07/e2a15fd09de54a2b8f1048d896af0c48.jpg', 3, 0);
INSERT INTO [dbo].[ProductImages] ([Id], [ProductId], [Url], [DisplayOrder], [IsMain]) VALUES
(92, 15, N'/uploads/products/2026-07/417fdb49cb344a7b8e0e9cfaf96769d5.jpg', 4, 0),
(93, 16, N'/uploads/products/2026-07/86e28e2b85e646269c5f159880efa957.jpg', 0, 1),
(94, 16, N'/uploads/products/2026-07/2c135613872c4dd081333c67dbbc4e84.jpg', 1, 0),
(95, 16, N'/uploads/products/2026-07/f6246d61716d4ce5a1452c9525faef05.jpg', 2, 0),
(96, 16, N'/uploads/products/2026-07/375272b5d1ea434fbc2f83a18444ce8f.jpg', 3, 0),
(97, 16, N'/uploads/products/2026-07/f18cd683643d4a8bbdfec1a3625807f3.jpg', 4, 0),
(98, 16, N'/uploads/products/2026-07/88bd20bc682f4299a904b737bd267128.jpg', 5, 0),
(99, 16, N'/uploads/products/2026-07/1857940287404400b1bdb1591eebccf7.jpg', 6, 0),
(100, 17, N'/uploads/products/2026-07/4e22a3d67e9c4df5ba437ee5aaa8baec.jpg', 0, 1),
(101, 17, N'/uploads/products/2026-07/644a24c915b64d9f9ac7e149277430e6.jpg', 1, 0),
(102, 17, N'/uploads/products/2026-07/2faf1120e4c04b7cb22c4e6e4f66594f.jpg', 2, 0),
(103, 17, N'/uploads/products/2026-07/5cb69ad22f54471aa16aad0dda3700f2.jpg', 3, 0),
(104, 18, N'/uploads/products/2026-07/5b834aef114148e5837a0d0370849c07.jpg', 0, 1),
(105, 18, N'/uploads/products/2026-07/84760961cf6847b5beeeae192bc4c53b.jpg', 1, 0),
(106, 18, N'/uploads/products/2026-07/723801177fed4a4884abacb996c9ad72.jpg', 2, 0),
(107, 18, N'/uploads/products/2026-07/b5aca52c816440d190a57f9fde5ee20f.jpg', 3, 0),
(108, 18, N'/uploads/products/2026-07/9efa52bacf914c069239a1735ab8b69b.jpg', 4, 0),
(109, 19, N'/uploads/products/2026-07/47522ae7706c40db94e8bb0f08072289.jpg', 0, 1),
(110, 20, N'/uploads/products/2026-07/d42be777c1a34d92a3a9d247b1169740.jpg', 0, 1),
(111, 20, N'/uploads/products/2026-07/e0c09f633a1a434798d7e6a91562aa75.jpg', 1, 0),
(112, 20, N'/uploads/products/2026-07/2dcd88197c514671bb9104bc7640b26e.jpg', 2, 0),
(113, 20, N'/uploads/products/2026-07/ebf22af065d444b6adbe743a0c3d523b.jpg', 3, 0),
(114, 21, N'/uploads/products/2026-07/f3dc54689ed44ada9a5864d438085c81.jpg', 0, 1),
(115, 21, N'/uploads/products/2026-07/4d9e7d9cbbfd4be08a5320a8f3a6a742.jpg', 1, 0),
(116, 21, N'/uploads/products/2026-07/b59ac5952ffe4c45b2d9b5b81da0d756.jpg', 2, 0),
(117, 21, N'/uploads/products/2026-07/048072213c4c45aebbd57a3a14290d49.jpg', 3, 0),
(118, 22, N'/uploads/products/2026-07/9a2dd7d013614cd393e816ea06e22156.jpg', 0, 1),
(119, 22, N'/uploads/products/2026-07/bf1db71a8719437fbafe16ef700f65d6.jpg', 1, 0),
(120, 22, N'/uploads/products/2026-07/382eedffb450400b912d439d3b30528d.jpg', 2, 0),
(121, 22, N'/uploads/products/2026-07/a58bab516c7c4eb5b8f53ebf7cf526f2.jpg', 3, 0),
(122, 22, N'/uploads/products/2026-07/881323e102fe489784d72479dd59dbe1.jpg', 4, 0),
(123, 22, N'/uploads/products/2026-07/e342209caff84c72a318070bfc8110d2.jpg', 5, 0),
(124, 24, N'/uploads/products/2026-07/91937144f22548db9d3125c75f993124.jpg', 0, 1),
(125, 24, N'/uploads/products/2026-07/66944835847b45bcbec2f9f223e91288.jpg', 1, 0);
SET IDENTITY_INSERT [dbo].[ProductImages] OFF;
GO

-- ----------------------------------------------------------------------
-- [ProductOptionGroups] - 6 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ProductOptionGroups] ON;
INSERT INTO [dbo].[ProductOptionGroups] ([Id], [ProductId], [Name], [DisplayOrder]) VALUES
(1, 8, N'Dung lượng', 0),
(2, 8, N'Màu sắc', 1),
(3, 8, N'Tình trạng', 2),
(4, 8, N'Loại SIM', 3),
(5, 9, N'Màu sắc', 0),
(6, 9, N'Tình trạng', 1);
SET IDENTITY_INSERT [dbo].[ProductOptionGroups] OFF;
GO

-- ----------------------------------------------------------------------
-- [ProductOptionValues] - 16 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ProductOptionValues] ON;
INSERT INTO [dbo].[ProductOptionValues] ([Id], [GroupId], [Value], [PriceAdjustment], [DisplayOrder], [IsDefault]) VALUES
(1, 1, N'256GB', 0, 0, 1),
(2, 1, N'512GB', 3500000, 1, 0),
(3, 1, N'1TB', 7000000, 2, 0),
(4, 2, N'Titan Đen', 0, 0, 1),
(5, 2, N'Titan Trắng', 0, 1, 0),
(6, 2, N'Titan Tự Nhiên', 0, 2, 0),
(7, 2, N'Titan Sa Mạc', 0, 3, 0),
(8, 3, N'99%', 0, 0, 1),
(9, 3, N'98%', -1500000, 1, 0),
(10, 4, N'Chính hãng VN/A', 0, 0, 1),
(11, 4, N'LL/A (Mỹ)', -800000, 1, 0),
(12, 5, N'Titan Đen', 0, 0, 1),
(13, 5, N'Titan Trắng', 0, 1, 0),
(14, 5, N'Titan Xanh', 0, 2, 0),
(15, 6, N'99%', 0, 0, 1),
(16, 6, N'98%', -1200000, 1, 0);
SET IDENTITY_INSERT [dbo].[ProductOptionValues] OFF;
GO

-- ----------------------------------------------------------------------
-- [NewsCategories] - 4 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[NewsCategories] ON;
INSERT INTO [dbo].[NewsCategories] ([Id], [Name], [Slug], [DisplayOrder], [IsActive], [DeletedAt], [IsDeleted]) VALUES
(1, N'Thiết bị', N'thiet-bi', 3, 1, NULL, 0),
(2, N'Khoa học công nghệ', N'khoa-hoc-cong-nghe', 5, 1, NULL, 0),
(3, N'Thủ thuật', N'thu-thuat', 1, 1, NULL, 0),
(4, N'Tư vấn', N'tu-van', 7, 1, NULL, 0);
SET IDENTITY_INSERT [dbo].[NewsCategories] OFF;
GO

-- ----------------------------------------------------------------------
-- [NewsArticles] - 8 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[NewsArticles] ON;
INSERT INTO [dbo].[NewsArticles] ([Id], [Title], [Slug], [ThumbnailUrl], [Excerpt], [ContentHtml], [IsFeatured], [IsActive], [PublishedAt], [ViewCount], [CreatedAt], [UpdatedAt], [CategoryId], [DeletedAt], [IsDeleted]) VALUES
(1, N'iPhone 17 Pro Max lộ diện: thiết kế mới, camera nâng cấp toàn diện', N'iphone-17-pro-max-lo-dien-thiet-ke-moi', N'/uploads/news/2026-07/2ec9f45ac69a41e9a961b73b59439853.jpg', N'iPhone 17 Pro Max lộ diện: thiết kế mới, camera nâng cấp toàn diện. Bài viết demo phục vụ học tập — nội dung chi tiết sẽ được biên tập trong trang quản trị.', N'<h1>iPhone 17 Pro Max lộ diện: thiết kế mới, camera n&acirc;ng cấp to&agrave;n diện</h1>
<p>Apple được cho l&agrave; đang chuẩn bị ra mắt <strong>iPhone 17 Pro Max</strong>, mẫu flagship cao cấp nhất trong d&ograve;ng iPhone thế hệ mới. D&ugrave; chưa ch&iacute;nh thức tr&igrave;nh l&agrave;ng, h&agrave;ng loạt th&ocirc;ng tin r&ograve; rỉ từ chuỗi cung ứng v&agrave; c&aacute;c nguồn tin uy t&iacute;n đ&atilde; phần n&agrave;o h&eacute; lộ những thay đổi đ&aacute;ng ch&uacute; &yacute; về thiết kế, hiệu năng v&agrave; đặc biệt l&agrave; hệ thống camera.</p>
<h2>Thiết kế tinh tế, hiện đại hơn</h2>
<p>Theo c&aacute;c th&ocirc;ng tin r&ograve; rỉ, iPhone 17 Pro Max vẫn giữ ng&ocirc;n ngữ thiết kế vu&ocirc;ng vức quen thuộc nhưng được tinh chỉnh để mang lại cảm gi&aacute;c cầm nắm thoải m&aacute;i hơn. Phần khung m&aacute;y dự kiến sử dụng vật liệu cao cấp, gi&uacute;p giảm trọng lượng đồng thời tăng độ bền.</p>
<p>Cụm camera sau được thiết kế lại với k&iacute;ch thước lớn hơn, tạo kh&ocirc;ng gian cho c&aacute;c cảm biến mới v&agrave; cải thiện khả năng xử l&yacute; h&igrave;nh ảnh. M&agrave;n h&igrave;nh viền mỏng hơn cũng gi&uacute;p tăng diện t&iacute;ch hiển thị m&agrave; kh&ocirc;ng l&agrave;m thay đổi đ&aacute;ng kể k&iacute;ch thước tổng thể của thiết bị.</p>
<h2>Camera được n&acirc;ng cấp to&agrave;n diện</h2>
<p>Camera lu&ocirc;n l&agrave; điểm nhấn tr&ecirc;n c&aacute;c d&ograve;ng Pro Max v&agrave; iPhone 17 Pro Max được kỳ vọng sẽ tiếp tục n&acirc;ng tầm trải nghiệm nhiếp ảnh.</p>
<p>Một số n&acirc;ng cấp được đồn đo&aacute;n gồm:</p>
<ul>
<li>
<p>Cảm biến ch&iacute;nh c&oacute; độ ph&acirc;n giải cao hơn, cho h&igrave;nh ảnh sắc n&eacute;t v&agrave; chi tiết.</p>
</li>
<li>
<p>Camera g&oacute;c si&ecirc;u rộng cải thiện khả năng chụp thiếu s&aacute;ng.</p>
</li>
<li>
<p>Camera telephoto hỗ trợ zoom quang học xa hơn.</p>
</li>
<li>
<p>Thuật to&aacute;n xử l&yacute; ảnh thế hệ mới gi&uacute;p t&aacute;i tạo m&agrave;u sắc ch&acirc;n thực.</p>
</li>
<li>
<p>Khả năng quay video chất lượng cao với chống rung được cải thiện.</p>
</li>
</ul>
<p>Những n&acirc;ng cấp n&agrave;y hứa hẹn mang đến chất lượng ảnh v&agrave; video vượt trội cho cả người d&ugrave;ng phổ th&ocirc;ng lẫn những người y&ecirc;u th&iacute;ch s&aacute;ng tạo nội dung.</p>
<h2>Hiệu năng mạnh mẽ</h2>
<p>iPhone 17 Pro Max được kỳ vọng sẽ trang bị vi xử l&yacute; Apple thế hệ mới với hiệu năng CPU v&agrave; GPU mạnh hơn, đồng thời tối ưu khả năng tiết kiệm điện năng.</p>
<p>Thiết bị c&oacute; thể đi k&egrave;m dung lượng RAM lớn hơn, gi&uacute;p xử l&yacute; đa nhiệm mượt m&agrave;, chơi game đồ họa cao ổn định v&agrave; hỗ trợ tốt c&aacute;c t&iacute;nh năng AI tr&ecirc;n iOS mới.</p>
<h2>M&agrave;n h&igrave;nh ProMotion tiếp tục được cải tiến</h2>
<p>M&aacute;y nhiều khả năng sẽ sở hữu m&agrave;n h&igrave;nh OLED chất lượng cao c&ugrave;ng tần số qu&eacute;t ProMotion l&ecirc;n đến 120Hz.</p>
<p>Độ s&aacute;ng tối đa dự kiến được n&acirc;ng l&ecirc;n, mang lại khả năng hiển thị r&otilde; r&agrave;ng ngay cả dưới &aacute;nh s&aacute;ng mạnh. C&ocirc;ng nghệ tiết kiệm điện tr&ecirc;n m&agrave;n h&igrave;nh cũng được cải thiện nhằm k&eacute;o d&agrave;i thời lượng pin.</p>
<h2>Pin v&agrave; sạc</h2>
<p>B&ecirc;n cạnh hiệu năng, Apple được cho l&agrave; sẽ tối ưu thời lượng sử dụng th&ocirc;ng qua vi&ecirc;n pin lớn hơn v&agrave; con chip tiết kiệm năng lượng hơn.</p>
<p>Người d&ugrave;ng c&oacute; thể kỳ vọng:</p>
<ul>
<li>
<p>Thời gian sử dụng l&acirc;u hơn so với thế hệ trước.</p>
</li>
<li>
<p>Sạc nhanh được cải thiện.</p>
</li>
<li>
<p>Hỗ trợ sạc kh&ocirc;ng d&acirc;y v&agrave; sạc MagSafe ổn định hơn.</p>
</li>
</ul>
<h2>iOS mới với nhiều t&iacute;nh năng th&ocirc;ng minh</h2>
<p>iPhone 17 Pro Max dự kiến được c&agrave;i sẵn phi&ecirc;n bản iOS mới nhất với nhiều cải tiến về giao diện, quyền ri&ecirc;ng tư v&agrave; c&aacute;c t&iacute;nh năng th&ocirc;ng minh hỗ trợ người d&ugrave;ng trong c&ocirc;ng việc cũng như giải tr&iacute;.</p>
<p>C&aacute;c t&iacute;nh năng AI hứa hẹn sẽ gi&uacute;p việc chỉnh sửa ảnh, t&igrave;m kiếm th&ocirc;ng tin v&agrave; tương t&aacute;c với thiết bị trở n&ecirc;n thuận tiện hơn.</p>
<h2>Gi&aacute; b&aacute;n dự kiến</h2>
<p>Hiện Apple vẫn chưa c&ocirc;ng bố mức gi&aacute; ch&iacute;nh thức. Tuy nhi&ecirc;n, nhiều chuy&ecirc;n gia nhận định iPhone 17 Pro Max sẽ nằm trong ph&acirc;n kh&uacute;c cao cấp với mức gi&aacute; tương đương hoặc nhỉnh hơn thế hệ tiền nhiệm, t&ugrave;y theo dung lượng lưu trữ v&agrave; từng thị trường.</p>
<h2>Kết luận<img src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQpjuXcZvVQ3NWjCU2DWKZEWmrhKxNfkr_pkbi7e7EIu4NRm4VT2hq7XFdm&amp;s=10" alt="The iPhone 17 Pro will feature an updated design! Do you like it?"></h2>
<p>D&ugrave; mọi th&ocirc;ng tin hiện nay vẫn chỉ dừng ở mức r&ograve; rỉ, iPhone 17 Pro Max đang cho thấy nhiều dấu hiệu sẽ l&agrave; một trong những chiếc smartphone đ&aacute;ng mong đợi nhất của Apple. Thiết kế được tinh chỉnh, hệ thống camera n&acirc;ng cấp mạnh mẽ, hiệu năng vượt trội c&ugrave;ng c&aacute;c t&iacute;nh năng AI mới hứa hẹn sẽ mang đến trải nghiệm sử dụng ấn tượng cho người d&ugrave;ng.</p>
<p>Website sẽ tiếp tục cập nhật những th&ocirc;ng tin mới nhất ngay khi Apple ch&iacute;nh thức c&ocirc;ng bố sản phẩm, gi&uacute;p bạn kh&ocirc;ng bỏ lỡ bất kỳ điểm nổi bật n&agrave;o của d&ograve;ng iPhone mới.</p>', 1, 1, '2026-07-19T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-24T15:47:27.388', 1, NULL, 0),
(2, N'Apple phát hành bản cập nhật iOS mới: vá lỗi bảo mật quan trọng', N'apple-phat-hanh-ban-cap-nhat-ios-moi', N'/images/placeholder-news.svg', N'Apple vừa phát hành bản cập nhật iOS mới khắc phục nhiều lỗi bảo mật quan trọng, người dùng iPhone được khuyến nghị cập nhật ngay để bảo vệ thiết bị.', N'<p>Apple vừa chính thức phát hành bản cập nhật hệ điều hành iOS mới, tập trung khắc phục một số lỗ hổng bảo mật được đánh giá ở mức nghiêm trọng. Đây là bản cập nhật mà người dùng iPhone nên cài đặt sớm để đảm bảo an toàn dữ liệu cá nhân.</p>
<h3>Những gì bản cập nhật khắc phục</h3>
<ul>
<li>Vá lỗi cho phép ứng dụng độc hại truy cập trái phép vào dữ liệu hệ thống.</li>
<li>Khắc phục lỗ hổng liên quan đến xử lý hình ảnh có thể bị khai thác từ xa.</li>
<li>Cải thiện cơ chế xác thực khi đồng bộ dữ liệu qua iCloud.</li>
<li>Sửa một số lỗi vặt ảnh hưởng đến thời lượng pin và kết nối Wi-Fi.</li>
</ul>
<h3>Vì sao nên cập nhật ngay</h3>
<p>Các lỗ hổng bảo mật, đặc biệt là những lỗi có thể bị khai thác từ xa mà không cần người dùng tương tác, luôn tiềm ẩn rủi ro cao. Việc trì hoãn cập nhật đồng nghĩa với việc thiết bị của bạn vẫn tồn tại lỗ hổng mà tin tặc có thể lợi dụng.</p>
<h3>Cách cập nhật</h3>
<p>Người dùng có thể vào <strong>Cài đặt → Cài đặt chung → Cập nhật phần mềm</strong> để kiểm tra và tải bản cập nhật mới nhất. Nên sao lưu dữ liệu qua iCloud hoặc máy tính trước khi cập nhật, đồng thời đảm bảo pin còn trên 50% hoặc đang cắm sạc trong quá trình cài đặt.</p>
<p>Nếu máy có dấu hiệu giật lag sau khi cập nhật, người dùng có thể mang đến các trung tâm bảo hành/sửa chữa uy tín để được kiểm tra và tối ưu lại thiết bị.</p>', 1, 0, '2026-07-17T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-24T10:55:34.274', 2, NULL, 0),
(3, N'MacBook Air M4 có gì mới? So sánh nhanh với thế hệ M3', N'macbook-air-m4-co-gi-moi-so-sanh-m3', N'/images/placeholder-news.svg', N'MacBook Air chuyển sang chip M4 với hiệu năng và thời lượng pin được cải thiện. Cùng so sánh nhanh những khác biệt so với thế hệ M3 trước đó.', N'<p>Sau khi dòng MacBook Pro và iPad Pro đã lên đời chip M4, đến lượt MacBook Air được đồn đoán sẽ chuyển sang thế hệ chip mới này trong đợt cập nhật sắp tới, thay thế cho chip M3 hiện tại.</p>
<h3>Hiệu năng CPU và GPU</h3>
<p>Chip M4 được kỳ vọng mang lại mức cải thiện hiệu năng CPU đáng kể nhờ tiến trình sản xuất mới, đồng thời GPU cũng được nâng cấp giúp xử lý tốt hơn các tác vụ đồ họa, dựng video nhẹ hay chơi game có yêu cầu cấu hình vừa phải.</p>
<h3>Thời lượng pin</h3>
<p>Một trong những điểm mạnh xuyên suốt của dòng MacBook Air là thời lượng pin, và với M4 tối ưu hơn về tiêu thụ điện năng, người dùng có thể kỳ vọng thời gian sử dụng thực tế được kéo dài thêm so với thế hệ M3.</p>
<h3>Bảng so sánh nhanh</h3>
<table>
<thead><tr><th>Tiêu chí</th><th>MacBook Air M3</th><th>MacBook Air M4 (dự kiến)</th></tr></thead>
<tbody>
<tr><td>Hiệu năng CPU</td><td>Tốt</td><td>Cải thiện rõ rệt</td></tr>
<tr><td>Hiệu năng GPU</td><td>Tốt</td><td>Cao hơn</td></tr>
<tr><td>Thời lượng pin</td><td>Lên đến 18 giờ</td><td>Kỳ vọng dài hơn</td></tr>
<tr><td>Hỗ trợ AI trên máy</td><td>Có</td><td>Tối ưu hơn</td></tr>
</tbody>
</table>
<h3>Có nên chờ M4 hay mua M3 ngay?</h3>
<p>Với nhu cầu văn phòng, học tập thông thường, MacBook Air M3 hiện tại vẫn đáp ứng rất tốt và đang có mức giá dễ tiếp cận hơn. Nếu công việc của bạn liên quan nhiều đến xử lý đồ họa hoặc muốn dùng máy lâu dài trong nhiều năm tới, chờ đợi thế hệ M4 có thể là lựa chọn đáng cân nhắc.</p>', 1, 0, '2026-07-15T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-24T10:55:34.274', 1, NULL, 0),
(4, N'5 mẹo tiết kiệm pin iPhone ai cũng nên biết', N'5-meo-tiet-kiem-pin-iphone', N'/uploads/news/2026-07/b02deb166b00472da0154114732555f4.jpg', N'Pin iPhone tụt nhanh khiến bạn khó chịu? Đây là 5 mẹo đơn giản giúp kéo dài thời lượng sử dụng pin mà ai cũng có thể áp dụng ngay.', N'<p>Pin là một trong những mối quan tâm hàng đầu của người dùng iPhone, đặc biệt là với những chiếc máy đã sử dụng qua vài năm. Dưới đây là 5 mẹo đơn giản giúp bạn kéo dài thời lượng pin mỗi ngày.</p>
<h3>1. Bật chế độ Tối ưu hóa sạc pin</h3>
<p>Vào <strong>Cài đặt → Pin → Tình trạng pin &amp; Sạc pin</strong>, bật tùy chọn "Sạc pin được tối ưu hóa" giúp máy học thói quen sạc của bạn và giảm thời gian pin ở mức 100%, từ đó làm chậm quá trình chai pin.</p>
<h3>2. Giảm độ sáng màn hình và bật tự động</h3>
<p>Màn hình là bộ phận tiêu tốn pin nhiều nhất trên iPhone. Bật "Độ sáng tự động" và tắt bớt các hiệu ứng nền động sẽ giúp máy tiết kiệm pin đáng kể trong sử dụng hàng ngày.</p>
<h3>3. Quản lý ứng dụng chạy nền</h3>
<p>Vào <strong>Cài đặt → Cài đặt chung → Làm mới ứng dụng nền</strong> để tắt bớt các ứng dụng không cần thiết tự động cập nhật dữ liệu khi bạn không sử dụng.</p>
<h3>4. Bật chế độ Tiết kiệm pin đúng lúc</h3>
<p>Chế độ Low Power Mode không chỉ dùng khi pin yếu - bạn có thể bật thủ công bất cứ lúc nào để giảm hiệu năng nền, tắt tải xuống tự động và kéo dài thời gian sử dụng khi cần thiết.</p>
<h3>5. Tránh để máy quá nóng hoặc quá lạnh</h3>
<p>Nhiệt độ môi trường ảnh hưởng trực tiếp đến hiệu suất và tuổi thọ pin Lithium-ion. Hạn chế để máy dưới nắng gắt hoặc trong xe hơi nóng, đặc biệt là trong lúc sạc pin.</p>
<p>Nếu đã áp dụng các mẹo trên mà pin vẫn tụt nhanh bất thường, có thể pin của máy đã chai và cần được kiểm tra, thay thế tại các trung tâm sửa chữa uy tín.</p>', 0, 1, '2026-07-12T10:44:03.249', 1, '2026-07-20T10:44:03.263', '2026-07-24T15:48:17.771', 3, NULL, 0),
(5, N'Nên mua iPhone 15 Pro Max cũ 99% hay iPhone 16 mới?', N'nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi', N'/uploads/news/2026-07/a94510b2986a4078bb9b6e64a783ef8a.jpg', N'Cùng tầm giá gần nhau, nhiều khách hàng phân vân giữa iPhone 15 Pro Max cũ 99% và iPhone 16 mới. Bài viết so sánh giúp bạn chọn đúng máy phù hợp nhu cầu.', N'<p>Đây là câu hỏi mà rất nhiều khách hàng đặt ra khi đến tư vấn tại Apple Store Uyên NTT: nên chọn <strong>iPhone 15 Pro Max cũ 99%</strong> hay <strong>iPhone 16 mới nguyên seal</strong> khi mức giá hai máy khá gần nhau? Câu trả lời phụ thuộc vào nhu cầu sử dụng cụ thể của từng người.</p>
<h3>Về hiệu năng và camera</h3>
<p>iPhone 15 Pro Max sở hữu chip A17 Pro cùng camera Telephoto tetraprism zoom quang 5x - vốn là tính năng cao cấp của dòng Pro Max, trong khi iPhone 16 (bản thường) dùng chip A18 nhưng camera không có ống kính zoom xa. Nếu bạn ưu tiên chụp ảnh xa, quay video chuyên nghiệp, 15 Pro Max vẫn có lợi thế.</p>
<h3>Về độ mới và bảo hành</h3>
<p>iPhone 16 mới nguyên seal có bảo hành chính hãng đầy đủ và không lo về pin/ngoại hình đã qua sử dụng. iPhone 15 Pro Max cũ 99%, nếu mua tại nơi uy tín có kiểm tra pin, ngoại hình minh bạch, vẫn đảm bảo trải nghiệm gần như máy mới với mức giá tốt hơn.</p>
<h3>Gợi ý lựa chọn</h3>
<ul>
<li>Ưu tiên camera zoom xa, khung Titan cao cấp, chấp nhận máy đã qua sử dụng → chọn <strong>iPhone 15 Pro Max 99%</strong>.</li>
<li>Ưu tiên máy mới nguyên seal, bảo hành dài, không quan trọng ống kính zoom xa → chọn <strong>iPhone 16</strong>.</li>
</ul>
<p>Dù chọn phương án nào, bạn cũng nên mua tại cửa hàng có chính sách kiểm tra máy minh bạch trước mặt khách và bảo hành rõ ràng để yên tâm sử dụng lâu dài.</p>', 0, 1, '2026-07-08T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-27T16:14:42.916', 4, NULL, 0),
(6, N'Apple Watch hỗ trợ theo dõi sức khỏe tim mạch như thế nào?', N'apple-watch-theo-doi-suc-khoe-tim-mach', N'/uploads/news/2026-07/12c2450f0f984631bb17e732f76a5ae4.jpg', N'Từ đo nhịp tim, điện tâm đồ đến cảnh báo nhịp tim bất thường - Apple Watch đang trở thành trợ thủ đắc lực theo dõi sức khỏe tim mạch hàng ngày.', N'<p>Không chỉ là một thiết bị đeo thông minh thông thường, Apple Watch ngày càng được đầu tư trở thành công cụ theo dõi sức khỏe tim mạch đáng tin cậy, với nhiều tính năng được phát triển cùng các chuyên gia y tế.</p>
<h3>Đo nhịp tim liên tục</h3>
<p>Cảm biến quang học ở mặt sau đồng hồ đo nhịp tim liên tục trong suốt cả ngày, giúp bạn theo dõi xu hướng nhịp tim khi nghỉ ngơi, khi vận động và trong lúc ngủ.</p>
<h3>Điện tâm đồ ECG ngay trên cổ tay</h3>
<p>Tính năng điện tâm đồ cho phép người dùng thực hiện đo ECG một đạo trình chỉ trong 30 giây bằng cách chạm ngón tay vào mặt kính đồng hồ, kết quả có thể xuất thành file PDF để chia sẻ với bác sĩ.</p>
<h3>Cảnh báo nhịp tim bất thường</h3>
<p>Apple Watch có thể phát hiện và cảnh báo khi nhịp tim quá cao hoặc quá thấp bất thường so với mức hoạt động hiện tại, cũng như cảnh báo nhịp tim không đều nghi ngờ rung nhĩ - một trong những nguyên nhân phổ biến dẫn đến đột quỵ.</p>
<h3>Lưu ý khi sử dụng</h3>
<p>Các tính năng theo dõi sức khỏe trên Apple Watch mang tính chất tham khảo, hỗ trợ phát hiện sớm dấu hiệu bất thường chứ không thay thế cho chẩn đoán y khoa. Nếu nhận được cảnh báo bất thường, người dùng nên đến cơ sở y tế để được thăm khám chính xác.</p>', 0, 1, '2026-07-05T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-24T15:49:47.193', 2, NULL, 0),
(7, N'Hướng dẫn mua điện thoại trả góp 0% đơn giản tại TP.HCM', N'huong-dan-mua-dien-thoai-tra-gop-0-phan-tram', N'/uploads/news/2026-07/3694a3833e5d48f98d7a48c8f6474c1a.jpg', N'Mua điện thoại trả góp 0% lãi suất giúp giảm áp lực tài chính khi lên đời máy mới. Bài viết hướng dẫn thủ tục đơn giản để mua trả góp nhanh chóng.', N'<p>Trả góp 0% lãi suất là hình thức mua sắm ngày càng phổ biến, giúp khách hàng sở hữu iPhone, MacBook, iPad đời mới mà không cần chuẩn bị toàn bộ số tiền ngay từ đầu. Vậy thủ tục mua trả góp đơn giản như thế nào?</p>
<h3>Điều kiện mua trả góp</h3>
<ul>
<li>Có thẻ tín dụng của ngân hàng liên kết (Visa/Mastercard/JCB) còn hạn mức khả dụng, hoặc</li>
<li>Đáp ứng điều kiện của công ty tài chính tiêu dùng (thường cần CMND/CCCD, hộ khẩu/tạm trú, chứng minh thu nhập tùy gói vay).</li>
</ul>
<h3>Các bước thực hiện</h3>
<ol>
<li><strong>Chọn sản phẩm và kỳ hạn trả góp</strong> phù hợp với khả năng chi trả hàng tháng, thường từ 3 đến 12 tháng.</li>
<li><strong>Cung cấp giấy tờ tùy thân</strong> để nhân viên tư vấn kiểm tra điều kiện và lên hợp đồng trả góp.</li>
<li><strong>Ký hợp đồng và thanh toán trước</strong> (nếu có) theo tỷ lệ quy định của ngân hàng/công ty tài chính.</li>
<li><strong>Nhận máy ngay</strong> sau khi hợp đồng được duyệt, thường chỉ mất 15-30 phút với thẻ tín dụng.</li>
</ol>
<h3>Lưu ý để tránh phát sinh chi phí</h3>
<p>Trả góp 0% lãi suất qua thẻ tín dụng thường miễn phí chuyển đổi nếu thanh toán đúng hạn hàng tháng, tuy nhiên một số ngân hàng có thể thu phí chuyển đổi trả góp (khoảng 2-3% giá trị đơn hàng). Khách hàng nên hỏi kỹ nhân viên tư vấn về tổng chi phí thực tế trước khi quyết định.</p>
<p>Tại Apple Store Uyên NTT, khách hàng được hỗ trợ tư vấn trả góp 0% linh hoạt ngay tại quầy, thủ tục nhanh gọn và minh bạch về chi phí phát sinh (nếu có).</p>', 0, 1, '2026-06-30T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-27T16:14:42.916', 4, NULL, 0),
(8, N'AirPods thế hệ mới có thể đo nhịp tim ngay trên tai nghe?', N'airpods-the-he-moi-do-nhip-tim', N'/uploads/news/2026-07/6aa23833e17c4a07ac4e849764f66b05.jpg', N'Tin đồn về AirPods tích hợp cảm biến đo nhịp tim đang thu hút sự chú ý lớn, mở ra hướng đi mới cho tai nghe không dây của Apple.', N'<p>Bên cạnh Apple Watch, tai nghe AirPods cũng đang được đồn đoán sẽ trở thành thiết bị theo dõi sức khỏe tiếp theo trong hệ sinh thái Apple, với tin đồn tích hợp cảm biến đo nhịp tim ngay trên thân tai nghe.</p>
<h3>Cảm biến đo nhịp tim hoạt động ra sao?</h3>
<p>Theo các thông tin rò rỉ, Apple đang thử nghiệm cảm biến quang học tương tự công nghệ trên Apple Watch, đặt ở phần tiếp xúc với ống tai - vị trí được cho là đo nhịp tim chính xác hơn cả cổ tay nhờ gần với động mạch hơn.</p>
<h3>Vì sao đây là bước đi hợp lý</h3>
<ul>
<li>AirPods được đeo liên tục trong lúc tập luyện thể thao, phù hợp để thu thập dữ liệu nhịp tim theo thời gian thực.</li>
<li>Không cần đeo thêm Apple Watch, giảm bớt thiết bị mang theo khi vận động.</li>
<li>Dữ liệu có thể đồng bộ trực tiếp vào ứng dụng Sức khỏe, kết hợp cùng dữ liệu từ iPhone.</li>
</ul>
<h3>Khi nào sẽ ra mắt?</h3>
<p>Hiện Apple chưa xác nhận chính thức về tính năng này. Nếu đúng như tin đồn, khả năng cao tính năng đo nhịp tim sẽ xuất hiện trên các mẫu AirPods cao cấp trong tương lai gần, mở ra một hướng phát triển mới cho dòng tai nghe true wireless vốn đã rất phổ biến.</p>
<p>Apple Store Uyên NTT sẽ cập nhật thông tin sớm nhất ngay khi Apple công bố chính thức các tính năng sức khỏe mới trên AirPods.</p>', 0, 1, '2026-06-25T10:44:03.249', 0, '2026-07-20T10:44:03.263', '2026-07-27T16:14:42.916', 1, NULL, 0);
SET IDENTITY_INSERT [dbo].[NewsArticles] OFF;
GO

-- ----------------------------------------------------------------------
-- [StaticPages] - 14 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[StaticPages] ON;
INSERT INTO [dbo].[StaticPages] ([Id], [Title], [Slug], [ContentHtml], [IsActive], [CreatedAt], [UpdatedAt], [DeletedAt], [IsDeleted]) VALUES
(1, N'Giới thiệu', N'gioi-thieu', N'<h1>Giới thiệu</h1>
<h2>Ch&agrave;o mừng bạn đến với iPhone Store</h2>
<p>iPhone Store được th&agrave;nh lập với mong muốn mang đến cho kh&aacute;ch h&agrave;ng những sản phẩm Apple ch&iacute;nh h&atilde;ng, chất lượng cao c&ugrave;ng mức gi&aacute; hợp l&yacute;. Ch&uacute;ng t&ocirc;i kh&ocirc;ng chỉ b&aacute;n thiết bị, m&agrave; c&ograve;n mang đến trải nghiệm mua sắm minh bạch, tiện lợi v&agrave; dịch vụ hậu m&atilde;i tận t&acirc;m.</p>
<p>Trong suốt qu&aacute; tr&igrave;nh hoạt động, ch&uacute;ng t&ocirc;i lu&ocirc;n đặt <strong>uy t&iacute;n &ndash; chất lượng &ndash; sự h&agrave;i l&ograve;ng của kh&aacute;ch h&agrave;ng</strong> l&agrave;m kim chỉ nam. Mỗi sản phẩm trước khi đến tay kh&aacute;ch h&agrave;ng đều được kiểm tra kỹ lưỡng nhằm đảm bảo chất lượng v&agrave; hiệu năng hoạt động tốt nhất.</p>
<h2>Sản phẩm của ch&uacute;ng t&ocirc;i</h2>
<p>Ch&uacute;ng t&ocirc;i cung cấp đa dạng c&aacute;c d&ograve;ng sản phẩm Apple, bao gồm:</p>
<ul>
<li>
<p>iPhone mới ch&iacute;nh h&atilde;ng.</p>
</li>
<li>
<p>iPhone Like New, đ&atilde; qua kiểm định chất lượng.</p>
</li>
<li>
<p>Phụ kiện ch&iacute;nh h&atilde;ng như AirPods, Apple Watch, sạc, c&aacute;p, ốp lưng...</p>
</li>
<li>
<p>C&aacute;c thiết bị Apple kh&aacute;c theo từng thời điểm.</p>
</li>
</ul>
<p>Tất cả sản phẩm đều c&oacute; th&ocirc;ng tin r&otilde; r&agrave;ng về t&igrave;nh trạng, nguồn gốc v&agrave; ch&iacute;nh s&aacute;ch bảo h&agrave;nh để kh&aacute;ch h&agrave;ng y&ecirc;n t&acirc;m lựa chọn.</p>
<h2>Cam kết của ch&uacute;ng t&ocirc;i</h2>
<p>Ch&uacute;ng t&ocirc;i lu&ocirc;n nỗ lực mang đến trải nghiệm mua sắm tốt nhất th&ocirc;ng qua những cam kết sau:</p>
<ul>
<li>
<p>100% sản phẩm ch&iacute;nh h&atilde;ng hoặc đ&uacute;ng như m&ocirc; tả.</p>
</li>
<li>
<p>B&aacute;o gi&aacute; minh bạch, kh&ocirc;ng ph&aacute;t sinh chi ph&iacute; ẩn.</p>
</li>
<li>
<p>Ch&iacute;nh s&aacute;ch bảo h&agrave;nh v&agrave; đổi trả r&otilde; r&agrave;ng.</p>
</li>
<li>
<p>Hỗ trợ tư vấn tận t&igrave;nh trước v&agrave; sau khi mua h&agrave;ng.</p>
</li>
<li>
<p>Giao h&agrave;ng nhanh ch&oacute;ng tr&ecirc;n to&agrave;n quốc.</p>
</li>
<li>
<p>Hỗ trợ trả g&oacute;p với nhiều h&igrave;nh thức thanh to&aacute;n linh hoạt.</p>
</li>
</ul>
<h2>Tại sao n&ecirc;n chọn iPhone Store?</h2>
<ul>
<li>
<p>Đội ngũ tư vấn am hiểu sản phẩm Apple.</p>
</li>
<li>
<p>Lu&ocirc;n cập nhật c&aacute;c mẫu iPhone mới nhất.</p>
</li>
<li>
<p>Gi&aacute; cả cạnh tranh c&ugrave;ng nhiều chương tr&igrave;nh ưu đ&atilde;i.</p>
</li>
<li>
<p>Quy tr&igrave;nh mua h&agrave;ng đơn giản, nhanh ch&oacute;ng v&agrave; an to&agrave;n.</p>
</li>
<li>
<p>H&agrave;ng ngh&igrave;n kh&aacute;ch h&agrave;ng đ&atilde; tin tưởng v&agrave; lựa chọn.</p>
</li>
</ul>
<h2>Sứ mệnh</h2>
<p>Ch&uacute;ng t&ocirc;i mong muốn gi&uacute;p mọi kh&aacute;ch h&agrave;ng dễ d&agrave;ng tiếp cận những sản phẩm c&ocirc;ng nghệ chất lượng với chi ph&iacute; hợp l&yacute;, đồng thời x&acirc;y dựng một m&ocirc;i trường mua sắm trực tuyến minh bạch, chuy&ecirc;n nghiệp v&agrave; đ&aacute;ng tin cậy.</p>
<h2>Tầm nh&igrave;n</h2>
<p>Trở th&agrave;nh một trong những hệ thống b&aacute;n lẻ thiết bị Apple được kh&aacute;ch h&agrave;ng tin tưởng h&agrave;ng đầu tại Việt Nam, kh&ocirc;ng ngừng n&acirc;ng cao chất lượng dịch vụ v&agrave; mở rộng hệ sinh th&aacute;i sản phẩm.</p>
<h2>Li&ecirc;n hệ</h2>
<p>Nếu bạn cần tư vấn về sản phẩm, ch&iacute;nh s&aacute;ch bảo h&agrave;nh hoặc hỗ trợ kỹ thuật, đội ngũ của ch&uacute;ng t&ocirc;i lu&ocirc;n sẵn s&agrave;ng đồng h&agrave;nh.</p>
<p><strong>iPhone Store</strong> xin ch&acirc;n th&agrave;nh cảm ơn sự tin tưởng v&agrave; ủng hộ của Qu&yacute; kh&aacute;ch. Ch&uacute;ng t&ocirc;i sẽ kh&ocirc;ng ngừng cải thiện chất lượng sản phẩm v&agrave; dịch vụ để mang đến trải nghiệm mua sắm tốt nhất.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-24T08:46:21.925', NULL, 0),
(2, N'Chính sách bảo hành', N'chinh-sach-bao-hanh', N'<h1 data-section-id="dcnq4n" data-start="4097" data-end="4128">Ch&iacute;nh s&aacute;ch bảo h&agrave;nh 20 năm<span class="PDq2pG_selectionAnchor" aria-hidden="true"></span></h1>
<blockquote data-start="4130" data-end="4435">
<p data-start="4132" data-end="4435"><strong data-start="4132" data-end="4142">Lưu &yacute;:</strong> "Bảo h&agrave;nh 20 năm" l&agrave; một th&ocirc;ng điệp marketing rất mạnh. Tr&ecirc;n thực tế, kh&ocirc;ng n&ecirc;n ghi l&agrave; bảo h&agrave;nh to&agrave;n bộ m&aacute;y trong 20 năm nếu bạn kh&ocirc;ng thể thực hiện. Thay v&agrave;o đ&oacute; n&ecirc;n diễn đạt l&agrave; <strong data-start="4321" data-end="4372">Chương tr&igrave;nh đồng h&agrave;nh &amp; hỗ trợ kỹ thuật 20 năm</strong>, c&ograve;n bảo h&agrave;nh phần cứng vẫn theo thời hạn cụ thể của sản phẩm.</p>
</blockquote>
<h2 data-section-id="1vzj523" data-start="4437" data-end="4467">Ch&iacute;nh s&aacute;ch đồng h&agrave;nh 20 năm</h2>
<p data-start="4469" data-end="4616">Ch&uacute;ng t&ocirc;i cam kết đồng h&agrave;nh c&ugrave;ng kh&aacute;ch h&agrave;ng trong suốt qu&aacute; tr&igrave;nh sử dụng thiết bị, mang đến dịch vụ hậu m&atilde;i l&acirc;u d&agrave;i v&agrave; hỗ trợ kỹ thuật nhanh ch&oacute;ng.</p>
<hr data-start="4618" data-end="4621">
<h2 data-section-id="12q2udt" data-start="4623" data-end="4635">Quyền lợi</h2>
<p data-start="4637" data-end="4653">Kh&aacute;ch h&agrave;ng được:</p>
<ul data-start="4655" data-end="4867">
<li data-section-id="1ighumk" data-start="4655" data-end="4678">Kiểm tra m&aacute;y miễn ph&iacute;</li>
<li data-section-id="1xzcbeb" data-start="4679" data-end="4696">Tư vấn kỹ thuật</li>
<li data-section-id="5l1yhy" data-start="4697" data-end="4718">Hỗ trợ cập nhật iOS</li>
<li data-section-id="150s2ey" data-start="4719" data-end="4743">Hỗ trợ sao lưu dữ liệu</li>
<li data-section-id="1ryrobw" data-start="4744" data-end="4772">Chuyển dữ liệu khi đổi m&aacute;y</li>
<li data-section-id="1b4sxii" data-start="4773" data-end="4795">Vệ sinh m&aacute;y miễn ph&iacute;</li>
<li data-section-id="11vwp3o" data-start="4796" data-end="4810">Kiểm tra pin</li>
<li data-section-id="2bd2i7" data-start="4811" data-end="4829">Kiểm tra Face ID</li>
<li data-section-id="1mg4gze" data-start="4830" data-end="4847">Kiểm tra camera</li>
<li data-section-id="4cbpqa" data-start="4848" data-end="4867">Kiểm tra m&agrave;n h&igrave;nh</li>
</ul>
<hr data-start="4869" data-end="4872">
<h2 data-section-id="bfmwfb" data-start="4874" data-end="4894">Bảo h&agrave;nh sản phẩm</h2>
<p data-start="4896" data-end="4955">Thời gian bảo h&agrave;nh cụ thể phụ thuộc v&agrave;o từng loại sản phẩm:</p>
<ul data-start="4957" data-end="5108">
<li data-section-id="scnfo7" data-start="4957" data-end="5024">M&aacute;y mới ch&iacute;nh h&atilde;ng: Theo ch&iacute;nh s&aacute;ch của Apple hoặc nh&agrave; ph&acirc;n phối.</li>
<li data-section-id="1qpwtbz" data-start="5025" data-end="5108">M&aacute;y đ&atilde; qua sử dụng: Theo thời gian bảo h&agrave;nh ghi tr&ecirc;n h&oacute;a đơn hoặc phiếu bảo h&agrave;nh.</li>
</ul>
<hr data-start="5110" data-end="5113">
<h2 data-section-id="1k4r3q0" data-start="5115" data-end="5137">Hỗ trợ sau bảo h&agrave;nh</h2>
<p data-start="5139" data-end="5197">Ngay cả khi đ&atilde; hết thời hạn bảo h&agrave;nh, kh&aacute;ch h&agrave;ng vẫn được:</p>
<ul data-start="5199" data-end="5295">
<li data-section-id="tkzh95" data-start="5199" data-end="5218">Kiểm tra miễn ph&iacute;</li>
<li data-section-id="60o8ff" data-start="5219" data-end="5236">Tư vấn sửa chữa</li>
<li data-section-id="174prj5" data-start="5237" data-end="5256">B&aacute;o gi&aacute; minh bạch</li>
<li data-section-id="bhth0q" data-start="5257" data-end="5277">Hỗ trợ vệ sinh m&aacute;y</li>
<li data-section-id="48dzxe" data-start="5278" data-end="5295">Hỗ trợ phần mềm</li>
</ul>
<hr data-start="5297" data-end="5300">
<h2 data-section-id="gec1nh" data-start="5302" data-end="5320">Cam kết dịch vụ</h2>
<ul data-start="5322" data-end="5527">
<li data-section-id="16u6vfl" data-start="5322" data-end="5346">Tiếp nhận nhanh ch&oacute;ng.</li>
<li data-section-id="11595gn" data-start="5347" data-end="5368">Kiểm tra minh bạch.</li>
<li data-section-id="nlggs5" data-start="5369" data-end="5434">Kh&ocirc;ng tự &yacute; thay linh kiện khi chưa c&oacute; sự đồng &yacute; của kh&aacute;ch h&agrave;ng.</li>
<li data-section-id="thrwe3" data-start="5435" data-end="5464">B&aacute;o gi&aacute; trước khi sửa chữa.</li>
<li data-section-id="t7jegm" data-start="5465" data-end="5527">Sử dụng linh kiện chất lượng theo thỏa thuận với kh&aacute;ch h&agrave;ng.</li>
</ul>
<hr data-start="5529" data-end="5532">
<h2 data-section-id="1ad10pv" data-start="5534" data-end="5555">Điều kiện bảo h&agrave;nh</h2>
<p data-start="5557" data-end="5588">Bảo h&agrave;nh kh&ocirc;ng &aacute;p dụng đối với:</p>
<ul data-start="5590" data-end="5787">
<li data-section-id="vv3om6" data-start="5590" data-end="5621">Rơi vỡ do t&aacute;c động b&ecirc;n ngo&agrave;i.</li>
<li data-section-id="dbc92m" data-start="5622" data-end="5641">V&agrave;o nước, ẩm mốc.</li>
<li data-section-id="nmz5s3" data-start="5642" data-end="5677">Ch&aacute;y nổ do sử dụng sai hướng dẫn.</li>
<li data-section-id="z9ds6m" data-start="5678" data-end="5724">Tự &yacute; th&aacute;o m&aacute;y hoặc sửa chữa tại đơn vị kh&aacute;c.</li>
<li data-section-id="1xy9rjj" data-start="5725" data-end="5787">Tem bảo h&agrave;nh bị r&aacute;ch hoặc bị can thiệp (nếu c&oacute; &aacute;p dụng tem).</li>
</ul>', 1, '2026-07-20T10:44:03.291', '2026-07-24T11:06:11.179', NULL, 0),
(3, N'Chính sách thanh toán', N'chinh-sach-thanh-toan', N'<h2>Chính sách thanh toán</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(4, N'Chính sách kiểm hàng', N'chinh-sach-kiem-hang', N'<h2>Chính sách kiểm hàng</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(5, N'Chính sách mua hàng', N'chinh-sach-mua-hang', N'<h2>Chính sách mua hàng</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(6, N'Chính sách vận chuyển', N'chinh-sach-van-chuyen', N'<h2>Chính sách vận chuyển</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(7, N'Chính sách bảo mật', N'chinh-sach-bao-mat', N'<h2>Chính sách bảo mật</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(8, N'Chính sách trả góp', N'chinh-sach-tra-gop', N'<h2>Chính sách trả góp</h2><p>Nội dung mẫu tự viết cho dự án học tập Apple Store Uyên NTT. Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p><p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(9, N'Bảng giá thu cũ đổi mới', N'bang-gia-thu-cu-doi-moi', N'<p><strong>Nâng cấp lên thiết bị mới chưa bao giờ dễ dàng đến thế!</strong> Apple Store Uyên NTT nhận đánh giá và trợ giá thiết bị cũ của bạn ngay tại chỗ, giúp giảm đáng kể chi phí khi lên đời máy mới. Quy trình minh bạch, định giá công khai, không phí ẩn.</p>

<h3>Tại sao nên thu cũ đổi mới tại Apple Store Uyên NTT?</h3>
<ul>
  <li><strong>Giá công bằng:</strong> đánh giá dựa trên tình trạng thực tế máy (98%, 99%, 100%), so sánh thị trường khách quan.</li>
  <li><strong>Quy trình nhanh:</strong> kiểm tra trong 5 phút, báo giá ngay, không chờ đợi lâu.</li>
  <li><strong>Không phí tiền:</strong> khách thay ý định không mua máy mới vẫn không tính phí kiểm tra.</li>
  <li><strong>Trực tiếp tại cửa hàng:</strong> khách chứng kiến toàn bộ quá trình đánh giá, minh bạch từ đầu đến cuối.</li>
</ul>

<h3>Bảng giá tham khảo</h3>
<p><em>Giá dưới đây là <strong>mức tham khảo</strong> cho dự án học tập, phụ thuộc tình trạng máy thực tế — những chiếc máy "chưa dùng bao giờ" (100%) hoặc có vết trầy xước đáng kể sẽ được điều chỉnh giá.</em></p>

<h4>iPhone — Từ iPhone 11 đến iPhone 17</h4>
<table>
  <thead>
    <tr>
      <th>Model</th>
      <th style="text-align: center;">Tình trạng 100%</th>
      <th style="text-align: center;">Tình trạng 99%</th>
      <th style="text-align: center;">Tình trạng 98%</th>
      <th>Ghi chú</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>iPhone 17 Pro Max 256GB</td>
      <td style="text-align: right;">18.500.000đ</td>
      <td style="text-align: right;">17.200.000đ</td>
      <td style="text-align: right;">15.900.000đ</td>
      <td>Máy mới nhất</td>
    </tr>
    <tr>
      <td>iPhone 17 Pro 128GB</td>
      <td style="text-align: right;">15.800.000đ</td>
      <td style="text-align: right;">14.600.000đ</td>
      <td style="text-align: right;">13.400.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 16 Pro Max 256GB</td>
      <td style="text-align: right;">16.200.000đ</td>
      <td style="text-align: right;">15.100.000đ</td>
      <td style="text-align: right;">13.900.000đ</td>
      <td>Bán chạy</td>
    </tr>
    <tr>
      <td>iPhone 16 Pro 128GB</td>
      <td style="text-align: right;">13.900.000đ</td>
      <td style="text-align: right;">12.900.000đ</td>
      <td style="text-align: right;">11.900.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 15 Pro Max 256GB</td>
      <td style="text-align: right;">13.500.000đ</td>
      <td style="text-align: right;">12.600.000đ</td>
      <td style="text-align: right;">11.600.000đ</td>
      <td>Giảm dần theo thế hệ</td>
    </tr>
    <tr>
      <td>iPhone 15 Pro 128GB</td>
      <td style="text-align: right;">11.200.000đ</td>
      <td style="text-align: right;">10.400.000đ</td>
      <td style="text-align: right;">9.500.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 15 128GB</td>
      <td style="text-align: right;">9.800.000đ</td>
      <td style="text-align: right;">9.100.000đ</td>
      <td style="text-align: right;">8.300.000đ</td>
      <td>Màn hình 6.1 inch</td>
    </tr>
    <tr>
      <td>iPhone 14 Pro Max 256GB</td>
      <td style="text-align: right;">11.200.000đ</td>
      <td style="text-align: right;">10.400.000đ</td>
      <td style="text-align: right;">9.500.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 13 128GB</td>
      <td style="text-align: right;">6.800.000đ</td>
      <td style="text-align: right;">6.200.000đ</td>
      <td style="text-align: right;">5.600.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 12 64GB</td>
      <td style="text-align: right;">5.200.000đ</td>
      <td style="text-align: right;">4.700.000đ</td>
      <td style="text-align: right;">4.200.000đ</td>
      <td></td>
    </tr>
    <tr>
      <td>iPhone 11 64GB</td>
      <td style="text-align: right;">3.900.000đ</td>
      <td style="text-align: right;">3.500.000đ</td>
      <td style="text-align: right;">3.100.000đ</td>
      <td>Giá thấp hơn</td>
    </tr>
  </tbody>
</table>

<h4>MacBook — Air &amp; Pro</h4>
<table>
  <thead>
    <tr>
      <th>Model</th>
      <th style="text-align: center;">Tình trạng 100%</th>
      <th style="text-align: center;">Tình trạng 99%</th>
      <th style="text-align: center;">Tình trạng 98%</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>MacBook Air M3 15 inch 16GB/256GB</td>
      <td style="text-align: right;">18.500.000đ</td>
      <td style="text-align: right;">17.300.000đ</td>
      <td style="text-align: right;">16.100.000đ</td>
    </tr>
    <tr>
      <td>MacBook Air M2 13 inch 8GB/256GB</td>
      <td style="text-align: right;">13.200.000đ</td>
      <td style="text-align: right;">12.300.000đ</td>
      <td style="text-align: right;">11.400.000đ</td>
    </tr>
    <tr>
      <td>MacBook Air M1 13 inch 8GB/256GB</td>
      <td style="text-align: right;">10.200.000đ</td>
      <td style="text-align: right;">9.500.000đ</td>
      <td style="text-align: right;">8.800.000đ</td>
    </tr>
    <tr>
      <td>MacBook Pro M4 14 inch 16GB/512GB</td>
      <td style="text-align: right;">22.500.000đ</td>
      <td style="text-align: right;">20.900.000đ</td>
      <td style="text-align: right;">19.500.000đ</td>
    </tr>
    <tr>
      <td>MacBook Pro M3 14 inch 8GB/512GB</td>
      <td style="text-align: right;">18.800.000đ</td>
      <td style="text-align: right;">17.500.000đ</td>
      <td style="text-align: right;">16.200.000đ</td>
    </tr>
  </tbody>
</table>

<h4>iPad — Pro, Air &amp; Mini</h4>
<table>
  <thead>
    <tr>
      <th>Model</th>
      <th style="text-align: center;">Tình trạng 100%</th>
      <th style="text-align: center;">Tình trạng 99%</th>
      <th style="text-align: center;">Tình trạng 98%</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>iPad Pro 11 inch M4 WiFi 256GB</td>
      <td style="text-align: right;">15.200.000đ</td>
      <td style="text-align: right;">14.100.000đ</td>
      <td style="text-align: right;">13.000.000đ</td>
    </tr>
    <tr>
      <td>iPad Air 11 inch M2 WiFi 256GB</td>
      <td style="text-align: right;">10.500.000đ</td>
      <td style="text-align: right;">9.800.000đ</td>
      <td style="text-align: right;">9.100.000đ</td>
    </tr>
    <tr>
      <td>iPad Mini 7 WiFi 128GB</td>
      <td style="text-align: right;">7.800.000đ</td>
      <td style="text-align: right;">7.200.000đ</td>
      <td style="text-align: right;">6.600.000đ</td>
    </tr>
    <tr>
      <td>iPad 10.9 inch Gen 10 WiFi 64GB</td>
      <td style="text-align: right;">5.800.000đ</td>
      <td style="text-align: right;">5.400.000đ</td>
      <td style="text-align: right;">4.900.000đ</td>
    </tr>
  </tbody>
</table>

<h4>Apple Watch &amp; AirPods</h4>
<table>
  <thead>
    <tr>
      <th>Model</th>
      <th style="text-align: center;">Tình trạng 100%</th>
      <th style="text-align: center;">Tình trạng 99%</th>
      <th style="text-align: center;">Tình trạng 98%</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Apple Watch Series 10 46mm GPS</td>
      <td style="text-align: right;">6.500.000đ</td>
      <td style="text-align: right;">6.000.000đ</td>
      <td style="text-align: right;">5.500.000đ</td>
    </tr>
    <tr>
      <td>Apple Watch Ultra 2 49mm</td>
      <td style="text-align: right;">11.500.000đ</td>
      <td style="text-align: right;">10.700.000đ</td>
      <td style="text-align: right;">9.900.000đ</td>
    </tr>
    <tr>
      <td>AirPods Pro 2 (USB-C)</td>
      <td style="text-align: right;">3.500.000đ</td>
      <td style="text-align: right;">3.200.000đ</td>
      <td style="text-align: right;">2.900.000đ</td>
    </tr>
    <tr>
      <td>AirPods 4</td>
      <td style="text-align: right;">2.200.000đ</td>
      <td style="text-align: right;">2.000.000đ</td>
      <td style="text-align: right;">1.800.000đ</td>
    </tr>
  </tbody>
</table>

<h3>Ví dụ: Lên đời từ iPhone 15 Pro Max lên iPhone 17 Pro Max</h3>
<p>Bạn có chiếc <strong>iPhone 15 Pro Max tình trạng 99%</strong>, muốn lên đời lên <strong>iPhone 17 Pro Max</strong>.</p>
<ol>
  <li>Giá gốc iPhone 17 Pro Max: <strong>37.990.000đ</strong></li>
  <li>Khách sở hữu iPhone 15 Pro Max 99%: shop trợ giá <strong>12.600.000đ</strong></li>
  <li><strong>Chi phí thực tế lên đời: 37.990.000 − 12.600.000 = 25.390.000đ</strong></li>
  <li>Có thể trả góp 0% qua thẻ tín dụng, giảm còn ~2 triệu/tháng</li>
</ol>

<h3>Quy trình đánh giá tại cửa hàng</h3>
<ol>
  <li><strong>Kiểm tra ngoại hình:</strong> bề mặt kính, viền khung, kiểm tra vết trầy xước hay nứt vỡ.</li>
  <li><strong>Kiểm tra chức năng:</strong> màn hình, loa, mic, cảm biến, dung lượng pin còn lại.</li>
  <li><strong>Xác định tình trạng:</strong> 100% (chưa qua sử dụng) / 99% (dùng nhẹ, đẹp) / 98% (có vết trầy nhẹ).</li>
  <li><strong>Báo giá tại chỗ:</strong> đối chiếu bảng giá tham khảo và báo mức trợ giá cụ thể cho khách ngay lập tức.</li>
</ol>

<h3>Điều kiện &amp; Lưu ý</h3>
<ul>
  <li><strong>Máy phải là hàng chính hãng Apple</strong> — không nhận máy dựng, hàng nhái, máy đã thay main.</li>
  <li><strong>Máy phải mở nguồn được, pin còn hoạt động</strong> — máy không lên nguồn hoặc pin chai nặng có mức trợ giá thấp hơn.</li>
  <li><strong>Không nhận máy vào nước nặng</strong> — vào nước dễ gây hỏng linh kiện, có thể bị từ chối hoặc giảm giá sâu.</li>
  <li><strong>Giá chốt sau khi ký xác nhận không thay đổi</strong> — khách nên kiểm tra kỹ càng ngay từ đầu để tránh tranh chấp.</li>
  <li><strong>Thanh toán bằng tiền mặt hoặc chuyển khoản</strong> — tiền trợ giá được trừ thẳng vào giá máy mới khi thanh toán.</li>
</ul>

<h3>Còn câu hỏi nào khác?</h3>
<p>Liên hệ trực tiếp qua <strong>hotline 0900.000.000</strong> hoặc ghé cửa hàng để được tư vấn chi tiết. Chúng tôi hỗ trợ <strong>trả góp 0%</strong> lên máy mới, giúp bạn lên đời mà không cần bỏ ra quá nhiều tiền ngay.</p>
', 1, '2026-07-20T10:44:03.291', '2026-07-27T16:14:42.916', NULL, 0),
(10, N'Máy mới', N'may-moi', N'<h1 data-section-id="zrtpvt" data-start="1981" data-end="2004">Ch&iacute;nh s&aacute;ch m&aacute;y mới<span class="PDq2pG_selectionAnchor" aria-hidden="true"></span></h1>
<h2 data-section-id="go9ccq" data-start="2006" data-end="2036">Cam kết sản phẩm ch&iacute;nh h&atilde;ng</h2>
<p data-start="2038" data-end="2121">To&agrave;n bộ iPhone mới tại cửa h&agrave;ng đều được kiểm tra kỹ trước khi giao đến kh&aacute;ch h&agrave;ng.</p>
<hr data-start="2123" data-end="2126">
<h2 data-section-id="1gyfp81" data-start="2128" data-end="2140">Nguồn gốc</h2>
<p data-start="2142" data-end="2161">Ch&uacute;ng t&ocirc;i cung cấp:</p>
<ul data-start="2163" data-end="2256">
<li data-section-id="snh9yh" data-start="2163" data-end="2187">iPhone ch&iacute;nh h&atilde;ng VN/A</li>
<li data-section-id="8loaps" data-start="2188" data-end="2204">iPhone quốc tế</li>
<li data-section-id="1xxibur" data-start="2205" data-end="2256">iPhone nhập khẩu ch&iacute;nh ngạch (t&ugrave;y từng phi&ecirc;n bản)</li>
</ul>
<p data-start="2258" data-end="2306">Th&ocirc;ng tin nguồn gốc lu&ocirc;n được c&ocirc;ng khai r&otilde; r&agrave;ng.</p>
<hr data-start="2308" data-end="2311">
<h2 data-section-id="ydwqcm" data-start="2313" data-end="2334">Cam kết chất lượng</h2>
<p data-start="2336" data-end="2341">100%:</p>
<ul data-start="2343" data-end="2463">
<li data-section-id="1p552em" data-start="2343" data-end="2386">Mới nguy&ecirc;n seal (đối với m&aacute;y nguy&ecirc;n seal)</li>
<li data-section-id="6trb8t" data-start="2387" data-end="2403">Chưa k&iacute;ch hoạt</li>
<li data-section-id="1c8ukf5" data-start="2404" data-end="2422">Chưa qua sử dụng</li>
<li data-section-id="4ta38g" data-start="2423" data-end="2440">Kh&ocirc;ng t&acirc;n trang</li>
<li data-section-id="bz54hm" data-start="2441" data-end="2463">Kh&ocirc;ng thay linh kiện</li>
</ul>
<hr data-start="2465" data-end="2468">
<h2 data-section-id="actx5f" data-start="2470" data-end="2481">Phụ kiện</h2>
<p data-start="2483" data-end="2516">Đi k&egrave;m theo ti&ecirc;u chuẩn của Apple:</p>
<ul data-start="2518" data-end="2567">
<li data-section-id="jcs7mz" data-start="2518" data-end="2527">C&aacute;p sạc</li>
<li data-section-id="p8crjd" data-start="2528" data-end="2548">T&agrave;i liệu hướng dẫn</li>
<li data-section-id="1fs9dyj" data-start="2549" data-end="2567">Que SIM (nếu c&oacute;)</li>
</ul>
<hr data-start="2569" data-end="2572">
<h2 data-section-id="hcwd2n" data-start="2574" data-end="2600">Kiểm tra trước khi nhận</h2>
<p data-start="2602" data-end="2627">Kh&aacute;ch h&agrave;ng được kiểm tra:</p>
<ul data-start="2629" data-end="2728">
<li data-section-id="1j3w2cg" data-start="2629" data-end="2635">IMEI</li>
<li data-section-id="6c6q2w" data-start="2636" data-end="2644">Serial</li>
<li data-section-id="k8yoe8" data-start="2645" data-end="2661">Bảo h&agrave;nh Apple</li>
<li data-section-id="15b23d0" data-start="2662" data-end="2674">Ngoại h&igrave;nh</li>
<li data-section-id="1tmtqnd" data-start="2675" data-end="2685">M&agrave;n h&igrave;nh</li>
<li data-section-id="1uxv8b5" data-start="2686" data-end="2694">Camera</li>
<li data-section-id="19edmys" data-start="2695" data-end="2704">Face ID</li>
<li data-section-id="1o474a" data-start="2705" data-end="2710">Loa</li>
<li data-section-id="1o4cun" data-start="2711" data-end="2716">Mic</li>
<li data-section-id="1o0xg9" data-start="2717" data-end="2722">Sạc</li>
<li data-section-id="1o4nof" data-start="2723" data-end="2728">Pin</li>
</ul>
<p data-start="2730" data-end="2758">Chỉ thanh to&aacute;n khi h&agrave;i l&ograve;ng.</p>
<hr data-start="2760" data-end="2763">
<h2 data-section-id="174hq0v" data-start="2765" data-end="2774">Hỗ trợ</h2>
<p data-start="2776" data-end="2785">Miễn ph&iacute;:</p>
<ul data-start="2787" data-end="2900">
<li data-section-id="1jcu7ni" data-start="2787" data-end="2803">Chuyển dữ liệu</li>
<li data-section-id="9x5d7c" data-start="2804" data-end="2822">C&agrave;i đặt ứng dụng</li>
<li data-section-id="1opgdyk" data-start="2823" data-end="2843">Đăng nhập Apple ID</li>
<li data-section-id="9bco6g" data-start="2844" data-end="2880">D&aacute;n m&agrave;n h&igrave;nh (nếu c&oacute; chương tr&igrave;nh)</li>
<li data-section-id="1vy1wd1" data-start="2881" data-end="2900">Hướng dẫn sử dụng</li>
</ul>
<hr data-start="2902" data-end="2905">
<h2 data-section-id="fh8t6c" data-start="2907" data-end="2928">Ch&iacute;nh s&aacute;ch đổi trả</h2>
<p data-start="2930" data-end="2949">Hỗ trợ đổi mới nếu:</p>
<ul data-start="2951" data-end="3020">
<li data-section-id="1gpbf53" data-start="2951" data-end="2982">Lỗi phần cứng do nh&agrave; sản xuất</li>
<li data-section-id="pp5ez3" data-start="2983" data-end="3020">Đủ điều kiện theo quy định bảo h&agrave;nh</li>
</ul>', 1, '2026-07-20T10:44:03.291', '2026-07-24T11:05:38.867', NULL, 0),
(11, N'Máy đã qua sử dụng', N'may-da-qua-su-dung', N'<h1 data-section-id="14gtk9i" data-start="3027" data-end="3061">Ch&iacute;nh s&aacute;ch m&aacute;y đ&atilde; qua sử dụng<span class="PDq2pG_selectionAnchor" aria-hidden="true"></span></h1>
<h2 data-section-id="14ffrw4" data-start="3063" data-end="3095">Cam kết m&aacute;y cũ chất lượng cao</h2>
<p data-start="3097" data-end="3187">Tất cả iPhone đ&atilde; qua sử dụng đều trải qua quy tr&igrave;nh kiểm tra nghi&ecirc;m ngặt trước khi b&aacute;n ra.</p>
<hr data-start="3189" data-end="3192">
<h2 data-section-id="13zxtpg" data-start="3194" data-end="3216">Quy tr&igrave;nh kiểm định</h2>
<p data-start="3218" data-end="3261">Mỗi thiết bị được kiểm tra hơn 30 hạng mục:</p>
<ul data-start="3263" data-end="3418">
<li data-section-id="1tmtqnd" data-start="3263" data-end="3273">M&agrave;n h&igrave;nh</li>
<li data-section-id="ophv1b" data-start="3274" data-end="3288">Camera trước</li>
<li data-section-id="188uely" data-start="3289" data-end="3301">Camera sau</li>
<li data-section-id="19edmys" data-start="3302" data-end="3311">Face ID</li>
<li data-section-id="iev18g" data-start="3312" data-end="3322">Touch ID</li>
<li data-section-id="1o474a" data-start="3323" data-end="3328">Loa</li>
<li data-section-id="16vzbde" data-start="3329" data-end="3336">Micro</li>
<li data-section-id="1j4gfw9" data-start="3337" data-end="3343">Wifi</li>
<li data-section-id="1bhqjpa" data-start="3344" data-end="3355">Bluetooth</li>
<li data-section-id="1o4ckj" data-start="3356" data-end="3361">NFC</li>
<li data-section-id="1ob18hr" data-start="3362" data-end="3372">Cảm biến</li>
<li data-section-id="1o0xg9" data-start="3373" data-end="3378">Sạc</li>
<li data-section-id="1o4nof" data-start="3379" data-end="3384">Pin</li>
<li data-section-id="gscjr6" data-start="3385" data-end="3394">N&uacute;t bấm</li>
<li data-section-id="u0jepw" data-start="3395" data-end="3405">Khay SIM</li>
<li data-section-id="1j4e492" data-start="3406" data-end="3412">Rung</li>
<li data-section-id="1o4hf0" data-start="3413" data-end="3418">GPS</li>
</ul>
<hr data-start="3420" data-end="3423">
<h2 data-section-id="r5rhoa" data-start="3425" data-end="3437">Ph&acirc;n loại</h2>
<h3 data-section-id="1ar0ed0" data-start="3439" data-end="3455">Like New 99%</h3>
<ul data-start="3457" data-end="3507">
<li data-section-id="3yu8f" data-start="3457" data-end="3470">Gần như mới</li>
<li data-section-id="100lgsc" data-start="3471" data-end="3490">Trầy xước rất nhẹ</li>
<li data-section-id="8drvvg" data-start="3491" data-end="3507">Ngoại h&igrave;nh đẹp</li>
</ul>
<hr data-start="3509" data-end="3512">
<h3 data-section-id="1xxo91b" data-start="3514" data-end="3521">Đẹp</h3>
<ul data-start="3523" data-end="3565">
<li data-section-id="3tq9kz" data-start="3523" data-end="3544">C&oacute; v&agrave;i vết xước nhỏ</li>
<li data-section-id="8r11hy" data-start="3545" data-end="3565">Hoạt động ho&agrave;n hảo</li>
</ul>
<hr data-start="3567" data-end="3570">
<h3 data-section-id="1pp82ag" data-start="3572" data-end="3585">Tiết kiệm</h3>
<ul data-start="3587" data-end="3649">
<li data-section-id="1q1sgdh" data-start="3587" data-end="3619">Ngoại h&igrave;nh c&oacute; dấu hiệu sử dụng</li>
<li data-section-id="1e05auw" data-start="3620" data-end="3639">Hiệu năng ổn định</li>
<li data-section-id="ixxmfq" data-start="3640" data-end="3649">Gi&aacute; tốt</li>
</ul>
<hr data-start="3651" data-end="3654">
<h2 data-section-id="1hryz76" data-start="3656" data-end="3662">Pin</h2>
<p data-start="3664" data-end="3699">Dung lượng pin lu&ocirc;n được c&ocirc;ng khai.</p>
<p data-start="3701" data-end="3750">Kh&aacute;ch h&agrave;ng được kiểm tra trực tiếp trước khi mua.</p>
<hr data-start="3752" data-end="3755">
<h2 data-section-id="baowne" data-start="3757" data-end="3767">Cam kết</h2>
<p data-start="3769" data-end="3783">Kh&ocirc;ng b&aacute;n m&aacute;y:</p>
<ul data-start="3785" data-end="3854">
<li data-section-id="16rm8vs" data-start="3785" data-end="3794">B&aacute;o mất</li>
<li data-section-id="bfq3tt" data-start="3795" data-end="3808">Kh&oacute;a iCloud</li>
<li data-section-id="diauce" data-start="3809" data-end="3828">Main sửa lỗi nặng</li>
<li data-section-id="jul5tu" data-start="3829" data-end="3854">M&aacute;y dựng k&eacute;m chất lượng</li>
</ul>
<hr data-start="3856" data-end="3859">
<h2 data-section-id="174hq0v" data-start="3861" data-end="3870">Hỗ trợ</h2>
<ul data-start="3872" data-end="3961">
<li data-section-id="tkzh95" data-start="3872" data-end="3891">Kiểm tra miễn ph&iacute;</li>
<li data-section-id="1kebvjk" data-start="3892" data-end="3905">Vệ sinh m&aacute;y</li>
<li data-section-id="1jcu7ni" data-start="3906" data-end="3922">Chuyển dữ liệu</li>
<li data-section-id="9x5d7c" data-start="3923" data-end="3941">C&agrave;i đặt ứng dụng</li>
<li data-section-id="1vy1wd1" data-start="3942" data-end="3961">Hướng dẫn sử dụng</li>
</ul>
<hr data-start="3963" data-end="3966">
<h2 data-section-id="1u0fhcc" data-start="3968" data-end="3978">Đổi trả</h2>
<p data-start="3980" data-end="4090">Nếu ph&aacute;t hiện lỗi phần cứng theo điều kiện bảo h&agrave;nh, kh&aacute;ch h&agrave;ng được hỗ trợ đổi hoặc sửa chữa theo ch&iacute;nh s&aacute;ch.</p>', 1, '2026-07-20T10:44:03.291', '2026-07-24T11:05:54.042', NULL, 0),
(12, N'Bảo hành 20 năm', N'bao-hanh-20-nam', N'<h1 data-section-id="dcnq4n" data-start="4097" data-end="4128">Ch&iacute;nh s&aacute;ch bảo h&agrave;nh 20 năm<span class="PDq2pG_selectionAnchor" aria-hidden="true"></span></h1>
<blockquote data-start="4130" data-end="4435">
<p data-start="4132" data-end="4435"><strong data-start="4132" data-end="4142">Lưu &yacute;:</strong> "Bảo h&agrave;nh 20 năm" l&agrave; một th&ocirc;ng điệp marketing rất mạnh. Tr&ecirc;n thực tế, kh&ocirc;ng n&ecirc;n ghi l&agrave; bảo h&agrave;nh to&agrave;n bộ m&aacute;y trong 20 năm nếu bạn kh&ocirc;ng thể thực hiện. Thay v&agrave;o đ&oacute; n&ecirc;n diễn đạt l&agrave; <strong data-start="4321" data-end="4372">Chương tr&igrave;nh đồng h&agrave;nh &amp; hỗ trợ kỹ thuật 20 năm</strong>, c&ograve;n bảo h&agrave;nh phần cứng vẫn theo thời hạn cụ thể của sản phẩm.</p>
</blockquote>
<h2 data-section-id="1vzj523" data-start="4437" data-end="4467">Ch&iacute;nh s&aacute;ch đồng h&agrave;nh 20 năm</h2>
<p data-start="4469" data-end="4616">Ch&uacute;ng t&ocirc;i cam kết đồng h&agrave;nh c&ugrave;ng kh&aacute;ch h&agrave;ng trong suốt qu&aacute; tr&igrave;nh sử dụng thiết bị, mang đến dịch vụ hậu m&atilde;i l&acirc;u d&agrave;i v&agrave; hỗ trợ kỹ thuật nhanh ch&oacute;ng.</p>
<hr data-start="4618" data-end="4621">
<h2 data-section-id="12q2udt" data-start="4623" data-end="4635">Quyền lợi</h2>
<p data-start="4637" data-end="4653">Kh&aacute;ch h&agrave;ng được:</p>
<ul data-start="4655" data-end="4867">
<li data-section-id="1ighumk" data-start="4655" data-end="4678">Kiểm tra m&aacute;y miễn ph&iacute;</li>
<li data-section-id="1xzcbeb" data-start="4679" data-end="4696">Tư vấn kỹ thuật</li>
<li data-section-id="5l1yhy" data-start="4697" data-end="4718">Hỗ trợ cập nhật iOS</li>
<li data-section-id="150s2ey" data-start="4719" data-end="4743">Hỗ trợ sao lưu dữ liệu</li>
<li data-section-id="1ryrobw" data-start="4744" data-end="4772">Chuyển dữ liệu khi đổi m&aacute;y</li>
<li data-section-id="1b4sxii" data-start="4773" data-end="4795">Vệ sinh m&aacute;y miễn ph&iacute;</li>
<li data-section-id="11vwp3o" data-start="4796" data-end="4810">Kiểm tra pin</li>
<li data-section-id="2bd2i7" data-start="4811" data-end="4829">Kiểm tra Face ID</li>
<li data-section-id="1mg4gze" data-start="4830" data-end="4847">Kiểm tra camera</li>
<li data-section-id="4cbpqa" data-start="4848" data-end="4867">Kiểm tra m&agrave;n h&igrave;nh</li>
</ul>
<hr data-start="4869" data-end="4872">
<h2 data-section-id="bfmwfb" data-start="4874" data-end="4894">Bảo h&agrave;nh sản phẩm</h2>
<p data-start="4896" data-end="4955">Thời gian bảo h&agrave;nh cụ thể phụ thuộc v&agrave;o từng loại sản phẩm:</p>
<ul data-start="4957" data-end="5108">
<li data-section-id="scnfo7" data-start="4957" data-end="5024">M&aacute;y mới ch&iacute;nh h&atilde;ng: Theo ch&iacute;nh s&aacute;ch của Apple hoặc nh&agrave; ph&acirc;n phối.</li>
<li data-section-id="1qpwtbz" data-start="5025" data-end="5108">M&aacute;y đ&atilde; qua sử dụng: Theo thời gian bảo h&agrave;nh ghi tr&ecirc;n h&oacute;a đơn hoặc phiếu bảo h&agrave;nh.</li>
</ul>
<hr data-start="5110" data-end="5113">
<h2 data-section-id="1k4r3q0" data-start="5115" data-end="5137">Hỗ trợ sau bảo h&agrave;nh</h2>
<p data-start="5139" data-end="5197">Ngay cả khi đ&atilde; hết thời hạn bảo h&agrave;nh, kh&aacute;ch h&agrave;ng vẫn được:</p>
<ul data-start="5199" data-end="5295">
<li data-section-id="tkzh95" data-start="5199" data-end="5218">Kiểm tra miễn ph&iacute;</li>
<li data-section-id="60o8ff" data-start="5219" data-end="5236">Tư vấn sửa chữa</li>
<li data-section-id="174prj5" data-start="5237" data-end="5256">B&aacute;o gi&aacute; minh bạch</li>
<li data-section-id="bhth0q" data-start="5257" data-end="5277">Hỗ trợ vệ sinh m&aacute;y</li>
<li data-section-id="48dzxe" data-start="5278" data-end="5295">Hỗ trợ phần mềm</li>
</ul>
<hr data-start="5297" data-end="5300">
<h2 data-section-id="gec1nh" data-start="5302" data-end="5320">Cam kết dịch vụ</h2>
<ul data-start="5322" data-end="5527">
<li data-section-id="16u6vfl" data-start="5322" data-end="5346">Tiếp nhận nhanh ch&oacute;ng.</li>
<li data-section-id="11595gn" data-start="5347" data-end="5368">Kiểm tra minh bạch.</li>
<li data-section-id="nlggs5" data-start="5369" data-end="5434">Kh&ocirc;ng tự &yacute; thay linh kiện khi chưa c&oacute; sự đồng &yacute; của kh&aacute;ch h&agrave;ng.</li>
<li data-section-id="thrwe3" data-start="5435" data-end="5464">B&aacute;o gi&aacute; trước khi sửa chữa.</li>
<li data-section-id="t7jegm" data-start="5465" data-end="5527">Sử dụng linh kiện chất lượng theo thỏa thuận với kh&aacute;ch h&agrave;ng.</li>
</ul>
<hr data-start="5529" data-end="5532">
<h2 data-section-id="1ad10pv" data-start="5534" data-end="5555">Điều kiện bảo h&agrave;nh</h2>
<p data-start="5557" data-end="5588">Bảo h&agrave;nh kh&ocirc;ng &aacute;p dụng đối với:</p>
<ul data-start="5590" data-end="5787">
<li data-section-id="vv3om6" data-start="5590" data-end="5621">Rơi vỡ do t&aacute;c động b&ecirc;n ngo&agrave;i.</li>
<li data-section-id="dbc92m" data-start="5622" data-end="5641">V&agrave;o nước, ẩm mốc.</li>
<li data-section-id="nmz5s3" data-start="5642" data-end="5677">Ch&aacute;y nổ do sử dụng sai hướng dẫn.</li>
<li data-section-id="z9ds6m" data-start="5678" data-end="5724">Tự &yacute; th&aacute;o m&aacute;y hoặc sửa chữa tại đơn vị kh&aacute;c.</li>
<li data-section-id="1xy9rjj" data-start="5725" data-end="5787">Tem bảo h&agrave;nh bị r&aacute;ch hoặc bị can thiệp (nếu c&oacute; &aacute;p dụng tem).</li>
</ul>', 1, '2026-07-20T10:44:03.291', '2026-07-24T11:06:37.325', NULL, 0),
(13, N'Dịch vụ sửa chữa', N'sua-chua', N'<h2>Dịch vụ sửa chữa thiết bị Apple</h2>
<p>Trung tâm kỹ thuật Apple Store Uyên NTT nhận kiểm tra và sửa chữa iPhone, iPad, MacBook,
Apple Watch và AirPods. Máy được kiểm tra miễn phí, báo giá trước khi sửa, khách đồng ý mới tiến hành.</p>

<h3>Nhóm dịch vụ chúng tôi nhận</h3>
<ul>
  <li><strong>Màn hình &amp; cảm ứng:</strong> thay màn hình, xử lý sọc màn, loạn cảm ứng, ám màu.</li>
  <li><strong>Nguồn &amp; pin:</strong> thay pin chai, sửa lỗi không lên nguồn, sập nguồn ngẫu nhiên.</li>
  <li><strong>Sạc &amp; kết nối:</strong> thay chân sạc, sửa lỗi sạc chập chờn, mất Wi-Fi/Bluetooth.</li>
  <li><strong>Camera &amp; loa mic:</strong> thay cụm camera mờ/rung, xử lý loa rè, mic không thu tiếng.</li>
  <li><strong>Phần mềm:</strong> cài đặt lại hệ điều hành, cứu dữ liệu, xử lý máy treo táo.</li>
  <li><strong>Vệ sinh &amp; bảo dưỡng:</strong> vệ sinh bo mạch, tra keo tản nhiệt MacBook, chống ẩm.</li>
</ul>

<h3>Quy trình 4 bước</h3>
<ol>
  <li><strong>Tiếp nhận:</strong> ghi nhận tình trạng máy, dán phiếu và niêm phong trước mặt khách.</li>
  <li><strong>Kiểm tra:</strong> kỹ thuật viên đo đạc, xác định lỗi và báo giá cụ thể — miễn phí kiểm tra.</li>
  <li><strong>Sửa chữa:</strong> tiến hành sau khi khách xác nhận báo giá; linh kiện thay ra được trả lại khách.</li>
  <li><strong>Bàn giao:</strong> test lại cùng khách, xuất phiếu bảo hành theo từng hạng mục.</li>
</ol>

<h3>Bảng giá tham khảo</h3>
<p>Giá dưới đây là <em>mức tham khảo cho dự án demo</em>, chi phí thực tế phụ thuộc model và tình trạng máy.</p>
<table>
  <thead>
    <tr><th>Hạng mục</th><th>Thời gian</th><th>Chi phí tham khảo</th><th>Bảo hành</th></tr>
  </thead>
  <tbody>
    <tr><td>Thay pin iPhone</td><td>30 – 45 phút</td><td>450.000 – 1.200.000đ</td><td>6 tháng</td></tr>
    <tr><td>Thay màn hình iPhone</td><td>45 – 90 phút</td><td>1.200.000 – 6.500.000đ</td><td>6 tháng</td></tr>
    <tr><td>Thay chân sạc</td><td>60 phút</td><td>350.000 – 900.000đ</td><td>3 tháng</td></tr>
    <tr><td>Thay pin MacBook</td><td>2 – 4 giờ</td><td>1.500.000 – 3.500.000đ</td><td>6 tháng</td></tr>
    <tr><td>Vệ sinh &amp; tra keo tản nhiệt MacBook</td><td>2 giờ</td><td>350.000 – 600.000đ</td><td>1 tháng</td></tr>
    <tr><td>Cài đặt lại hệ điều hành</td><td>1 – 2 giờ</td><td>150.000 – 300.000đ</td><td>—</td></tr>
  </tbody>
</table>

<h3>Cam kết của chúng tôi</h3>
<ul>
  <li>Kiểm tra và báo giá miễn phí, không sửa vẫn không mất phí.</li>
  <li>Không thay linh kiện khi chưa có xác nhận của khách hàng.</li>
  <li>Bảo hành theo hạng mục, ghi rõ trên phiếu bàn giao.</li>
  <li>Hoàn tiền nếu không khắc phục được đúng lỗi đã nhận.</li>
</ul>', 1, '2026-07-22T12:20:00.895', '2026-07-27T16:14:42.916', NULL, 0),
(14, N'Sửa chữa phần cứng', N'sua-chua-phan-cung', N'<h2>Sửa chữa phần cứng chuyên sâu</h2>
<p>Với các lỗi phần cứng nặng — vào nước, rơi vỡ, chập nguồn, mất tín hiệu — máy được đưa sang
khu vực kỹ thuật chuyên sâu để đo đạc trên sơ đồ mạch trước khi can thiệp.</p>

<h3>Các lỗi phần cứng thường gặp</h3>
<ul>
  <li><strong>Vào nước:</strong> tháo máy, vệ sinh siêu âm, xử lý oxy hóa chân linh kiện.</li>
  <li><strong>Lỗi nguồn:</strong> đo dòng tiêu thụ, dò chập, thay IC nguồn.</li>
  <li><strong>Lỗi sạc:</strong> thay IC sạc, xử lý đường tín hiệu bị đứt sau va đập.</li>
  <li><strong>Mất sóng / Wi-Fi:</strong> hàn lại hoặc thay IC thu phát tín hiệu.</li>
  <li><strong>Lỗi màn hình MacBook:</strong> thay cáp màn, xử lý lỗi backlight.</li>
  <li><strong>Lỗi bàn phím &amp; trackpad:</strong> thay cụm phím, xử lý phím liệt do vào nước.</li>
</ul>

<h3>Thiết bị kỹ thuật sử dụng</h3>
<ul>
  <li>Kính hiển vi soi mạch và máy khò nhiệt có kiểm soát nhiệt độ.</li>
  <li>Máy đo dòng, nguồn phòng thí nghiệm để dò chập trước khi cấp điện.</li>
  <li>Bể rửa siêu âm dùng cho máy vào nước.</li>
</ul>

<h3>Bảng giá tham khảo</h3>
<table>
  <thead>
    <tr><th>Hạng mục</th><th>Mức độ</th><th>Chi phí tham khảo</th><th>Bảo hành</th></tr>
  </thead>
  <tbody>
    <tr><td>Vệ sinh máy vào nước</td><td>Nhẹ</td><td>300.000 – 700.000đ</td><td>1 tháng</td></tr>
    <tr><td>Sửa lỗi nguồn (thay IC nguồn)</td><td>Nặng</td><td>800.000 – 2.500.000đ</td><td>3 tháng</td></tr>
    <tr><td>Sửa lỗi sạc (thay IC sạc)</td><td>Trung bình</td><td>600.000 – 1.500.000đ</td><td>3 tháng</td></tr>
    <tr><td>Khắc phục mất sóng / Wi-Fi</td><td>Nặng</td><td>900.000 – 2.200.000đ</td><td>3 tháng</td></tr>
    <tr><td>Thay cáp màn hình MacBook</td><td>Trung bình</td><td>1.200.000 – 2.800.000đ</td><td>6 tháng</td></tr>
    <tr><td>Thay bàn phím MacBook</td><td>Nặng</td><td>1.500.000 – 3.900.000đ</td><td>6 tháng</td></tr>
  </tbody>
</table>

<h3>Lưu ý trước khi gửi máy</h3>
<ol>
  <li>Sao lưu dữ liệu nếu máy còn mở được — sửa phần cứng luôn có rủi ro mất dữ liệu.</li>
  <li>Tắt tính năng tìm thiết bị và cung cấp mật khẩu mở máy để kỹ thuật test được đầy đủ.</li>
  <li>Máy vào nước nên mang tới càng sớm càng tốt, không tự sấy hoặc cắm sạc.</li>
  <li>Máy đã qua sửa chữa ở nơi khác vẫn nhận, nhưng thời gian xử lý có thể lâu hơn.</li>
</ol>

<p><em>Toàn bộ nội dung trang này là dữ liệu mẫu của dự án học tập, không phải cam kết dịch vụ thật.</em></p>', 1, '2026-07-22T12:20:00.895', NULL, NULL, 0);
SET IDENTITY_INSERT [dbo].[StaticPages] OFF;
GO

-- ----------------------------------------------------------------------
-- [Banners] - 5 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Banners] ON;
INSERT INTO [dbo].[Banners] ([Id], [Title], [ImageUrl], [LinkUrl], [Position], [DisplayOrder], [IsActive], [DeletedAt], [IsDeleted]) VALUES
(1, N'iPhone 17 Pro Max - Đặt trước hôm nay', N'/uploads/editor/2026-07/aa9f87bbc20c4adda942a59b442d959a.jpg', N'iphone-17-series', 0, 2, 1, NULL, 0),
(2, N'Thu cũ đổi mới - Trợ giá đến 2 triệu', N'/uploads/banners/2026-07/b04c7cb08da74acfaa2c97e1be8ccff8.jpg', N'/bang-gia-thu-cu-doi-moi', 0, 0, 1, NULL, 0),
(3, N'MacBook sale đến 30%', N'/uploads/banners/2026-07/ce79a6a16b304e59894ecfae430e05bf.jpg', N'/macbook', 0, 1, 1, NULL, 0),
(4, N'Ưu đãi iPad', N'/uploads/banners/2026-07/85e29827dd6448f6afb67b3cdd388b18.jpg', N'/ipad', 1, 1, 1, NULL, 0),
(5, N'Hàng thanh lý giá tốt', N'/uploads/banners/2026-07/614b500852af473eb294e6f1e7faf26b.jpg', N'/hang-thanh-ly', 1, 2, 1, NULL, 0);
SET IDENTITY_INSERT [dbo].[Banners] OFF;
GO

-- ----------------------------------------------------------------------
-- [Testimonials] - 5 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Testimonials] ON;
INSERT INTO [dbo].[Testimonials] ([Id], [CustomerName], [AvatarUrl], [Content], [DisplayOrder], [IsActive], [DeletedAt], [IsDeleted]) VALUES
(1, N'Anh Minh - Quận 1', N'/images/placeholder-avatar.svg', N'Mua iPhone 15 Pro Max 99% mà máy đẹp như mới, shop tư vấn nhiệt tình, giá tốt hơn nhiều nơi mình khảo sát.', 1, 1, NULL, 0),
(2, N'Chị Hương - Gò Vấp', N'/images/placeholder-avatar.svg', N'Lên đời từ iPhone 12 sang 16 Pro Max, được trợ giá thu cũ khá cao. Thủ tục trả góp nhanh gọn.', 2, 1, NULL, 0),
(3, N'Anh Tuấn - Thủ Đức', N'/images/placeholder-avatar.svg', N'MacBook Air M1 thanh lý mà pin còn tốt, ngoại hình 96% đúng mô tả. Sẽ ủng hộ tiếp.', 3, 1, NULL, 0),
(4, N'Chị Lan - Bình Thạnh', N'/images/placeholder-avatar.svg', N'Nhân viên kiểm tra máy kỹ trước mặt mình, bảo hành rõ ràng nên rất yên tâm.', 4, 1, NULL, 0),
(5, N'Anh Khoa - Tân Bình', N'/images/placeholder-avatar.svg', N'Đặt hàng online, giao trong ngày, máy nguyên seal đúng cam kết. Rất hài lòng.', 5, 1, NULL, 0);
SET IDENTITY_INSERT [dbo].[Testimonials] OFF;
GO

-- ----------------------------------------------------------------------
-- [MenuItems] - 24 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[MenuItems] ON;
INSERT INTO [dbo].[MenuItems] ([Id], [Group], [Type], [Text], [Url], [Icon], [ParentId], [DisplayOrder], [IsActive], [OpenInNewTab], [DeletedAt], [IsDeleted]) VALUES
(1, 0, 0, N'Trang chủ', N'/', N'bi-house-door', NULL, 1, 1, 0, NULL, 0),
(2, 0, 0, N'Giới thiệu', N'/gioi-thieu', NULL, NULL, 2, 1, 0, NULL, 0),
(3, 0, 1, N'Sản phẩm', N'/san-pham', N'bi-grid', NULL, 3, 1, 0, NULL, 0),
(4, 0, 0, N'Sửa chữa', N'/sua-chua', N'bi-tools', NULL, 4, 1, 0, NULL, 0),
(5, 0, 0, N'Chính sách', N'/chinh-sach-bao-hanh', N'bi-shield-check', NULL, 5, 1, 0, NULL, 0),
(6, 0, 0, N'Tin công nghệ', N'/tin-tuc', N'bi-newspaper', NULL, 6, 1, 0, NULL, 0),
(7, 0, 0, N'Liên hệ', N'/lien-he', NULL, NULL, 8, 1, 0, NULL, 0),
(8, 1, 0, N'Chính sách bảo hành', N'/chinh-sach-bao-hanh', NULL, NULL, 1, 1, 0, NULL, 0),
(9, 1, 0, N'Chính sách thanh toán', N'/chinh-sach-thanh-toan', NULL, NULL, 2, 1, 0, NULL, 0),
(10, 1, 0, N'Chính sách vận chuyển', N'/chinh-sach-van-chuyen', NULL, NULL, 3, 1, 0, NULL, 0),
(11, 1, 0, N'Chính sách kiểm hàng', N'/chinh-sach-kiem-hang', NULL, NULL, 4, 1, 0, NULL, 0),
(12, 1, 0, N'Chính sách trả góp', N'/chinh-sach-tra-gop', NULL, NULL, 5, 1, 0, NULL, 0),
(13, 1, 0, N'Chính sách bảo mật', N'/chinh-sach-bao-mat', NULL, NULL, 6, 1, 0, NULL, 0),
(14, 2, 0, N'Giới thiệu', N'/gioi-thieu', NULL, NULL, 1, 1, 0, NULL, 0),
(15, 2, 0, N'Tin công nghệ', N'/tin-tuc', NULL, NULL, 2, 1, 0, NULL, 0),
(16, 2, 0, N'Sửa chữa', N'/sua-chua', NULL, NULL, 3, 1, 0, NULL, 0),
(17, 2, 0, N'Giỏ hàng', N'/gio-hang', NULL, NULL, 4, 1, 0, NULL, 0),
(18, 2, 0, N'Liên hệ', N'/lien-he', NULL, NULL, 5, 1, 0, NULL, 0),
(19, 0, 0, N'Dịch vụ sửa chữa', N'/sua-chua', N'bi-tools', 4, 1, 1, 0, NULL, 0),
(20, 0, 0, N'Sửa chữa phần cứng', N'/sua-chua-phan-cung', N'bi-cpu', 4, 2, 1, 0, NULL, 0),
(21, 0, 0, N'Bảng giá thu cũ đổi mới', N'/bang-gia-thu-cu-doi-moi', N'bi-shield-check', 5, 1, 1, 0, NULL, 0),
(22, 0, 0, N'Máy mới', N'/may-moi', N'bi-shield-check', 5, 2, 1, 0, NULL, 0),
(23, 0, 0, N'Máy đã qua sử dụng', N'/may-da-qua-su-dung', N'bi-shield-check', 5, 3, 1, 0, NULL, 0),
(24, 0, 0, N'Bảo hành 20 năm', N'/bao-hanh-20-nam', N'bi-shield-check', 5, 4, 1, 0, NULL, 0);
SET IDENTITY_INSERT [dbo].[MenuItems] OFF;
GO

-- ----------------------------------------------------------------------
-- [ContentBlocks] - 9 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ContentBlocks] ON;
INSERT INTO [dbo].[ContentBlocks] ([Id], [GroupKey], [Icon], [Title], [Subtitle], [LinkUrl], [ImageUrl], [DisplayOrder], [IsActive], [DeletedAt], [IsDeleted]) VALUES
(1, N'product.commitments', N'bi-patch-check-fill', N'Hàng chính hãng, nguyên seal / đúng mô tả', NULL, NULL, NULL, 1, 1, NULL, 0),
(2, N'product.commitments', N'bi-arrow-repeat', N'Thu cũ đổi mới, thu giá cao', NULL, NULL, NULL, 2, 1, NULL, 0),
(3, N'product.commitments', N'bi-shield-check', N'Bảo hành dài hạn, 1 đổi 1', NULL, NULL, NULL, 3, 1, NULL, 0),
(4, N'product.commitments', N'bi-truck', N'Giao nhanh nội thành, kiểm tra khi nhận', NULL, NULL, NULL, 4, 1, NULL, 0),
(5, N'cart.assurance', N'bi-shield-check', N'Thu cũ đổi mới · Bảo hành dài hạn', NULL, NULL, NULL, 1, 1, NULL, 0),
(6, N'contact.channels', N'bi-telephone-fill', N'Hotline', N'0900.000.000', N'tel:0900000000', NULL, 1, 1, NULL, 0),
(7, N'contact.channels', N'bi-chat-dots-fill', N'Zalo', N'Chat ngay', N'https://zalo.me/', NULL, 2, 1, NULL, 0),
(8, N'contact.channels', N'bi-messenger', N'Messenger', N'Chat ngay', N'https://m.me/', NULL, 3, 1, NULL, 0),
(9, N'contact.channels', N'bi-geo-alt-fill', N'Cửa hàng', N'Chỉ đường', N'https://maps.google.com/', NULL, 4, 1, NULL, 0);
SET IDENTITY_INSERT [dbo].[ContentBlocks] OFF;
GO

-- ----------------------------------------------------------------------
-- [SiteSettings] - 26 dong
-- ----------------------------------------------------------------------
INSERT INTO [dbo].[SiteSettings] ([Key], [Value]) VALUES
(N'Address', N'123 Đường Số 1, Phường Demo, TP.HCM123'),
(N'BankAccountName', N'APPLE STORE UYEN NTT'),
(N'BankAccountNumber', N'0000 0000 0000'),
(N'BankName', N'Demo Bank (ngân hàng giả lập)'),
(N'BankTransferNoteTemplate', N'Thanh toan don {0}'),
(N'ContactIntro', N'Bạn cần tư vấn chọn máy, báo giá thu cũ đổi mới hay hỗ trợ bảo hành? Để lại thông tin bên dưới, shop phản hồi ngay trong giờ làm việc.'),
(N'ContactSubjects', N'Tư vấn mua hàng|Thu cũ đổi mới|Bảo hành|Sửa chữa|Khiếu nại|Khác'),
(N'Email', N'lienhe@applestoreuyenntt.local'),
(N'FacebookUrl', N'https://facebook.com/'),
(N'FooterAbout', N'Chuyên iPhone, MacBook, iPad, Apple Watch, AirPods chính hãng — mới & đã qua sử dụng.'),
(N'FooterDisclaimer', N'Website demo phục vụ mục đích học tập — không phải trang thương mại thật.'),
(N'Hotline', N'0900.000.000'),
(N'LogoDarkUrl', NULL),
(N'LogoHighlight', N'Uyên NTT'),
(N'LogoUrl', NULL),
(N'MapEmbedUrl', NULL),
(N'MapUrl', N'https://maps.google.com/'),
(N'MessengerUrl', N'https://m.me/'),
(N'NewsPageIntro', N'Tin tức, đánh giá và mẹo dùng thiết bị Apple — cập nhật mỗi tuần.'),
(N'NewsPageTitle', N'Tin công nghệ'),
(N'OpenHours', N'8h30 - 21h30 (T2 - CN)'),
(N'ShippingNote', N'Shop báo khi xác nhận'),
(N'StoreName', N'Apple Store Uyên NTT'),
(N'Tagline', N'Thu cũ đổi mới - Lên đời dễ dàng'),
(N'TaxCode', N'0000000000'),
(N'ZaloUrl', N'https://zalo.me/');
GO

-- ----------------------------------------------------------------------
-- [ContactMessages] - 1 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ContactMessages] ON;
INSERT INTO [dbo].[ContactMessages] ([Id], [FullName], [Phone], [Address], [Email], [Subject], [Content], [AttachmentPath], [IsRead], [CreatedAt], [UpdatedAt], [DeletedAt], [IsDeleted]) VALUES
(5, N'Dat', N'0912345678', N'TPHCM', N'khachhang@demo.local', N'Tư vấn mua hàng', N'Tôi muốn mua iphone', NULL, 1, '2026-07-24T10:44:47.686', '2026-07-24T10:44:56.226', NULL, 0);
SET IDENTITY_INSERT [dbo].[ContactMessages] OFF;
GO

-- ----------------------------------------------------------------------
-- [ProductReviews] - 2 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[ProductReviews] ON;
INSERT INTO [dbo].[ProductReviews] ([Id], [ProductId], [AuthorName], [Rating], [Content], [Status], [AdminReply], [RepliedAt], [IsDeleted], [DeletedAt], [CreatedAt], [UpdatedAt], [Phone], [IsVerifiedPurchase], [UserId]) VALUES
(2, 8, N'Dat', 5, N'Sản phẩm quá tuyệt vời', 1, N'Cảm ơn bạn đã ủng hộ shop', '2026-07-25T08:10:24.424', 0, NULL, '2026-07-25T08:07:25.096', '2026-07-25T08:10:24.424', NULL, 0, NULL),
(5, 8, N'Dat', 4, N'Sản phẩm tốt nhe', 1, N'Cảm ơn bạn đã ủng hộ shop ạ!!', '2026-07-25T08:27:01.631', 0, NULL, '2026-07-25T08:26:40.318', '2026-07-25T08:27:01.632', N'0912345678', 0, NULL);
SET IDENTITY_INSERT [dbo].[ProductReviews] OFF;
GO

-- ----------------------------------------------------------------------
-- [Orders] - 15 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[Orders] ON;
INSERT INTO [dbo].[Orders] ([Id], [Code], [CustomerName], [Phone], [Email], [Address], [Note], [PaymentMethod], [Status], [TotalAmount], [CreatedAt], [UpdatedAt], [UserId]) VALUES
(1, N'RZ2607221452', N'Nguyen Van Test', N'0900000123', N'test@example.com', N'123 Duong Demo, Quan 1, TP.HCM', N'Goi truoc khi giao', 1, 3, 62180000, '2026-07-22T11:24:26.100', '2026-07-22T22:07:24.918', NULL),
(2, N'RZ2607229666', N'Nguyen Van Test', N'0900000123', N'test@example.com', N'123 Duong Demo, Quan 1, TP.HCM', N'Goi truoc khi giao', 1, 3, 62180000, '2026-07-22T11:27:06.657', '2026-07-24T10:32:17.160', NULL),
(3, N'RZ2607227906', N'Test Buoc', N'0900000999', NULL, N'1 Demo', NULL, 0, 3, 30490000, '2026-07-22T11:40:16.643', '2026-07-22T23:41:18.556', NULL),
(5, N'RZ2607224536', N'Kiem Thu GD6', N'0912345678', N'gd6@example.com', N'123 Duong Test, Go Vap, TP.HCM', NULL, 0, 3, 6580000, '2026-07-22T22:11:59.242', '2026-07-24T10:32:09.897', NULL),
(6, N'RZ2607223653', N'Kiem Thu Cuoi GD6', N'0900111222', NULL, N'1 Duong Test', NULL, 0, 4, 3290000, '2026-07-22T22:57:59.938', '2026-07-24T10:23:50.243', NULL),
(8, N'RZ2607233632', N'E2E Khach Hang', N'0987654321', NULL, N'99 Duong E2E, Q7, HCM', NULL, 1, 3, 31690000, '2026-07-23T14:00:49.476', '2026-07-23T14:01:09.589', NULL),
(9, N'RZ2607242397', N'Dat', N'0912345678', N'khachhang@demo.local', N'32/14 Tô Vĩnh Diện', NULL, 0, 3, 25990000, '2026-07-24T14:30:09.116', '2026-07-24T14:30:27.132', NULL),
(10, N'RZ2607242346', N'Dat', N'0912345678', N'khachhang@demo.local', N'32/14 Tô Vĩnh Diện', NULL, 0, 4, 14490000, '2026-07-24T14:30:53.211', '2026-07-24T16:01:43.462', NULL),
(11, N'RZ2607243266', N'Dat', N'0912345678', N'khachhang@demo.local', N'123', NULL, 0, 4, 5190000, '2026-07-24T14:46:58.897', '2026-07-24T16:01:44.927', NULL),
(15, N'RZ2607246128', N'Dat', N'0912345678', N'khachhang@demo.local', N'2222', NULL, 0, 4, 14490000, '2026-07-24T15:24:08.670', '2026-07-24T16:01:45.851', NULL),
(16, N'RZ2607241374', N'Dat', N'0912345678', N'khachhang@demo.local', N'123123', NULL, 0, 4, 12490000, '2026-07-24T15:24:45.020', '2026-07-24T16:01:46.723', NULL),
(17, N'RZ2607249828', N'Dat', N'0912345678', N'khachhang@demo.local', N'123', NULL, 0, 4, 14490000, '2026-07-24T15:25:05.560', '2026-07-24T16:01:48.773', NULL),
(18, N'RZ2607241801', N'Dat', N'0912345678', N'khachhang@demo.local', N'3', NULL, 0, 3, 900000, '2026-07-24T16:05:11.650', '2026-07-24T16:17:59.340', NULL),
(19, N'RZ2607245247', N'Dat', N'0912345678', N'khachhang@demo.local', N'3', NULL, 0, 3, 14490000, '2026-07-24T16:05:30.680', '2026-07-24T16:17:57.036', NULL),
(20, N'RZ2607252837', N'Dat', N'0912345678', N'khachhang@demo.local', N'123', NULL, 1, 1, 12490000, '2026-07-25T11:47:20.390', '2026-07-27T16:02:52.100', NULL);
SET IDENTITY_INSERT [dbo].[Orders] OFF;
GO

-- ----------------------------------------------------------------------
-- [OrderItems] - 17 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[OrderItems] ON;
INSERT INTO [dbo].[OrderItems] ([Id], [OrderId], [ProductId], [ProductName], [VariantDescription], [UnitPrice], [Quantity]) VALUES
(1, 1, 8, N'iPhone 16 Pro Max 256GB', N'512GB · Titan Trắng · 98% · LL/A (Mỹ)', 31690000, 1),
(2, 1, 8, N'iPhone 16 Pro Max 256GB', N'256GB · Titan Đen · 99% · Chính hãng VN/A', 30490000, 1),
(3, 2, 8, N'iPhone 16 Pro Max 256GB', N'512GB · Titan Trắng · 98% · LL/A (Mỹ)', 31690000, 1),
(4, 2, 8, N'iPhone 16 Pro Max 256GB', N'256GB · Titan Đen · 99% · Chính hãng VN/A', 30490000, 1),
(5, 3, 8, N'iPhone 16 Pro Max 256GB', N'256GB · Titan Đen · 99% · Chính hãng VN/A', 30490000, 1),
(7, 5, 5, N'AirPods 4', NULL, 3290000, 2),
(8, 6, 5, N'AirPods 4', NULL, 3290000, 1),
(10, 8, 8, N'iPhone 16 Pro Max 256GB', N'512GB · Titan Sa Mạc · 98% · LL/A (Mỹ)', 31690000, 1),
(11, 9, 11, N'iPhone 16 Pro 128GB', NULL, 25990000, 1),
(12, 10, 21, N'iPad Air M2 11 inch WiFi 128GB', NULL, 14490000, 1),
(13, 11, 4, N'AirPods Pro 2 (USB-C)', NULL, 5190000, 1),
(17, 15, 21, N'iPad Air M2 11 inch WiFi 128GB', NULL, 14490000, 1),
(18, 16, 22, N'iPad Mini 7 WiFi 128GB', NULL, 12490000, 1),
(19, 17, 21, N'iPad Air M2 11 inch WiFi 128GB', NULL, 14490000, 1),
(20, 18, 2, N'Apple Watch Series 10 46mm GPS', NULL, 900000, 1),
(21, 19, 21, N'iPad Air M2 11 inch WiFi 128GB', NULL, 14490000, 1),
(22, 20, 22, N'iPad Mini 7 WiFi 128GB', NULL, 12490000, 1);
SET IDENTITY_INSERT [dbo].[OrderItems] OFF;
GO

-- ----------------------------------------------------------------------
-- [OrderStatusHistories] - 35 dong
-- ----------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[OrderStatusHistories] ON;
INSERT INTO [dbo].[OrderStatusHistories] ([Id], [OrderId], [FromStatus], [ToStatus], [Note], [ChangedBy], [ChangedAt]) VALUES
(3, 8, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-23T14:00:49.432'),
(4, 8, 0, 1, N'Da goi xac nhan, khach dong y', N'admin', '2026-07-23T14:01:09.367'),
(5, 8, 1, 2, N'Ban giao cho GHTK, ma van don GHTK123', N'admin', '2026-07-23T14:01:09.475'),
(6, 8, 2, 3, N'Giao thanh cong, khach da nhan', N'admin', '2026-07-23T14:01:09.589'),
(7, 6, 0, 4, NULL, N'admin', '2026-07-24T10:23:50.222'),
(8, 5, 0, 1, NULL, N'admin', '2026-07-24T10:32:02.919'),
(9, 5, 1, 2, NULL, N'admin', '2026-07-24T10:32:06.896'),
(10, 5, 2, 3, NULL, N'admin', '2026-07-24T10:32:09.897'),
(11, 2, 0, 1, NULL, N'admin', '2026-07-24T10:32:13.497'),
(12, 2, 1, 2, NULL, N'admin', '2026-07-24T10:32:15.007'),
(13, 2, 2, 3, NULL, N'admin', '2026-07-24T10:32:17.160'),
(14, 9, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T14:30:09.036'),
(15, 9, 0, 1, NULL, N'admin', '2026-07-24T14:30:21.039'),
(16, 9, 1, 2, NULL, N'admin', '2026-07-24T14:30:26.112'),
(17, 9, 2, 3, NULL, N'admin', '2026-07-24T14:30:27.131'),
(18, 10, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T14:30:53.210'),
(19, 11, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T14:46:58.896'),
(23, 15, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T15:24:08.601'),
(24, 16, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T15:24:45.020'),
(25, 17, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T15:25:05.560'),
(26, 10, 0, 4, NULL, N'admin', '2026-07-24T16:01:43.433'),
(27, 11, 0, 4, NULL, N'admin', '2026-07-24T16:01:44.926'),
(28, 15, 0, 4, NULL, N'admin', '2026-07-24T16:01:45.851'),
(29, 16, 0, 4, NULL, N'admin', '2026-07-24T16:01:46.723'),
(30, 17, 0, 4, NULL, N'admin', '2026-07-24T16:01:48.773'),
(31, 18, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T16:05:11.633'),
(32, 19, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-24T16:05:30.676'),
(33, 18, 0, 1, NULL, N'admin', '2026-07-24T16:17:53.913'),
(34, 19, 0, 1, NULL, N'admin', '2026-07-24T16:17:54.836'),
(35, 19, 1, 2, NULL, N'admin', '2026-07-24T16:17:55.860'),
(36, 19, 2, 3, NULL, N'admin', '2026-07-24T16:17:57.036'),
(37, 18, 1, 2, NULL, N'admin', '2026-07-24T16:17:57.763'),
(38, 18, 2, 3, NULL, N'admin', '2026-07-24T16:17:59.340'),
(39, 20, NULL, 0, NULL, N'Khách đặt hàng', '2026-07-25T11:47:20.346'),
(40, 20, 0, 1, NULL, N'admin', '2026-07-27T16:02:52.073');
SET IDENTITY_INSERT [dbo].[OrderStatusHistories] OFF;
GO

-- [CustomerAddresses]: bang rong, khong co du lieu

-- [WishlistItems]: bang rong, khong co du lieu

-- [UserCarts]: bang rong, khong co du lieu

PRINT N'BUOC 3 hoan tat: da nap xong du lieu.';
GO


-- ============================================================================
--  BƯỚC 3B — GẮN DỮ LIỆU VÀO TÀI KHOẢN KHÁCH DEMO
-- ============================================================================
--  15 đơn hàng và 2 đánh giá trong dữ liệu này được đặt từ trước khi có tính năng
--  tài khoản khách hàng, nên cột UserId đang để trống. Khối lệnh dưới đây gắn chúng
--  vào tài khoản demo 0912345678 để khu "Tài khoản của tôi" (lịch sử đơn, tự hủy đơn,
--  mua lại, huy hiệu "Đã mua hàng") có dữ liệu mà xem ngay.
--
--  Không muốn thì xóa nguyên khối này — website vẫn chạy đúng, chỉ khác là tài khoản
--  demo sẽ trống trơn còn 15 đơn kia chỉ xem được ở trang quản trị.
-- ============================================================================
DECLARE @DemoUserId NVARCHAR(450) =
    (SELECT [Id] FROM [dbo].[AspNetUsers] WHERE [UserName] = N'0912345678');

-- Gắn toàn bộ đơn hàng chưa có chủ cho tài khoản demo
UPDATE [dbo].[Orders]
   SET [UserId] = @DemoUserId
 WHERE [UserId] IS NULL;

-- Gắn đánh giá + bật huy hiệu "Đã mua hàng"
-- (sản phẩm được đánh giá nằm trong một đơn đã ở trạng thái Hoàn tất nên huy hiệu này đúng sự thật)
UPDATE [dbo].[ProductReviews]
   SET [UserId] = @DemoUserId,
       [IsVerifiedPurchase] = 1
 WHERE [UserId] IS NULL;

-- Sổ địa chỉ: một địa chỉ mặc định
IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerAddresses])
INSERT INTO [dbo].[CustomerAddresses]
    ([UserId], [Label], [ReceiverName], [Phone], [AddressLine], [IsDefault], [CreatedAt])
VALUES
    (@DemoUserId, N'Nhà riêng', N'Khách Hàng Demo', N'0912345678',
     N'123 Đường Demo, Phường 5, Quận Gò Vấp, TP. Hồ Chí Minh', 1, GETDATE());

-- Danh sách yêu thích: hai sản phẩm
IF NOT EXISTS (SELECT 1 FROM [dbo].[WishlistItems])
INSERT INTO [dbo].[WishlistItems] ([UserId], [ProductId], [CreatedAt])
SELECT TOP 2 @DemoUserId, [Id], GETDATE()
  FROM [dbo].[Products]
 WHERE [IsActive] = 1 AND [IsContactPrice] = 0
 ORDER BY [Id];
GO

PRINT N'BUOC 3B hoan tat: da gan du lieu vao tai khoan demo.';
GO


-- ============================================================================
--  BƯỚC 4 — KIỂM TRA KẾT QUẢ (số dòng mong đợi ghi ở cột bên phải)
-- ============================================================================
SELECT 'Categories'           AS [Bang], COUNT(*) AS [SoDong], 18 AS [MongDoi] FROM [dbo].[Categories]
UNION ALL SELECT 'Products',            COUNT(*), 23 FROM [dbo].[Products]
UNION ALL SELECT 'ProductImages',       COUNT(*), 84 FROM [dbo].[ProductImages]
UNION ALL SELECT 'ProductOptionValues', COUNT(*), 16 FROM [dbo].[ProductOptionValues]
UNION ALL SELECT 'NewsArticles',        COUNT(*),  8 FROM [dbo].[NewsArticles]
UNION ALL SELECT 'StaticPages',         COUNT(*), 14 FROM [dbo].[StaticPages]
UNION ALL SELECT 'MenuItems',           COUNT(*), 24 FROM [dbo].[MenuItems]
UNION ALL SELECT 'SiteSettings',        COUNT(*), 26 FROM [dbo].[SiteSettings]
UNION ALL SELECT 'Orders',              COUNT(*), 15 FROM [dbo].[Orders]
UNION ALL SELECT 'OrderItems',          COUNT(*), 17 FROM [dbo].[OrderItems]
UNION ALL SELECT 'OrderStatusHistories',COUNT(*), 35 FROM [dbo].[OrderStatusHistories]
UNION ALL SELECT 'ProductReviews',      COUNT(*),  2 FROM [dbo].[ProductReviews]
UNION ALL SELECT 'AspNetUsers',         COUNT(*),  2 FROM [dbo].[AspNetUsers]
UNION ALL SELECT 'Migrations',          COUNT(*),  7 FROM [dbo].[__EFMigrationsHistory];
GO

PRINT N'';
PRINT N'==============================================================';
PRINT N'  HOAN TAT! Chay website bang lenh:';
PRINT N'     dotnet run --project src/RenewZone.Web';
PRINT N'';
PRINT N'  Quan tri : http://localhost:5216/admin   admin / Admin@123';
PRINT N'  Khach    : http://localhost:5216         0912345678 / Demo@123';
PRINT N'==============================================================';
GO
