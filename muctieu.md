## 1. Tổng quan

**Nguồn:** repo [hmdung26/javaBTL_QLBanHang](https://github.com/hmdung26/javaBTL_QLBanHang) — đồ án "Quản lý bán hàng" hiện viết bằng Spring Boot 3 (Java) + PostgreSQL cho backend, React + TypeScript + Vite + Tailwind cho frontend, xác thực bằng JWT, có tích hợp AI (Gemini) cho chatbot tư vấn và báo cáo dashboard.

**Mục tiêu spec:** mô tả đầy đủ dữ liệu, nghiệp vụ, phân quyền của hệ thống gốc để làm cơ sở viết lại bằng ASP.NET Core MVC (Razor Views, mô hình MVC truyền thống thay vì Web API + SPA như bản gốc).

**Phạm vi:** sản phẩm, danh mục, thương hiệu, đơn hàng, thanh toán, kho, bảo hành, khuyến mãi, banner, đánh giá sản phẩm, thông báo, quản trị người dùng, dashboard thống kê, chatbot AI.

**Vai trò người dùng:** `USER` (khách hàng), `STAFF` (nhân viên), `ADMIN` (quản trị).

## 2. Kiến trúc & công nghệ đề xuất

| Thành phần | Bản gốc (Java) | Bản .NET MVC đề xuất |
| --- | --- | --- |
| Backend framework | Spring Boot 3 | ASP.NET Core 8 MVC |
| Ngôn ngữ | Java 17+ | C# 12 |
| ORM | Spring Data JPA / Hibernate | Entity Framework Core |
| CSDL | PostgreSQL | SQL Server (dùng `Microsoft.EntityFrameworkCore.SqlServer`) |
| Xác thực | JWT tự cấp qua `JwtAuthenticationFilter` | ASP.NET Core Identity + Cookie Authentication (đúng mô hình MVC), hoặc JWT nếu vẫn muốn tách API |
| Giao diện | React SPA riêng (Vite) gọi REST API | Razor Views (`.cshtml`) render phía server, dùng Bootstrap/Tailwind |
| Validation | Bean Validation (`@Valid`, DTO) | Data Annotations / FluentValidation trên ViewModel |
| Mapping entity↔DTO | thủ công qua constructor DTO | AutoMapper |
| Upload file | `MultipartFile`, lưu thư mục `uploads/`, serve qua `/uploads/**` | `IFormFile`, lưu `wwwroot/uploads/`, serve qua static files middleware |
| AI chatbot | Gọi Gemini API (`GeminiAiServiceImpl`) qua `HttpClient` | Giữ nguyên: gọi Gemini REST API qua `HttpClientFactory`, cấu hình key trong `appsettings.json` |
| Cấu hình | `application.yml` + biến môi trường | `appsettings.json` + `appsettings.{Environment}.json` + biến môi trường |
| Kiến trúc tầng | Controller → Service (interface + Impl) → Repository (Spring Data) → Entity | Controller → Service (interface + Impl, DI) → Repository hoặc `DbContext` trực tiếp → Entity |
| Xử lý lỗi tập trung | `GlobalExceptionHandler` (`@ControllerAdvice`) | Middleware `IExceptionHandler` / `app.UseExceptionHandler`, kèm trang lỗi Razor |

Giữ nguyên toàn bộ nghiệp vụ, chuyển CSDL từ PostgreSQL sang SQL Server và thay lớp trình bày từ SPA React sang Razor Views để đúng mô hình MVC được yêu cầu.

## 3. Mô hình dữ liệu (17 entity)

Mỗi entity dưới đây ánh xạ sang một class C# trong `Models/`, `DbSet<T>` trong `AppDbContext`, khóa chính `Id` (int, identity).

**User** — `Username` (unique), `Password` (hash BCrypt/PBKDF2), `FullName`, `Phone`, `Address`, `Role` (enum: `User`, `Admin`, `Staff`).

**Category** — `Name` (unique), `Description`, tự tham chiếu `ParentId`/`Parent` + `Children` (danh mục cha-con), 1-n với `Product`.

**Brand** — `Name` (unique), `LogoUrl`, `Description`, 1-n với `Product`.

**Product** — `Name`, `Description`, `Specifications`, `Price` (decimal), `StockQuantity`, `ImageUrl`, `WarrantyPeriod`, n-1 `Brand`, n-1 `Category`, 1-n `ProductImage`, 1-n `WarehouseItem`.

**ProductImage** — `ImageUrl`, `SortOrder`, n-1 `Product` (cascade delete).

**ProductReview** — `Rating` (int), `Comment`, n-1 `Product`, n-1 `User`.

**WarehouseItem** (quản lý từng đơn vị hàng theo serial) — `Barcode` (unique), `SerialNumber` (unique), `ShelfLocation`, `Status` (enum: `Available`, `Reserved`, `Sold`, `Damaged`, `Warranty`), n-1 `Product`, n-1 `Order` (`ReservedOrder`, nullable), `LastUpdated`.

**Order** (tên bảng `orders`, entity tên `SalesOrder` bên Java để tránh trùng từ khóa) — `CustomerName`, `CustomerPhone`, `CustomerAddress`, `TotalAmount`, `SubTotal`, `DiscountAmount`, `Status` (enum: `Pending`, `Processing`, `Shipped`, `Delivered`, `Cancelled`), n-1 `User` (nullable — khách vãng lai), n-1 `Promotion` (nullable), 1-1 `Payment`, 1-1 `Invoice`, 1-n `OrderItem` (cascade delete).

**OrderItem** — `Quantity`, `Price`, `SubTotal`, n-1 `Order`, n-1 `Product`.

**Payment** — 1-1 `Order` (unique), `Method` (enum: `Cod`, `BankTransfer`, `EWallet`), `Status` (enum: `Pending`, `Paid`, `Failed`, `Refunded`), `Amount`, `TransactionCode` (unique, nullable), `PaidAt`.

**Invoice** — `InvoiceNumber` (unique, sinh tự động dạng `INV-XXXXXXXX`), 1-1 `Order` (unique), `IssuedAt`.

**Promotion** (mã giảm giá) — `Code` (unique), `Name`, `DiscountType` (enum: `Percent`, `Fixed`), `DiscountValue`, `MinOrderValue`, `StartAt`, `EndAt`, `UsageLimit`, `UsedCount`, `Active`.

**Warranty** — `SerialNumber` (unique), n-1 `WarehouseItem`, n-1 `OrderItem`, n-1 `User`, `StartDate`, `EndDate`, `Status` (enum: `Active`, `Requested`, `Inspecting`, `Repairing`, `Replaced`, `Completed`, `Rejected`, `Expired`), `Note`, 1-n `WarrantyHistory` (cascade delete).

**WarrantyHistory** — n-1 `Warranty`, `Status` (cùng enum `WarrantyStatus`), `Note`, `CreatedAt`.

**Notification** — n-1 `User`, `Title`, `Message`, `Read` (bool), `CreatedAt`.

**Banner** — `Title`, `Subtitle`, `ImageUrl`, `LinkUrl`, `Active` (bool), `SortOrder`, `CreatedAt`.

Mọi entity có `CreatedAt` được gán tự động khi tạo (tương đương `@PrePersist` — dùng `SaveChanges` override trong `AppDbContext` hoặc interceptor EF Core). Áp dụng Fluent API hoặc Data Annotations cho các ràng buộc `unique`, `required`, `maxlength` tương ứng với các annotation `@Column(nullable=false, unique=true)` trong bản gốc.

## 4. Xác thực & phân quyền

Bản gốc dùng JWT stateless. Với MVC dùng Razor Views, khuyến nghị chuyển sang **Cookie Authentication** của ASP.NET Core Identity (đăng nhập giữ session qua cookie, phù hợp trình duyệt render server-side) — mật khẩu vẫn hash bằng BCrypt/Identity `PasswordHasher`. Ba vai trò: `User`, `Staff`, `Admin`, gán qua `[Authorize(Roles = "...")]` trên Controller/Action.

**Ma trận quyền theo chức năng** (suy từ `SecurityConfig` gốc):

| Chức năng | Ai được truy cập |
| --- | --- |
| Đăng ký / đăng nhập | Ai cũng được (`AllowAnonymous`) |
| Xem sản phẩm, danh mục, thương hiệu, banner, đánh giá | Ai cũng được |
| Chat AI (`/ai/chat`) | Ai cũng được |
| Viết đánh giá sản phẩm | Đã đăng nhập |
| Xóa đánh giá | Admin |
| Tạo đơn hàng | Đã đăng nhập |
| Xem đơn hàng của mình (`/orders/my`) | Đã đăng nhập |
| Xem/tra cứu toàn bộ đơn hàng, cập nhật trạng thái đơn | Admin, Staff |
| Quản lý sản phẩm/danh mục/thương hiệu/khuyến mãi (thêm/sửa/xóa) | Admin |
| Quản lý kho (`/warehouse`) | Admin, Staff |
| Quản lý thanh toán (`/payments`) | Admin, Staff |
| Xem/tạo yêu cầu bảo hành | Đã đăng nhập |
| Cập nhật trạng thái bảo hành | Admin, Staff |
| Thông báo cá nhân | Đã đăng nhập |
| Quản lý banner, upload ảnh | Admin |
| Quản lý người dùng (`/admin/users`) | Admin |
| Dashboard, báo cáo doanh thu, báo cáo AI cho admin | Admin (báo cáo AI: chỉ Admin), Staff xem được dashboard chung |

Tài khoản admin mặc định khởi tạo lúc chạy lần đầu: username `admin`, mật khẩu `admin123` (cấu hình lại qua biến môi trường/appsettings khi triển khai thật).

## 5. Chi tiết chức năng theo module

Mỗi module dưới đây là một `Controller` (Razor MVC) với các Action tương ứng route/HTTP verb gốc, cùng View đi kèm.

**5.1 AccountController (Auth)** — đăng ký, đăng nhập (tạo cookie/claims sau khi xác thực), xem/sửa hồ sơ cá nhân (`GET/POST Me`), đăng xuất. View: `Register.cshtml`, `Login.cshtml`, `Profile.cshtml`.

**5.2 ProductController** — danh sách sản phẩm có lọc theo danh mục/thương hiệu/từ khóa/khoảng giá + phân trang, chi tiết sản phẩm (kèm ảnh, đánh giá), danh sách "bán chạy nhất" và "đánh giá cao nhất" cho trang chủ; CRUD sản phẩm (chỉ Admin) gồm cả quản lý nhiều ảnh sản phẩm. View: `Index`, `Details`, `Create`, `Edit` (khu Admin).

**5.3 CategoryController / BrandController** — CRUD, hỗ trợ danh mục cha-con (Category). Dùng cho cả trang khách (dropdown lọc) và trang quản trị.

**5.4 CartController** (giỏ hàng phía client, bản gốc dùng React Context — MVC có thể dùng Session hoặc cookie giỏ hàng) — thêm/sửa số lượng/xóa sản phẩm khỏi giỏ, tính tạm tính trước khi đặt hàng.

**5.5 OrderController** — đặt hàng từ giỏ (tạo `Order` + `OrderItem`, áp mã khuyến mãi nếu có, trừ tồn kho), xem đơn hàng của tôi (khách hàng), danh sách toàn bộ đơn hàng + xem chi tiết + đổi trạng thái (Admin/Staff, quy trình `Pending → Processing → Shipped → Delivered`, hoặc `Cancelled`).

**5.6 PaymentController** — cập nhật trạng thái thanh toán của một đơn (`Pending/Paid/Failed/Refunded`), gắn mã giao dịch khi thanh toán chuyển khoản/ví điện tử.

**5.7 WarehouseController** — quản lý từng đơn vị hàng tồn theo serial/barcode: thêm mới, đổi vị trí kệ, đổi trạng thái (`Available/Reserved/Sold/Damaged/Warranty`), gắn với đơn hàng khi được giữ chỗ.

**5.8 WarrantyController** — khách hàng tra cứu bảo hành theo số serial, gửi yêu cầu bảo hành mới; Admin/Staff cập nhật trạng thái xử lý (ghi lại lịch sử trạng thái vào `WarrantyHistory` mỗi lần đổi).

**5.9 PromotionController** — CRUD mã giảm giá (phần trăm hoặc số tiền cố định), theo dõi số lần đã dùng/giới hạn dùng, thời gian hiệu lực; validate khi áp mã vào đơn hàng (còn hiệu lực, chưa hết lượt, đủ giá trị đơn tối thiểu).

**5.10 ProductReviewController** — khách hàng để lại đánh giá (rating + bình luận) cho sản phẩm đã mua; Admin xóa đánh giá vi phạm.

**5.11 BannerController** — CRUD banner trang chủ (ảnh, tiêu đề, link, thứ tự hiển thị, bật/tắt).

**5.12 NotificationController** — danh sách thông báo của người dùng hiện tại, đánh dấu đã đọc.

**5.13 FileUploadController** — upload ảnh (sản phẩm, banner, avatar), lưu vào `wwwroot/uploads`, trả về URL công khai.

**5.14 AdminUserController** — Admin quản lý danh sách người dùng: tạo, sửa thông tin/vai trò, xóa.

**5.15 AdminController / AdminReportController (Dashboard)** — thống kê tổng quan (doanh thu, số đơn, số sản phẩm, số người dùng...) và báo cáo doanh thu theo tháng (biểu đồ).

**5.16 AiController** — chatbot tư vấn sản phẩm cho khách (gọi Gemini API, công khai không cần đăng nhập) và tính năng "AI tạo báo cáo" tóm tắt số liệu dashboard bằng ngôn ngữ tự nhiên (chỉ Admin).

## 6. Cấu trúc thư mục dự án .NET MVC đề xuất

```
SalesManagement/
├── Controllers/
│   ├── AccountController.cs
│   ├── ProductController.cs
│   ├── CategoryController.cs
│   ├── BrandController.cs
│   ├── CartController.cs
│   ├── OrderController.cs
│   ├── PaymentController.cs
│   ├── WarehouseController.cs
│   ├── WarrantyController.cs
│   ├── PromotionController.cs
│   ├── ProductReviewController.cs
│   ├── BannerController.cs
│   ├── NotificationController.cs
│   ├── FileUploadController.cs
│   ├── AiController.cs
│   └── Admin/
│       ├── DashboardController.cs
│       ├── AdminUserController.cs
│       └── AdminReportController.cs
├── Models/                 # entity, ánh xạ mục 3
├── ViewModels/             # thay cho DTO request/response
├── Data/
│   └── AppDbContext.cs
├── Services/
│   ├── Interfaces/
│   └── Implementations/    # ProductService, OrderService, GeminiAiService, ...
├── Repositories/           # (tùy chọn, nếu không dùng thẳng DbContext trong Service)
├── Views/
│   ├── Shared/_Layout.cshtml, _AdminLayout.cshtml
│   ├── Product/, Category/, Order/, Account/, Admin/...
├── wwwroot/
│   ├── css/, js/, lib/
│   └── uploads/
├── Migrations/             # EF Core migrations
├── appsettings.json
├── Program.cs
└── SalesManagement.csproj
```

## 7. Kế hoạch triển khai theo giai đoạn

1. **Khởi tạo dự án & hạ tầng** — tạo project ASP.NET Core MVC, cấu hình EF Core + SQL Server, `AppDbContext` với toàn bộ 17 entity ở mục 3, chạy migration đầu tiên, seed dữ liệu mẫu (tài khoản admin, vài category/brand).
2. **Xác thực & phân quyền** — tích hợp ASP.NET Core Identity, đăng ký/đăng nhập/đăng xuất, áp `[Authorize(Roles=...)]` theo ma trận mục 4.
3. **Module danh mục sản phẩm (khách hàng)** — Product, Category, Brand, ProductImage, Banner, ProductReview: trang chủ, danh sách + lọc/tìm kiếm, chi tiết sản phẩm, đánh giá.
4. **Giỏ hàng & đặt hàng** — Cart (session), Order, OrderItem, Promotion (áp mã), trừ tồn kho qua WarehouseItem.
5. **Thanh toán & hóa đơn** — Payment, Invoice.
6. **Vận hành nội bộ (Admin/Staff)** — quản lý đơn hàng, kho (WarehouseController), bảo hành (Warranty + WarrantyHistory), người dùng (AdminUserController).
7. **Dashboard & báo cáo** — thống kê tổng quan, doanh thu theo tháng, tích hợp Gemini AI cho chatbot và báo cáo tự động.
8. **Hoàn thiện** — thông báo (Notification), upload ảnh, kiểm thử luồng nghiệp vụ chính (đặt hàng → thanh toán → xuất kho → bảo hành), rà soát phân quyền.

## 8. Lưu ý & rủi ro khi chuyển đổi

- **Đổi kiến trúc trình bày:** bản gốc tách hẳn API (Spring) và SPA (React); chuyển sang MVC nghĩa là gộp lại thành một ứng dụng render server-side — cần thiết kế lại toàn bộ giao diện bằng Razor View thay vì tái dùng component React.
- **Giỏ hàng:** React dùng Context (client-side, mất khi refresh nếu không có localStorage); MVC nên chuyển sang Session hoặc bảng `Cart`/`CartItem` trong CSDL để bền hơn.
- **Đồng bộ tồn kho:** nghiệp vụ có 2 lớp tồn kho — `Product.StockQuantity` (tổng quan) và từng `WarehouseItem` theo serial — cần giữ đúng logic đồng bộ khi đặt/hủy đơn, tránh lệch số liệu.
- **JWT → Cookie:** nếu sau này vẫn cần expose API riêng (app di động, tích hợp bên thứ ba), cân nhắc kiến trúc lai: MVC + cookie cho giao diện web, đồng thời giữ thêm JWT cho API endpoints.
- **Khóa Gemini API:** không hard-code, đưa vào `appsettings.json`/User Secrets/biến môi trường như bản gốc (`GEMINI_API_KEY`).
- **Migration dữ liệu:** nếu cần giữ dữ liệu cũ từ CSDL PostgreSQL của bản Spring Boot gốc (cấu hình `ddl-auto: update`) sang SQL Server bằng công cụ như SQL Server Migration Assistant (SSMA) hoặc script chuyển đổi thủ công, rồi chạy EF Core migration để tạo schema mới — nên giữ tên bảng/cột tương tự để dễ đối chiếu dữ liệu.