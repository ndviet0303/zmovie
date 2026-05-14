# ZMovie - Website xem phim trực tuyến

## Thông tin đồ án

**Tên đề tài:** Xây dựng website xem phim trực tuyến ZMovie  
**Loại đồ án:** Đồ án tốt nghiệp / đồ án chuyên ngành Công nghệ thông tin  
**Lĩnh vực:** Web application, video streaming, quản trị nội dung số  
**Định hướng:** Xây dựng hệ thống quản lý, tìm kiếm, phát và kiểm duyệt nội dung phim trên nền tảng web.

## Thành viên thực hiện

| STT | Họ và tên | MSSV | Vai trò |
| --- | --- | --- | --- |
| 1 | Nguyễn Đức Việt | Cập nhật | Backend, database, API, triển khai hệ thống |
| 2 | Cập nhật | Cập nhật | Frontend, giao diện người dùng |
| 3 | Cập nhật | Cập nhật | Phân tích nghiệp vụ, kiểm thử, tài liệu |

**Giảng viên hướng dẫn:** Cập nhật  
**Khoa/Bộ môn:** Cập nhật  
**Trường:** Cập nhật  
**Thời gian thực hiện:** 2026

## Tóm tắt đề tài

ZMovie là hệ thống website xem phim trực tuyến được xây dựng với mục tiêu mô phỏng một nền tảng phân phối nội dung số hiện đại. Hệ thống hỗ trợ người dùng tra cứu phim, xem thông tin chi tiết, phát video, tìm kiếm theo từ khóa và lọc theo danh mục. Bên cạnh giao diện người dùng, đồ án còn xây dựng khu vực quản trị dành cho admin và đối tác nội dung nhằm quản lý phim, nhà cung cấp, bản quyền, hồ sơ pháp lý và quy trình upload nội dung.

Đề tài tập trung vào việc kết hợp giữa trải nghiệm xem phim trực quan ở frontend và kiến trúc backend có phân quyền, quản lý dữ liệu rõ ràng, phù hợp với bài toán quản trị nội dung phim trong thực tế.

## Lý do chọn đề tài

Trong bối cảnh các nền tảng xem phim trực tuyến ngày càng phổ biến, nhu cầu quản lý nội dung số, phân quyền vận hành, kiểm duyệt bản quyền và tối ưu trải nghiệm tìm kiếm trở thành những vấn đề quan trọng. Vì vậy, nhóm lựa chọn đề tài ZMovie để nghiên cứu và xây dựng một hệ thống có tính ứng dụng cao, bao gồm cả phần hiển thị cho người dùng cuối và phần quản trị dành cho nhà vận hành.

Thông qua đồ án, sinh viên có cơ hội vận dụng kiến thức về lập trình web, thiết kế cơ sở dữ liệu, RESTful API, xác thực, phân quyền, quản lý media, kiểm thử và triển khai ứng dụng.

## Mục tiêu

- Xây dựng website xem phim có giao diện hiện đại, dễ sử dụng và tương thích với nhiều kích thước màn hình.
- Thiết kế backend API phục vụ danh mục phim, tìm kiếm, chi tiết phim, phát video và quản trị nội dung.
- Xây dựng cơ sở dữ liệu phục vụ các nghiệp vụ phim lẻ, phim bộ, tập phim, thể loại, quốc gia, ngôn ngữ và nhà cung cấp nội dung.
- Tích hợp phân quyền RBAC cho admin, đối tác nội dung và các vai trò vận hành.
- Mô phỏng quy trình pháp lý gồm giấy phép nội dung, tài liệu bản quyền, kiểm duyệt và phê duyệt phim.
- Hoàn thiện tài liệu cài đặt, vận hành và mô tả chức năng của hệ thống.

## Phạm vi chức năng

### Người dùng

- Xem danh sách phim theo danh mục.
- Xem phim nổi bật ở trang chủ.
- Tìm kiếm phim theo từ khóa.
- Lọc phim theo thể loại, quốc gia, năm phát hành.
- Xem chi tiết phim gồm poster, backdrop, mô tả, thể loại, năm phát hành và điểm đánh giá.
- Phát video từ nguồn MP4/HLS.

### Quản trị viên

- Đăng nhập khu vực quản trị bằng tài khoản demo.
- Quản lý danh sách phim.
- Thêm, sửa, xóa thông tin phim.
- Quản lý trạng thái phim: nháp, đã xuất bản, chờ duyệt.
- Quản lý trạng thái bản quyền trước khi phát hành.
- Quản lý danh mục tra cứu như thể loại, quốc gia, ngôn ngữ.

### Đối tác nội dung và pháp lý

- Quản lý nhà cung cấp nội dung.
- Quản lý giấy phép khai thác phim.
- Quản lý tài liệu pháp lý liên quan đến bản quyền.
- Mô phỏng quy trình upload nội dung.
- Mô phỏng quy trình gửi duyệt và phê duyệt nội dung.
- Lưu vết thao tác phục vụ audit và kiểm soát.

## Công nghệ sử dụng

### Frontend

- Vue 3
- Vite
- Vue Router
- Tailwind CSS
- Lucide Vue Next
- HLS.js

### Backend

- PHP 8.3
- Laravel 13
- Laravel Scout
- Meilisearch
- Composer
- PHPUnit

### Cơ sở dữ liệu và hạ tầng

- SQLite cho môi trường phát triển
- Có thể mở rộng sang MySQL hoặc PostgreSQL
- Docker/Docker Compose cho môi trường chạy backend
- RESTful API

## Kiến trúc hệ thống

