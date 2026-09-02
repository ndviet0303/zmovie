## 1. Cloudflare R2 Storage & Demo Benchmark Seeding

- [x] 1.1 Cấu hình Cloudflare R2 S3-compatible client trong `ZMovie.Infrastructure/Storage` (`AWSSDK.S3` hoặc HttpClient S3 API)
- [x] 1.2 Tạo script / command `dotnet run --seed-r2-demo` nạp 3 bộ phim demo chất lượng cao lên catalog trỏ về domain Cloudflare R2 CDN (kèm HLS m3u8 và WebVTT subtitles)
- [x] 1.3 Đánh dấu cờ `IsR2Hosted = true` hoặc gắn badge "Chất lượng cao / R2 Server" trên UI cho 3 phim demo này

## 2. Player Dual-Engine: HLS.js, Embed Fallback, Hotkeys & Subtitles

- [x] 2.1 Cập nhật `watch/[slug].vue` với cơ chế Dual-Engine: tự động phát qua HLS.js khi nguồn là R2 / link direct m3u8, và chuyển sang responsive iframe khi nguồn là link embed (StreamC từ NguonC)
- [x] 2.2 Tạo composable `usePlayerHotkeys` hỗ trợ phím tắt (`Space`, `F`, `M`, `←`/`→`, `↑`/`↓`, `C`) kèm kiểm tra focus guard trên `<input>`, `<textarea>`, `<select>` và dialogs
- [x] 2.3 Mở rộng player để phát hiện subtitle tracks từ HLS manifest (`hls.subtitleTracks`) và hỗ trợ nạp WebVTT track rời từ R2
- [x] 2.4 Thiết kế menu chọn phụ đề (ngôn ngữ / tắt phụ đề) trong giao diện điều khiển player
- [x] 2.5 Bổ sung HUD visual indicator khi tua hoặc chỉnh âm lượng trên video

## 3. NguonC Catalog Ingestion & Metadata Enrichment

- [x] 3.1 Cập nhật Aggregate `Title` trong `ZMovie.Domain/Catalog` bổ sung `Actors`, `Directors`, `Country`, `TrailerUrl`
- [x] 3.2 Tạo EF Core migration cho `CatalogDbContext` cập nhật bảng `titles` với các trường metadata mới
- [x] 3.3 Phát triển `NguonCCatalogImporter` kết nối tới `https://phim.nguonc.com/api/films/phim-moi-cap-nhat` và `/api/film/{slug}` bóc tách đầy đủ casts, director, category và danh sách tập
- [x] 3.4 Tạo command CLI `dotnet run --import-nguonc-catalog` cho phép chạy nhập dữ liệu phim NguonC
- [x] 3.5 Cập nhật API queries và DTOs trả về metadata phong phú cho frontend
- [x] 3.6 Thiết kế UI hiển thị danh sách diễn viên, đạo diễn, quốc gia trên trang chi tiết phim và component `TrailerModal.vue` xem trước video trailer

## 4. Bộ lọc nâng cao trang Browse

- [x] 4.1 Mở rộng backend query `GetCatalogTitlesQuery` hỗ trợ lọc đồng thời: đa thể loại (`genres[]`), quốc gia (`country`), năm (`fromYear`-`toYear`), loại phim (`type`) và sắp xếp (`sort`)
- [x] 4.2 Nâng cấp `useBrowse.ts` để đồng bộ URL query params đa chiều hai chiều (two-way binding)
- [x] 4.3 Thiết kế drawer bộ lọc nâng cao trên `browse.vue` với multi-select tags, country dropdown, year range slider và nút reset bộ lọc

## 5. Quản lý Lịch sử xem & Báo lỗi phim

- [x] 5.1 Bổ sung command `DeleteWatchProgressCommand` và `ClearWatchHistoryCommand` trong `ZMovie.Application/Engagement`
- [x] 5.2 Thêm API endpoints `DELETE /v1/engagement/history/{titleId}` và `DELETE /v1/engagement/history`
- [x] 5.3 Cập nhật UI trang cá nhân / thư viện cho phép xoá từng phim hoặc xoá toàn bộ lịch sử xem
- [x] 5.4 Thiết kế Entity `MovieReport` trong `Engagement` lưu trữ thông tin báo lỗi tập phim (audio, video, sub, server)
- [x] 5.5 Thêm endpoint gửi báo lỗi `POST /v1/reports` và component modal báo lỗi nhanh ngay dưới video player
- [x] 5.6 Xây dựng giao diện danh sách báo lỗi phim trong Admin Console (`/admin/reports`) cho phép duyệt và đánh dấu đã xử lý

## 6. Floating AI Assistant Widget & Streaming SSE

- [x] 6.1 Xây dựng endpoint streaming SSE `GET /v1/assistant/chat/stream` trong `AssistantEndpoints.cs` trả về `IAsyncEnumerable<string>`
- [x] 6.2 Kết nối stream từ `local-ai` / Ollama proxy adapter sang SSE response pipeline
- [x] 6.3 Phát triển component `FloatingAssistantWidget.vue` ghim góc màn hình với hiệu ứng bung mở glassmorphism
- [x] 6.4 Xử lý frontend stream reader với hiệu ứng typing mượt mà và render title card gợi ý phim có thể click xem ngay

## 7. Watch Party Realtime qua SignalR

