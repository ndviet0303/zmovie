## Why

ZMovie đã hoàn thiện nền tảng cốt lõi (catalog, HLS streaming cơ bản, xác thực Google, quản lý thư viện cá nhân và AI assistant sơ khởi). Tuy nhiên, để đảm bảo độ tin cậy tuyệt đối khi trình diễn (demo) và nâng tầm sản phẩm thành một nền tảng streaming phim hoàn chỉnh với tính năng cộng đồng và mô hình kinh doanh thực tế, sản phẩm cần giải quyết các bài toán sau:
1. **Độ ổn định phát video (Playback Reliability)**: Phụ thuộc 100% vào nguồn stream bên ngoài dễ gặp sự cố đứt link, giật lag hoặc bị chặn khi demo. Cần giải pháp tự lưu trữ (self-hosted) trên **Cloudflare R2** cho **3 bộ phim mẫu (Demo Benchmark Movies)** với chuẩn HLS siêu tốc, kết hợp nguồn phim mở rộng được crawl tự động từ **NguonC API** (`https://phim.nguonc.com/api-document`).
2. **Trải nghiệm xem & khám phá cốt lõi**: Player hỗ trợ phím tắt hotkeys, phụ đề linh hoạt, đạn mạc (Danmaku), metadata làm giàu (diễn viên, đạo diễn, quốc gia, trailer preview), bộ lọc phim nâng cao, dọn dẹp lịch sử xem và gửi báo lỗi tập phim.
3. **Tương tác cộng đồng & Nâng tầm AI**: AI chat chuyển đổi sang widget dạng nổi (Floating Widget) với streaming Server-Sent Events (SSE); phòng xem chung (Watch Party) thời gian thực qua SignalR và chuông thông báo tập mới.
4. **Vận hành tự động & Hội viên VIP**: Bộ lập lịch tự động crawler NguonC, bảng điều khiển phân tích số liệu người xem (Analytics Dashboard) và gói VIP kích hoạt tự động qua VietQR.

## What Changes

### Giai đoạn 1: Trải nghiệm Xem & Khám phá Cốt lõi (Phase 1)
- **Cloudflare R2 Streaming cho 3 phim Demo**: Tích hợp Cloudflare R2 (S3-compatible object storage) lưu trữ video HLS cho 3 phim demo chất lượng cao, bảo đảm tải tức thì, không giật lag và 100% khả dụng khi demo.
- **Player Dual Engine (HLS.js + Embed Fallback)**: Player hỗ trợ phát m3u8 native (cho R2 và HLS direct links) kèm fallback sang responsive iframe embed (cho các link embed của NguonC).
- **Player Hotkeys & Subtitles**: Phím tắt chuẩn (`Space`, `F`, `M`, `←`/`→` tua 10s, `↑`/`↓` âm lượng, `C` toggle phụ đề) có focus guard; hỗ trợ WebVTT subtitle tracks.
- **Làm giàu Metadata & NguonC Ingestion**: Bổ sung diễn viên (`Actors`), đạo diễn (`Directors`), quốc gia (`Country`), trailer URL vào Aggregate `Title` và hiển thị Modal Trailer; bộ chuyển đổi dữ liệu từ **NguonC API** (`/api/films/phim-moi-cap-nhat`, `/api/film/{slug}`).
- **Bộ lọc nâng cao trang Browse**: Lọc đa tiêu chí đồng thời (Thể loại multi-select, Quốc gia, Năm phát hành, Loại phim lẻ/bộ, Sắp xếp).
- **Quản lý lịch sử xem & Báo lỗi phim**: Xoá từng mục hoặc xoá toàn bộ `WatchProgress`; modal gửi báo cáo lỗi tập phim (`EpisodeReport`) và trang duyệt báo lỗi trong Admin.