```text
zmovie/
├── frontend/              # Giao diện người dùng và trang quản trị
│   ├── src/
│   ├── public/
│   └── package.json
├── backend/               # Laravel API và xử lý nghiệp vụ
│   ├── app/
│   ├── database/
│   ├── routes/
│   ├── storage/
│   └── composer.json
├── Diagram/               # Sơ đồ thiết kế hệ thống
├── README.md              # Tài liệu tổng quan đồ án
└── .gitignore
```

Frontend giao tiếp với backend thông qua API tại:

```text
http://127.0.0.1:8000/api/v1
```

Backend chịu trách nhiệm xử lý dữ liệu, phân quyền, tìm kiếm, phát video và quản lý workflow nội dung. Frontend đảm nhiệm hiển thị catalog phim, trang chi tiết, trình phát video và dashboard quản trị.

## Thiết kế cơ sở dữ liệu

Hệ thống cơ sở dữ liệu được chia thành các nhóm chính:

- **Tài khoản và phân quyền:** `users`, `roles`, `permissions`, `role_user`, `permission_role`.
- **Danh mục phim:** `movies`, `seasons`, `episodes`, `genres`, `countries`, `languages`, `studios`, `people`, `tags`.
- **Đối tác và bản quyền:** `content_providers`, `content_licenses`, `legal_documents`, `movie_uploads`, `upload_files`.
- **Phát video:** `video_sources`, `media_assets`, `subtitles`.
- **Tương tác người dùng:** `ratings`, `reviews`, `comments`, `favorites`, `watch_histories`, `watchlist_items`.
- **Thanh toán và gói thành viên:** `plans`, `subscriptions`, `payment_transactions`.

Chi tiết thiết kế được mô tả trong [backend/DATABASE_DESIGN.md](backend/DATABASE_DESIGN.md).

## API chính

Một số endpoint tiêu biểu:

```http
GET  /api/health
GET  /api/v1/lookups
GET  /api/v1/movies
GET  /api/v1/movies/{id}
GET  /api/v1/search/movies
GET  /api/v1/video-sources/{id}/stream
POST /api/v1/auth/login
POST /api/v1/movies
PUT  /api/v1/movies/{id}
POST /api/v1/movies/{id}/publish
```

Chi tiết API được mô tả trong [backend/API.md](backend/API.md).

## Cài đặt và chạy dự án

### Yêu cầu môi trường

- PHP >= 8.3
- Composer
- Node.js và npm
- SQLite hoặc hệ quản trị CSDL tương thích Laravel
- Meilisearch nếu muốn sử dụng tìm kiếm nâng cao

### Chạy backend

```bash
cd backend
composer install
cp .env.example .env
php artisan key:generate
php artisan migrate --seed
php artisan serve
```

Backend mặc định chạy tại:

```text
http://127.0.0.1:8000
```

### Chạy frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend mặc định chạy tại:

```text
http://127.0.0.1:5173
```

Nếu cần cấu hình API cho frontend, tạo file `.env` trong thư mục `frontend`:

```env
VITE_API_BASE_URL=http://127.0.0.1:8000/api/v1
```

## Tài khoản demo

Hệ thống có seed tài khoản demo phục vụ kiểm thử:

| Email | Mật khẩu | Vai trò |
| --- | --- | --- |
| admin@zmovie.local | password | Super Admin |
| provider@zmovie.local | password | Provider Owner |

Khi gọi API ở môi trường phát triển, có thể sử dụng header:

```text
X-User-Id: 1
```

## Kiểm thử

Chạy test backend:

```bash
cd backend
php artisan test
```

Build frontend:

```bash
cd frontend
npm run build
```

## Kết quả đạt được

- Xây dựng được giao diện website xem phim với trang chủ, danh sách phim, tìm kiếm, chi tiết và phát video.
- Xây dựng được trang quản trị hỗ trợ đăng nhập demo và quản lý phim.
- Thiết kế backend RESTful API bằng Laravel.
- Thiết kế cơ sở dữ liệu phục vụ nghiệp vụ phim, bản quyền, đối tác, upload và phân quyền.
- Tích hợp phân quyền theo vai trò và quyền hạn.
- Chuẩn bị tài liệu API, thiết kế CSDL và hướng dẫn triển khai.

## Hạn chế

- Chưa tích hợp cổng thanh toán thật.
- Chưa triển khai hệ thống transcode video thực tế.
- Chưa tích hợp xác thực production như OAuth2, JWT hoặc Laravel Sanctum đầy đủ.
- Chưa có hệ thống recommendation cá nhân hóa.
- Dữ liệu demo còn giới hạn.

## Hướng phát triển

- Tích hợp thanh toán online và quản lý gói thành viên.
- Bổ sung hệ thống đề xuất phim theo hành vi người dùng.
- Tích hợp transcode video tự động và lưu trữ cloud.
- Bổ sung CDN cho phát video.
- Hoàn thiện module bình luận, đánh giá và báo cáo nội dung.
- Triển khai CI/CD và môi trường production.
- Nâng cấp xác thực, bảo mật API và giám sát hệ thống.

## Kết luận

ZMovie là đồ án mô phỏng một nền tảng xem phim trực tuyến có đầy đủ các thành phần cơ bản của một hệ thống web hiện đại: giao diện người dùng, trang quản trị, API backend, cơ sở dữ liệu, phân quyền và quy trình quản lý nội dung. Đồ án giúp sinh viên củng cố kiến thức lập trình web full-stack, đồng thời tiếp cận các vấn đề thực tế trong quản trị nội dung số và phát video trực tuyến.

## Tài liệu liên quan

- [backend/API.md](backend/API.md)
- [backend/DATABASE_DESIGN.md](backend/DATABASE_DESIGN.md)
- [backend/DOCKER.md](backend/DOCKER.md)
- [frontend/README.md](frontend/README.md)
- [backend/README.md](backend/README.md)