- [x] 7.1 Cấu hình SignalR trong `backend/src/ZMovie.Api` và triển khai `WatchPartyHub` quản lý phòng xem chung
- [x] 7.2 Hiện thực các Hub methods: `JoinRoom`, `LeaveRoom`, `SyncPlay`, `SyncPause`, `SyncSeek`, `SendPartyChat`
- [x] 7.3 Tạo composable `useWatchParty` phía frontend kết nối SignalR Hub và tích hợp thuật toán cân chỉnh độ lệch playback (drift correction)
- [x] 7.4 Xây dựng trang rạp xem chung `/party/[roomId].vue` kết hợp video player đồng bộ và khung chat nhóm

## 8. Bình luận Danmaku trên Video

- [x] 8.1 Tạo Entity `DanmakuComment` trong `ZMovie.Domain/Engagement` lưu trữ `TitleId`, `EpisodeId`, `TimeSeconds`, `Content`, `Color`
- [x] 8.2 Tạo migration cho bảng `danmaku_comments` và triển khai API lấy danh sách đạn mạc theo tập: `GET /v1/danmaku/{episodeId}` và gửi đạn mạc: `POST /v1/danmaku`
- [x] 8.3 Phát triển component `DanmakuCanvas.vue` phủ trên video player, render text bay ngang màn hình theo `currentTime` bằng `requestAnimationFrame`
- [x] 8.4 Tích hợp thanh nhập đạn mạc và bảng tuỳ chỉnh (bật/tắt, chỉnh opacity, tốc độ bay) trên thanh điều khiển player
- [x] 8.5 Kết nối SignalR để phát sóng đạn mạc mới theo thời gian thực tới những người đang cùng xem

## 9. Hệ thống Thông báo Tập mới (Notification Bell)

- [x] 9.1 Thiết kế Entity `Notification` trong `ZMovie.Domain` và migration bảng `notifications`
- [x] 9.2 Đăng ký Domain Event Handler lắng nghe `EpisodeAddedDomainEvent` để tự động tạo notification cho các user đã lưu phim vào thư viện
- [x] 9.3 Triển khai API lấy thông báo `GET /v1/notifications` và đánh dấu đã đọc `PUT /v1/notifications/{id}/read`
- [x] 9.4 Phát triển dropdown `NotificationBell.vue` trên navbar hiển thị badge đỏ số lượng thông báo chưa đọc và danh sách tập mới ra mắt
- [x] 9.5 Đẩy realtime notification qua SignalR khi user đang hoạt động trên website

## 10. Auto-crawler Scheduler cho NguonC trong Admin

- [x] 10.1 Tạo `CatalogCrawlerBackgroundWorker` kế thừa `BackgroundService` sử dụng `PeriodicTimer` để tự động crawl NguonC theo chu kỳ
- [x] 10.2 Thiết kế bảng `crawler_configs` và API quản lý: cập nhật lịch chạy (cron interval), xem trạng thái và nút "Crawl thủ công ngay"
- [x] 10.3 Xây dựng tab quản lý Crawler trong Admin (`/admin/crawler`) hiển thị nhật ký crawl NguonC, số phim/tập đã nhập và tỉ lệ lỗi

## 11. Dashboard biểu đồ Analytics chuyên sâu

- [x] 11.1 Xây dựng service tổng hợp số liệu phân tích trong `ZMovie.Application/Analytics`: tổng giờ xem, phân bố khung giờ xem cao điểm, top 10 phim
- [x] 11.2 Tạo API endpoint `GET /v1/admin/analytics/overview` phục vụ dữ liệu biểu đồ
- [x] 11.3 Cài đặt thư viện biểu đồ nhẹ trên Nuxt và phát triển giao diện `AdminAnalyticsDashboard.vue` hiển thị biểu đồ đường lượt xem và biểu đồ thanh retention

## 12. Gói VIP & Thanh toán tự động VietQR

- [x] 12.1 Thêm trường `VipExpiresAt` và `SubscriptionTier` vào Aggregate `User` trong `ZMovie.Domain/Identity`
- [x] 12.2 Tạo Entity `PaymentOrder` theo dõi đơn hàng nạp VIP và mã chuyển khoản duy nhất (`ZM_VIP_...`)
- [x] 12.3 Xây dựng API tạo đơn hàng thanh toán: `POST /v1/billing/checkout` sinh mã VietQR Napas 247
- [x] 12.4 Triển khai Webhook `POST /v1/billing/webhook` nhận thông báo thanh toán (SePay/Casso), kiểm tra chữ ký bí mật, xác thực số tiền và tự động gia hạn `VipExpiresAt`
- [x] 12.5 Thiết kế modal nâng cấp VIP `VipCheckoutModal.vue` hiển thị mã VietQR động, đếm ngược thời gian và tự động chuyển trạng thái VIP khi thanh toán thành công

## 13. Kiểm thử Tích hợp & Xác minh Hệ thống

- [x] 13.1 Viết unit tests cho `NguonCCatalogImporter` và R2 seeding logic
- [x] 13.2 Viết frontend component tests cho Dual-Engine player (HLS vs Embed fallback) và Hotkeys
- [x] 13.3 Kiểm thử toàn diện luồng phát 3 phim demo R2 siêu tốc và luồng crawl tự động NguonC