### Giai đoạn 2: Tương tác Cộng đồng & Nâng tầm AI (Phase 2)
- **Floating AI Assistant Widget & SSE Streaming**: Endpoint streaming SSE trả về token theo thời gian thực; widget AI floating bubble góc phải dưới màn hình.
- **Watch Party Realtime qua SignalR**: Phòng xem chung đồng bộ play/pause/seek kèm thuật toán cân chỉnh độ lệch (drift correction) và chat nhóm thời gian thực.
- **Bình luận Danmaku (Đạn mạc)**: Hiệu ứng chữ chạy ngang màn hình video dựa theo timestamp trên Canvas overlay.
- **Hệ thống Chuông thông báo (Notification Bell)**: Tự động push thông báo và cập nhật badge khi phim đã lưu có tập mới.

### Giai đoạn 3: Vận hành Tự động & Hội viên VIP (Phase 3)
- **Admin Auto-crawler Scheduler cho NguonC**: Background service tự động crawl phim & tập mới từ NguonC theo chu kỳ; giao diện admin quản lý cấu hình và log crawl.
- **Analytics Dashboard chuyên sâu**: Biểu đồ trực quan hoá số liệu lượt xem, retention drop-off giữa các tập, top phim và khung giờ cao điểm.
- **Gói VIP & Thanh toán tự động VietQR**: Gói hội viên VIP (xem phim R2 không quảng cáo, mở full chất lượng, tạo phòng Watch Party); sinh mã VietQR động theo đơn hàng và webhook cập nhật VIP tức thì.

## Capabilities

### New Capabilities
- `playback/player-enhancements`: Phát trực tuyến HLS từ Cloudflare R2 cho 3 phim demo, chế độ dual engine player, phụ đề, hotkeys và bình luận đạn mạc (Danmaku).
- `catalog/enriched-discovery`: Bóc tách metadata từ NguonC API (diễn viên, đạo diễn, quốc gia, trailer) và bộ lọc đa chiều trên trang Browse.
- `engagement/history-and-reports`: Quản lý dọn dẹp lịch sử xem và hệ thống tiếp nhận, xử lý báo lỗi phim.
- `community/watch-party`: Hệ sinh thái phòng xem chung thời gian thực với đồng bộ playback và chat qua SignalR.
- `assistant/streaming-widget`: Widget trợ lý phim AI dạng nổi với kết nối streaming SSE hiển thị phản hồi theo thời gian thực.
- `notifications/episode-alerts`: Hạ tầng lưu trữ, xử lý domain events và phân phối thông báo tập mới cho người dùng.
- `operations/admin-automation-and-vip`: Bộ lập lịch tự động crawler NguonC, script nạp 3 phim demo lên R2, dashboard phân tích dữ liệu và cổng VIP qua VietQR.

### Modified Capabilities
*(Không có - dự án hiện tại chưa có delta specs tồn tại trong `openspec/specs/`)*

## Impact

- **Storage & Cloud Services**:
  - Tích hợp Cloudflare R2 Bucket cho 3 phim demo (`ZMovie.Infrastructure/Storage`).
- **Data Ingestion**:
  - Module mới `NguonCCatalogImporter` kết nối tới `https://phim.nguonc.com/api`.
- **Database & Persistence**:
  - Migration thêm cột `actors`, `directors`, `country`, `trailer_url` cho bảng `titles`.
  - Tạo mới các bảng: `movie_reports`, `notifications`, `danmaku_comments`, `watch_party_rooms`, `vip_subscriptions`, `payment_orders`.
- **Backend Architecture**:
  - Thêm `Microsoft.AspNetCore.SignalR` cho Watch Party & Danmaku.
  - Endpoint SSE `/v1/assistant/chat/stream`.
  - Background Service (`IHostedService`) cho NguonC Crawler Scheduler.
  - Webhook endpoint `/v1/billing/webhook` cho VietQR (SePay/Casso).
- **Frontend (Nuxt 4 / Vue 3)**:
  - Cập nhật player (`watch/[slug].vue`) hỗ trợ phát HLS R2, embed fallback, hotkeys, subtitles, Danmaku canvas.
  - Trang Browse lọc đa tiêu chí, Global Floating AI Widget, Watch Party `/party/[roomId]`, Notification Bell, Admin Crawler Console và Modal VietQR.
