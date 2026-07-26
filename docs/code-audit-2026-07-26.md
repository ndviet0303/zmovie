# Báo cáo rà soát mã nguồn ZMovie — 26/07/2026

Phạm vi: toàn bộ repo tại nhánh `dev/ziet` (commit `f154be1`) **bao gồm cả các thay đổi chưa commit và file chưa track** của mảng assistant learning / mood.

Cách làm: 8 luồng rà soát độc lập theo từng chiều (API/bảo mật, Application+Domain, EF/persistence, assistant/AI, crawler, frontend player, frontend app shell, repo/CI/test/docs), mỗi luồng sau đó bị một luồng phản biện đọc lại mã và cố bác bỏ. 80 phát hiện được nêu, 2 bị bác bỏ, phần còn lại được gộp trùng thành **69 mục** dưới đây.

Trạng thái kiểm chứng:

- ✅ **Đã tự kiểm chứng lại** bằng cách đọc trực tiếp file trong phiên này.
- ☑️ Đã được luồng phản biện xác nhận trên mã thật.
- ⚠️ Cơ chế đúng nhưng mức độ ảnh hưởng phụ thuộc môi trường triển khai (đã hạ mức nghiêm trọng tương ứng).

Ghi chú: `eslint .` ở frontend chạy sạch. `dotnet build ZMovie.slnx` được khởi chạy trong phiên này nhưng chưa kết thúc trong thời gian rà soát, nên báo cáo **không** kết luận gì về trạng thái build/test.

---

## Tổng quan mức độ

| Mức | Số lượng | Chủ đề chính |
|---|---|---|
| Cao | 7 | Migration không chạy khi deploy, DoS assistant, deploy tĩnh 404, đóng băng dữ liệu prerender, mất lịch sử xem, mất tập phim, rò rỉ hls.js |
| Trung bình | 41 | Bảo mật/lạm dụng, đua ghi DB, thiếu index, sai lệch schema, chất lượng assistant, crawler, UX player, CI |
| Thấp | 21 | Validator thiếu, escape, a11y, tài liệu, dọn repo |

Ba vấn đề có hậu quả thực tế lớn nhất, theo thứ tự nên xử lý:

1. **[C-01] Migration không bao giờ chạy ở staging/production** — tính năng vừa làm xong sẽ chết âm thầm ngay khi deploy.
2. **[C-03] Bản deploy tĩnh không có SPA fallback** — chỉ 26 phim mở được bằng URL trực tiếp.
3. **[C-05] Trang xem phim hạ cấp phiên đăng nhập xuống `anonymous`** — người dùng đã đăng nhập mất toàn bộ lịch sử xem phía server.

---

## 1. Nghiêm trọng — chặn phát hành

### C-01 · Migration chỉ chạy ở Development, pipeline deploy không có bước migrate ⚠️☑️

[backend/src/ZMovie.Api/Program.cs:117](../backend/src/ZMovie.Api/Program.cs#L117)

`Database.MigrateAsync()` nằm trong `if (app.Environment.IsDevelopment())`. Hai lời gọi `MigrateAsync` còn lại chỉ nằm trong nhánh CLI `--import-ophim-*`. Job deploy (`.backend-deploy`) chỉ `curl -X POST` vào webhook Portainer — không có `dotnet ef database update`, không có migration bundle, không có init container. `compose.yaml` chỉ khởi động Postgres, `AppHost.cs` chỉ có project API.

**Hệ quả:** deploy working tree hiện tại lên staging → bảng `public.assistant_learning_events` không tồn tại → mọi lời gọi `/v1/assistant/*` ném `42P01`, bị `EfAssistantLearningStore` nuốt (xem M-22), API trả 200 và ghi một dòng Warning không ai theo dõi. Tính năng vừa phát triển hoàn toàn không hoạt động mà **không có bất kỳ tín hiệu lỗi nào**.

**Sửa:** thêm bước migrate rõ ràng vào đường deploy — `dotnet ef migrations bundle` chạy như một job trước `backend:deploy:*`, hoặc chuyển `MigrateAsync()` ra khỏi guard `IsDevelopment()` và đặt sau cờ cấu hình `RunMigrationsOnStartup`.

### C-02 · `GetAssistantContextQuery` không có validator → DoS CPU ✅☑️

[backend/src/ZMovie.Application/Assistant/AssistantContracts.cs:25](../backend/src/ZMovie.Application/Assistant/AssistantContracts.cs#L25)

Chỉ `AskCatalogAssistantQuery` bị giới hạn 500 ký tự. Bản sao của nó — `GetAssistantContextQuery`, phục vụ cả `POST` lẫn `GET /v1/assistant/context` — không có `AbstractValidator` nào, nên `Message` chỉ bị chặn bởi giới hạn body 30 MB mặc định của Kestrel.

Chuỗi xử lý: `AssistantMood.SearchTermWeights` tạo một entry dictionary cho mỗi từ phân biệt (không giới hạn) → `CatalogAssistantStore.Score` chạy 3 phép tìm chuỗi con cho **mỗi token × mỗi trong 500 candidate** (synopsis ~4 KB) → `EfAssistantLearningStore.RecordImpressionAsync` băm SHA-256 trên cùng tập token. Không có rate limiter nào được đăng ký trong `Program.cs`.

**Hệ quả:** một người dùng đã đăng nhập POST body ~30 MB toàn từ khác nhau sẽ chiếm trọn một core CPU trong nhiều phút, đồng thời giữ một `DbContext`/connection từ pool.

**Sửa:** thêm `GetAssistantContextValidator` giới hạn `MaximumLength(500)`, và chặn thêm số token ngay trong `AssistantMood.SearchTermWeights`. Kèm theo nên bật `AddRateLimiter` cho nhóm assistant.

### C-03 · Deploy tĩnh Cloudflare Pages không có SPA fallback — chỉ 26 phim truy cập được ✅☑️

[.gitlab/ci/frontend.gitlab-ci.yml:48](../.gitlab/ci/frontend.gitlab-ci.yml#L48)

`nuxt generate` chỉ prerender các route được link từ trang chủ: `frontend/.output/public/movies/` và `.../watch/` mỗi thư mục đúng **26** mục. Thư mục deploy `.output/public` **không có `_redirects` cũng không có `_routes.json`** — Cloudflare Pages phục vụ file tĩnh, nên mọi path không có `index.html` prerender sẽ trả trang 404.

**Hệ quả:** người dùng chia sẻ/bookmark `https://movie.ziet.dev/movies/<slug-bất-kỳ-ngoài-26>` hoặc F5 sau khi điều hướng client-side từ `/browse` (trang này liệt kê toàn bộ catalog từ API) → nhận trang 404. Công cụ tìm kiếm chỉ index được 26 trên hàng nghìn phim.

**Sửa:** thêm `frontend/public/_redirects` với `/* /200.html 200` (Nuxt static preset đã sinh sẵn `200.html`), hoặc chuyển sang preset `cloudflare_pages` với route rules `ssr: false` cho `/movies/**` và `/watch/**`. Kiểm chứng bằng cách request một slug không nằm trong 26 mục.

### C-04 · Prerender đóng băng cả dữ liệu lẫn trạng thái lỗi, client không bao giờ refetch ✅☑️

[frontend/app/pages/index.vue:46](../frontend/app/pages/index.vue#L46)

Mọi bề mặt dữ liệu (hero, trending, "Top hôm nay/tuần/tháng", chi tiết phim, playback) được `useAsyncData` giải quyết **một lần lúc build** rồi đóng băng vào HTML. Không có `watch`, không có `refresh()` sau hydrate.

**Hệ quả:** một lỗi 502 thoáng qua từ `movie-api.ziet.dev` trong lúc job deploy chạy sẽ sinh ra `index.html` hiển thị vĩnh viễn "Không thể tải catalog" cho mọi khách truy cập cho tới khi có người deploy lại. Ngược lại, "Top hôm nay" đứng yên nhiều ngày trong khi crawler vẫn nhập phim mới.

**Sửa:** hoặc ngừng prerender các route phụ thuộc dữ liệu (`routeRules: { '/': { ssr: false } }`), hoặc refetch phía client (`{ server: false }` / `refresh()` trong `onMounted`), và đặt `nitro.prerender.failOnError: true` để build hỏng thì fail thay vì ship trang lỗi.

### C-05 · Trang xem phim hạ cấp session xuống `anonymous` khi gặp *bất kỳ* lỗi nào ✅☑️

[frontend/app/pages/watch/[slug].vue:326](../frontend/app/pages/watch/%5Bslug%5D.vue#L326)

```js
} catch {
  authState.value = "anonymous";
  loadLocalResumePosition();
}
```

Khối `catch` trần không phân biệt 401 với lỗi mạng, CORS, 500, hay timeout 524 mà chính trang assistant đã lường trước.

**Hệ quả:** người dùng đã đăng nhập mở `/watch/foo` đúng lúc API chập chờn → một lời gọi `/v1/me/library` hỏng khiến `authState` thành `anonymous` vĩnh viễn cho phần còn lại của trang → xem hết phim mà **không có gì được ghi lên server**. Lịch sử ở `/my-list` và hàng "Xem tiếp" ở trang chủ vẫn trống, không có thông báo lỗi. Kết hợp với M-42 (thay đổi chưa commit đã bỏ checkpoint định kỳ), không còn cơ hội phục hồi nào sau đó.

**Sửa:** chỉ coi 401/403 là anonymous; các lỗi khác giữ `authState` ở `unknown`, retry có backoff, và flush tiến độ localStorage lên `/v1/me/history` khi một lời gọi có xác thực thành công trở lại.

### C-06 · Chỉ 3 tập đầu tiên bấm được, và tập đang chọn không nằm trong URL ✅☑️

[frontend/app/pages/watch/[slug].vue:872](../frontend/app/pages/watch/%5Bslug%5D.vue#L872)

`v-for="(item, index) in playback.episodes.slice(0, 3)"`, và nút "Xem tất cả" ở dòng 862 **không có handler `@click`**. `selectEpisode` chỉ đổi state cục bộ, không gọi `router.replace`; `route.query.episode` được đọc đúng một lần lúc setup và không có watcher.

**Hệ quả:** phim bộ 10 tập chỉ lộ ra tập 1–3, tập 4+ không thể tới được trừ khi sửa tay query string. Sau khi chuyển từ tập 1 sang tập 2, thanh địa chỉ vẫn là `/watch/slug` — F5, chia sẻ link, hoặc bấm Back đều quay về tập 1 (Back thực chất rời hẳn trang xem vì không có history entry nào được push). `CatalogSeed` seed sẵn các series 4, 10, 12 và 24 tập nên đây không phải tình huống giả định.

**Sửa:** render toàn bộ `playback.episodes`; trong `selectEpisode` gọi `router.replace({ query: { ...route.query, episode: … } })`; thêm `watch(() => route.query.episode, …)` cho back/forward.

### C-07 · Instance hls.js rò rỉ / gắn đôi do race sau `await` ✅⚠️

[frontend/app/pages/watch/[slug].vue:190](../frontend/app/pages/watch/%5Bslug%5D.vue#L190)

`loadEpisode()` huỷ player cũ **trước** `await import("hls.js")` và gán player mới **sau** đó, không có cờ mounted và không có guard chống gọi lại đồng thời.

Hai kịch bản:
- **Unmount race:** người dùng rời trang trong lúc chunk hls.js đang tải. `onBeforeUnmount` chạy `hls?.destroy()` khi `hls` vẫn là `null`; import resolve xong mới tạo engine, gắn vào `<video>` đã detach → engine đó chạy mãi, không ai huỷ.
- **Re-entrancy:** chuyển tập nhanh hai lần → hai engine cùng tồn tại, engine đầu rò rỉ.

**Sửa:** dùng generation token — `const token = ++loadToken;` trước `await`, rồi ngay sau `await`: `if (token !== loadToken || !isMounted) { instance.destroy(); return; }` trước khi `attachMedia`.

---

## 2. Bảo mật và chống lạm dụng

### M-08 · Endpoint crawler không có authorization; trang `/admin/crawler` được prerender công khai ⚠️☑️

[backend/src/ZMovie.Api/Endpoints/CrawlerEndpoints.cs:9](../backend/src/ZMovie.Api/Endpoints/CrawlerEndpoints.cs#L9) · [frontend/app/pages/admin/crawler.vue:88](../frontend/app/pages/admin/crawler.vue#L88)

Nhóm `/v1/admin/crawler` không chain `.RequireAuthorization()` ở bất kỳ route nào, khác với mọi nhóm khác trong codebase. `POST /start` chạm tới `db.Database.MigrateAsync(ct)`, tức một request ẩn danh chạy migration schema. `EndPage = null` được `TryStart` chấp nhận → crawl toàn bộ catalog không giới hạn. `GET /status` trả `exception.GetBaseException().Message` nguyên văn.

**Đánh giá lại mức độ:** `MapCrawlerEndpoints()` chỉ được gọi trong `if (app.Environment.IsDevelopment())`, và ASP.NET Core mặc định môi trường là *Production* khi `ASPNETCORE_ENVIRONMENT` không được đặt — nên container deploy **không** map các route này. Đây là vấn đề phòng thủ theo lớp trên một bề mặt chỉ có ở dev, không phải lỗ hổng production. Tuy vậy phía frontend, `frontend/.output/public/admin/crawler/` **được prerender và deploy công khai**, không có `definePageMeta` middleware nào (thư mục `frontend/app/middleware` không tồn tại).

**Sửa:** chain `.RequireAuthorization("Admin")` lên MapGroup bất kể môi trường; bắt buộc `EndPage` và clamp khoảng trang; thay message exception thô bằng correlation id; thêm middleware `admin` cho route frontend và loại `/admin/**` khỏi danh sách prerender.

### M-09 · Bộ đếm lượt xem ẩn danh có thể replay vô hạn ☑️

[backend/src/ZMovie.Api/Endpoints/CatalogEndpoints.cs:33](../backend/src/ZMovie.Api/Endpoints/CatalogEndpoints.cs#L33)

Endpoint là anonymous, và khoá chống trùng 30 phút trong `EfUserLibraryStore.RecordAsync` dựa trên `sessionId` — hoặc là cookie do chính caller đặt, hoặc là một Guid mới server sinh ra khi thiếu cookie. Script không gửi cookie sẽ nhận danh tính chống trùng mới ở **mỗi request**. Không có rate limiter nào trong toàn bộ ứng dụng.

**Hệ quả:** thổi phồng tuỳ ý view count và thứ hạng ở `/v1/discovery/top/{period}`; mỗi request đồng thời chèn một dòng vào `title_view_events` (bảng append-only, không retention).

**Sửa:** thêm rate limiter theo IP đã forward; coi request không mang sẵn cookie `zmovie.analytics-session` là *không tính* (đặt cookie, trả về số hiện tại, không ghi).

### M-10 · Không persist Data Protection key → mất toàn bộ session mỗi lần deploy ☑️

[backend/src/ZMovie.Api/Program.cs:43](../backend/src/ZMovie.Api/Program.cs#L43)

Toàn bộ mô hình xác thực là cookie `zmovie.session` được mã hoá bằng Data Protection, nhưng `AddDataProtection().PersistKeysTo…().SetApplicationName(…)` chưa từng được gọi (grep toàn repo: 0 kết quả). Key ring mặc định nằm trong filesystem container, không mount volume.

**Hệ quả:** mỗi lần redeploy/restart container sinh key ring mới → mọi cookie đang lưu hành không giải mã được → `OnRedirectToLogin` trả 401 cho mọi lời gọi có xác thực. Tất cả người dùng bị đăng xuất âm thầm sau mỗi lần deploy. Nếu sau này chạy nhiều replica thì lỗi thành ngẫu nhiên theo request.

**Sửa:** `builder.Services.AddDataProtection().SetApplicationName("zmovie").PersistKeysToDbContext<CatalogDbContext>()` (hoặc volume/blob dùng chung) trước `AddAuthentication`.

### M-11 · `GET /v1/assistant/*` ghi DB và gọi LLM; không endpoint riêng tư nào đặt `Cache-Control` ✅☑️

[backend/src/ZMovie.Api/Endpoints/AssistantEndpoints.cs:18,30](../backend/src/ZMovie.Api/Endpoints/AssistantEndpoints.cs#L18)

Cả hai route GET đều chảy vào `RecordImpressionAsync` (INSERT vào `assistant_learning_events`), và GET chat còn phát một request LLM ra ngoài (timeout 15s). GET theo chuẩn phải *an toàn* và bị trình duyệt/prefetcher/proxy tự do retry và cache. Ngoài ra, grep `Cache-Control|OutputCache|ResponseCache` trên `backend/src` không có kết quả nào, trong khi `/v1/auth/me`, `/v1/me/library`, `/v1/discovery/for-you` và cả hai GET assistant đều trả dữ liệu riêng tư theo người dùng qua cookie auth — cookie (khác với header `Authorization`) **không** ngăn shared cache lưu response.

**Sửa:** xoá hẳn biến thể GET (frontend chỉ gọi POST — `assistant.vue:105`), hoặc làm chúng read-only; và thêm `Cache-Control: no-store, private` cho các endpoint riêng tư.

### M-12 · Token truy vấn người dùng lưu bằng SHA-256 không salt ☑️

[backend/src/ZMovie.Infrastructure/Assistant/EfAssistantLearningStore.cs:106](../backend/src/ZMovie.Infrastructure/Assistant/EfAssistantLearningStore.cs#L106)

`HashFeature` là SHA-256 trần: không salt, không pepper, không key. Miền đầu vào là từ đơn viết thường trong một từ vựng tự nhiên nhỏ, nên toàn bộ ánh xạ khôi phục được bằng cách băm một từ điển 100k từ trong vài giây. Cột `features` nằm cạnh `user_id` dạng plaintext.

**Hệ quả:** bất kỳ ai có quyền đọc bảng (backup, replica analytics, credential read-only bị lộ) khôi phục nguyên văn từ khoá truy vấn theo từng người dùng — bao gồm chính các trigger cảm xúc mà changeset này cố tình đi tìm: "buồn", "cô đơn", "stress".

**Sửa:** hoặc chỉ lưu ID của tập term đóng trong mood rules (không lưu từ tuỳ ý của người dùng), hoặc dùng HMAC-SHA256 với khoá server giữ ngoài database. Kèm theo: thêm job xoá dữ liệu quá 180 ngày cho khớp với cửa sổ đọc đã có.

### M-13 · Prompt injection qua synopsis crawl từ OPhim ⚠️☑️

[local-ai/server.mjs:68](../local-ai/server.mjs#L68)

Nội dung từ nguồn ngoài (synopsis phim crawl về) và văn bản người dùng cùng được nội suy vào **một message role `user`** dưới dạng `JSON.stringify(catalog)`, không phân tách, không escape, không kiểm tra output. Lớp bảo vệ duy nhất là một câu system mềm mà model `qwen3:0.6b` sẽ không tuân thủ đáng tin cậy.

**Ghi chú:** khẳng định trong `docs/backend-architecture.md` rằng "generator không tạo được suggestion ID nên text của nó không quyết định phim nào xuất hiện" **là đúng** — danh sách phim do backend quyết định. Nhưng *nội dung câu trả lời* thì bị chi phối được.

**Sửa:** cắt ngắn và làm sạch synopsis trước khi vào prompt (bỏ newline, cắt ~300 ký tự), đặt dữ liệu catalog trong khối rào rõ ràng không mang tính chỉ dẫn, và validate output phía server — rơi về `FallbackMessage` khi output bất thường.

### M-14 · LocalAi mặc định trỏ tới host cá nhân qua HTTP không mã hoá, service không xác thực ✅

[backend/src/ZMovie.Infrastructure/Assistant/LocalAiAssistantTextGenerator.cs:12](../backend/src/ZMovie.Infrastructure/Assistant/LocalAiAssistantTextGenerator.cs#L12)

```csharp
public string BaseUrl { get; set; } = "http://ziet-mac.ts.bantool.net:8788";
```

Giá trị mặc định được biên dịch vào assembly Infrastructure. `local-ai/server.mjs` không xác thực caller nào.

**Hệ quả:** bất kỳ vị trí mạng nào giữa API và host đó quan sát được, ở dạng cleartext, truy vấn assistant của từng người dùng (thường nêu trạng thái cảm xúc) kèm danh sách phim suy ra từ lịch sử xem.

**Sửa:** đổi mặc định thành loopback và bắt operator cấu hình `BaseUrl` tường minh; dùng HTTPS hoặc tunnel xác thực hai chiều cho mọi đích không phải loopback; yêu cầu shared secret header ở `server.mjs` (so sánh bằng `timingSafeEqual`).

### M-15 · CORS âm thầm rơi về `http://localhost:3000` kèm credentials ☑️

[backend/src/ZMovie.Api/Program.cs:39](../backend/src/ZMovie.Api/Program.cs#L39)

`FrontendOrigin` chỉ tồn tại trong `appsettings.Development.json`; ở production nó phải đến từ Infisical. Nếu secret thiếu hoặc bị đổi tên, toán tử `??` nuốt lỗi và API production khởi động với `http://localhost:3000` là origin tin cậy **có credentials** — trong khi connection string ngay dòng 52 lại `throw` đúng cách khi thiếu cấu hình.

**Sửa:** áp dụng cùng khuôn mẫu: `?? (builder.Environment.IsDevelopment() ? "http://localhost:3000" : throw new InvalidOperationException(…))`.

### M-16 · Không xử lý forwarded headers, không HSTS, HTTPS redirect bị comment ☑️

[backend/src/ZMovie.Api/Program.cs:151](../backend/src/ZMovie.Api/Program.cs#L151)

API phục vụ HTTP trần trên port 8080 sau một edge proxy, nhưng `UseForwardedHeaders` chưa từng được gọi → `Request.IsHttps` luôn `false` và `RemoteIpAddress` luôn là địa chỉ proxy. `app.UseHttpsRedirection()` bị comment, không có `UseHsts()`, `AllowedHosts` là `*`.

**Hệ quả cụ thể:** `CatalogEndpoints.cs:58` đặt cookie analytics với `Secure = context.Request.IsHttps` — luôn là `false` sau proxy, nên `zmovie.analytics-session` được ghi **không có thuộc tính Secure** trên site HTTPS. Ngoài ra mọi rate limiting theo IP sau này sẽ nhìn thấy đúng một IP.

**Sửa:** `app.UseForwardedHeaders(…XForwardedFor | XForwardedProto)` làm middleware đầu tiên, thêm `app.UseHsts()` cho non-Development.

### M-17 · `Guid.Parse` trên claim đã null-forgive → 500 thay vì 401 ✅

[backend/src/ZMovie.Api/Endpoints/AuthEndpoints.cs:70](../backend/src/ZMovie.Api/Endpoints/AuthEndpoints.cs#L70)

`RequireAuthorization()` chỉ đảm bảo *có* principal, không đảm bảo principal đó mang `NameIdentifier` parse được. Toán tử `!` che warning, nên claim thiếu hoặc không phải Guid trở thành `ArgumentNullException`/`FormatException` ném ra khỏi delegate. Khuôn mẫu này hiện lặp lại ở **9 chỗ**, gồm cả 3 route assistant và route `/v1/assistant/feedback` vừa thêm.

**Sửa:** tách một helper dùng `Guid.TryParse` và trả `Results.Unauthorized()` khi hỏng (giống `CatalogEndpoints.UserIdOrNull` đã có), dùng lại ở `AuthEndpoints`, `AssistantEndpoints`, `DiscoveryEndpoints`.

---

## 3. Dữ liệu và EF Core

### M-18 · Upsert kiểu check-then-insert đua nhau → HTTP 500 ☑️

[backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs:22](../backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs#L22)

Ba đường ghi dùng read-then-insert, không transaction, không xử lý conflict ở DB, không retry: `SaveAsync` (PK `(user_id, title_id)`), `RecordProgressAsync` (PK `(user_id, playable_id)`), `UpsertAsync` cho review (unique index). `EfUserIdentityStore.UpsertGoogleUserAsync` cũng vậy. Đáng chú ý: tác giả đã ý thức được lớp lỗi này với view events (`RecordAsync` mở transaction và lấy `pg_advisory_xact_lock`) nhưng không áp dụng cho ba đường còn lại.

**Hệ quả:** double-click "Lưu vào danh sách", hoặc trang xem bắn `recordWatchProgress` từ `onVideoPause` và `onPageExit` gần như đồng thời, hoặc hai tab → cả hai cùng đọc không thấy dòng nào, cùng INSERT, request thứ hai nhận Postgres 23505 và trả 500.

**Sửa:** `INSERT … ON CONFLICT … DO UPDATE` qua `ExecuteSqlInterpolatedAsync`, hoặc tối thiểu bắt `DbUpdateException` với inner `PostgresException { SqlState: "23505" }` và coi là thành công.

### M-19 · `assistant_learning_events` không có uniqueness, không có retention ✅☑️

[backend/src/ZMovie.Infrastructure/Persistence/CatalogDbContext.cs:82](../backend/src/ZMovie.Infrastructure/Persistence/CatalogDbContext.cs#L82)

Cả hai index đều **không unique**, và `RecordFeedbackAsync` `Add` một dòng mới vô điều kiện cho mọi lời gọi. `GetTitleScoresAsync` sau đó `Sum` trên tất cả. `RecordImpressionAsync` chèn tới 8 dòng mỗi truy vấn assistant, và được gọi từ cả hai endpoint **GET**. Không có job dọn dẹp; bộ lọc 180 ngày chỉ áp ở thời điểm đọc nên dòng cũ tích tụ vĩnh viễn.

**Hệ quả:** `assistant.vue:203` bắn `recordFeedback` mỗi lần click poster, không dedupe phía client. Người dùng click → back → click 20 lần sẽ ghi 20 dòng ×0.5 reward, cộng dồn thành 10 cho phim đó. Với `like` (+4), khoảng 20 lần replay là bão hoà mức clamp ±6 và ghim phim đó lên đầu mọi gợi ý sau này (xem thêm L-31).

**Sửa:** thêm unique index `(UserId, RecommendationId, TitleId, EventType)` kèm `ON CONFLICT DO UPDATE`; chuyển ghi impression ra khỏi đường request (channel/queue nền); thêm job xoá theo retention.

### M-20 · Bảng `titles` không có index nào ngoài `slug` ☑️

[backend/src/ZMovie.Infrastructure/Persistence/CatalogDbContext.cs:27](../backend/src/ZMovie.Infrastructure/Persistence/CatalogDbContext.cs#L27)

Ba đường đọc nóng đều `ORDER BY (Featured, Year)` mà không có index hỗ trợ: `GetHomeAsync` (Take 80), `ListAsync` (Take 500), `GetRecommendationCandidatesAsync` (Take 500). `ListAsync` còn lọc bằng `Contains(q)` và `ILike($"%{genre}%")` — pattern có wildcard đầu chuỗi mà không index btree nào phục vụ được, và không có index trigram/GIN.

**Hệ quả:** sau một lần `--import-ophim-catalog --all` (hàng chục nghìn phim), mỗi lần tải trang chủ là một `Seq Scan on titles` + sort toàn bảng để lấy 80 dòng. `Take(500)` không loại bỏ chi phí sort.

**Sửa:** `title.HasIndex(x => new { x.Featured, x.Year })` + migration `CREATE INDEX … (featured DESC, year DESC)`; thêm GIN `pg_trgm` cho các cột tìm kiếm, hoặc đẩy hết text search sang Meilisearch.

### M-21 · `COUNT(*)` không giới hạn trên `title_view_events` chạy mỗi lần xem chi tiết, ngay trong advisory lock ☑️

[backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs:62](../backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs#L62)

`RecordAsync` đếm toàn bộ view event của phim **trong khi vẫn giữ** `pg_advisory_xact_lock`. `CachedViewAnalyticsStore` — bất chấp tên gọi và doc comment "Protects the event table from read bursts" — chỉ cache `GetTopAsync`; `GetViewCountAsync` đi thẳng qua.

**Hệ quả:** khi một phim hot tích luỹ 1M event, mỗi lượt xem trang chi tiết quét 1M index entry chỉ để hiển thị con số, và mọi người xem đồng thời bị serialize sau advisory lock.

**Sửa:** cột `view_count` denormalized tăng trong cùng transaction (hoặc bảng rollup), cache `GetViewCountAsync`, và tối thiểu là chuyển phép đếm ra ngoài transaction có lock.

### M-22 · `EfAssistantLearningStore` nuốt mọi exception DB ✅☑️

[backend/src/ZMovie.Infrastructure/Assistant/EfAssistantLearningStore.cs:40](../backend/src/ZMovie.Infrastructure/Assistant/EfAssistantLearningStore.cs#L40)

Cả ba method đều `catch (Exception)` và hạ xuống log Warning. Ý định là graceful degradation, nhưng nó nuốt luôn missing-table (42P01), missing-column (42703), tràn max-length trên `features`/`event_type`, cạn connection pool và deadlock.

Cộng với C-01, tính năng ship ra ở trạng thái hoàn toàn không hoạt động **mà API vẫn báo thành công**.

**Sửa:** thu hẹp catch về đúng nhóm transient (`NpgsqlException` với SqlState transient, `TimeoutException`), để lỗi schema/dữ liệu propagate, và log ở mức Error.

### M-23 · Không có ModelSnapshot, test chỉ chạy InMemory → không gì kiểm chứng schema khớp model ☑️

[backend/tests/ZMovie.Api.Tests/Infrastructure/TestDatabase.cs:13](../backend/tests/ZMovie.Api.Tests/Infrastructure/TestDatabase.cs#L13)

Không có `CatalogDbContextModelSnapshot.cs` — cả 14 migration đều viết tay, nên `dotnet ef migrations has-pending-model-changes` không phát hiện được drift. Test suite dùng provider InMemory: bỏ qua `ToTable`, `HasMaxLength`, unique index, FK và `IsConcurrencyToken` trên `titles.updated_at`, và không chạy một migration nào.

**Drift đã tồn tại:** `202607230005_MoveTitleViewEventsToEngagement` đổi tên `viewer_id` → `session_id` và tạo index mới, nhưng **không drop** `ix_title_view_events_title_id_viewer_id_episode_number_viewed_at` tạo ở `202607220001` — các database đã deploy đang mang một index 4 cột thừa trên chính bảng ghi nhiều nhất.

Test mới `Assistant_learning_records_feedback_and_returns_contextual_title_scores` pass trên InMemory nhưng không chứng minh gì về việc migration `202607260001` có khớp `CatalogDbContext` hay không.

**Sửa:** thêm test tích hợp Testcontainers/Postgres thật chạy `Database.MigrateAsync()` và assert không còn pending model changes; commit `CatalogDbContextModelSnapshot`; thêm migration drop index thừa.

### L-24 · Toàn bộ khoá ngoại bị bỏ ở migration 003 và chưa bao giờ khôi phục ☑️

[backend/src/ZMovie.Infrastructure/Persistence/Migrations/202607230003_MoveUserLibraryToEngagement.cs:12](../backend/src/ZMovie.Infrastructure/Persistence/Migrations/202607230003_MoveUserLibraryToEngagement.cs#L12)

`grep AddForeignKey` trên toàn bộ 14 migration: không có kết quả. Bốn FK cascade bị drop để dời schema chưa từng được tạo lại, kể cả trong `Down()`. `title_reviews` và `assistant_learning_events` được tạo mới hoàn toàn không có FK. `CatalogDbContext.OnModelCreating` khai báo 0 quan hệ `HasOne`/`WithMany` nên EF sẽ không tự sinh lại.

**Hệ quả:** xoá một phim sẽ cascade `title_view_events` (FK này còn sót) nhưng bỏ mồ côi `saved_titles`, `watch_history`, `title_reviews`, `assistant_learning_events`.

### L-25 · `GetHistoryAsync` nạp toàn bộ lịch sử rồi khử trùng trong bộ nhớ ☑️

[backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs:15](../backend/src/ZMovie.Infrastructure/Engagement/EfUserLibraryStore.cs#L15)

Không có `Take()`; `GroupBy(TitleId).First()` chạy phía client. `watch_history` khoá theo `(user_id, playable_id)` nên mỗi tập đã xem là một dòng. Index `ix_watch_history_user_id_title_id_updated_at` tồn tại đúng cho shape này nhưng không được dùng vì grouping không xuống tới SQL. Hàm này nằm trên đường nóng của assistant (`CatalogAssistantStore.SearchAsync:37`).

### L-26 · `AddPlayableProgress.Down()` không thể chạy ☑️

[backend/src/ZMovie.Infrastructure/Persistence/Migrations/202607230004_AddPlayableProgress.cs:20](../backend/src/ZMovie.Infrastructure/Persistence/Migrations/202607230004_AddPlayableProgress.cs#L20)

`Up()` mở rộng PK từ `(user_id, title_id)` sang `(user_id, playable_id)` chính là để một user có nhiều dòng cho nhiều tập. `Down()` drop `playable_id` rồi cố khôi phục PK hẹp — bất khả thi vì bảng đang hợp lệ chứa nhiều dòng trùng `(user_id, title_id)`. Rollback sẽ fail giữa chừng sau khi cột đã bị drop.

---

## 4. Chất lượng assistant / AI

### M-27 · `AssistantMood` so khớp ordinal + phân biệt hoa thường ✅☑️

[backend/src/ZMovie.Application/Assistant/AssistantMood.cs:19](../backend/src/ZMovie.Application/Assistant/AssistantMood.cs#L19)

`string.Contains(string)` là ordinal và phân biệt hoa thường; toàn bộ trigger viết thường và được kiểm tra trên `message` **thô, chưa lowercase**. Điều này mâu thuẫn với chính `Words()` ở dòng 37–38 vốn *có* gọi `ToLowerInvariant()` — phía token được chuẩn hoá, phía trigger thì không.

Kết quả chạy thực tế: `WantsComfort("Hôm nay tôi Buồn")` → `False`; `"Buồn quá"` → `False`; `"I am Sad today"` → `False`; `"I feel STRESS"` → `False`; và chuỗi NFD (bàn phím macOS/iOS phát ra) `"hôm nay tôi buồn"` → `False`.

**Hệ quả:** tính năng "mood-aware" của commit `f154be1` im lặng không kích hoạt với câu tiếng Việt viết hoa đầu câu — tức phần lớn input thật.

**Sửa:** chuẩn hoá một lần ở đầu vào: `message.Normalize(NormalizationForm.FormC).ToLowerInvariant()` rồi mới match; bỏ hoặc thêm ranh giới từ cho trigger mơ hồ `"down"`; thêm test với input viết hoa.

### L-28 · Term mood bị xé thành mảnh 2 ký tự rồi gán trọng số 3 ☑️

[backend/src/ZMovie.Application/Assistant/AssistantMood.cs:30](../backend/src/ZMovie.Application/Assistant/AssistantMood.cs#L30)

`Words` là regex từ ≥2 ký tự, nên mọi term nhiều từ bị băm: `"hy vọng"` → `hy` + `vọng`; `"tình bạn"` → `tình` + `bạn`; `"feel good"` → `feel` + `good`. Mỗi mảnh vào dictionary với weight 3, rồi `CatalogAssistantStore.Score` match bằng `Contains` chứ không theo ranh giới từ.

**Hệ quả:** `hy` khớp "hy sinh"/"Hy Lạp", `bạn` khớp gần như mọi synopsis, `lành` khớp "trong lành". Mỗi false hit ăn tới 3×(5+3+1) điểm, đủ để đẩy phim không liên quan lên trên.

### M-29 · TF-IDF cold start trả về phim theo thứ tự Guid nhưng vẫn được +4 ☑️

[backend/src/ZMovie.Infrastructure/Recommendations/Models/TinyTfidfRecommendationModel.cs:43](../backend/src/ZMovie.Infrastructure/Recommendations/Models/TinyTfidfRecommendationModel.cs#L43)

Nếu không phim seed nào của user nằm trong cửa sổ candidate, `userVector` toàn 0, mọi `Dot()` bằng đúng 0.0f, và xếp hạng suy biến về tie-break `ThenBy(x => x.Key)` — tức 24 phim có Guid nhỏ nhất về mặt số học. Những ID tuỳ tiện đó sau đó được cộng +4, tự nó đã vượt ngưỡng lọc `Score > 0`.

**Sửa:** trả list rỗng khi `userVector` có magnitude bằng 0; và trong `CatalogAssistantStore` chỉ cộng +4 cho candidate đã có base score > 0 (personalization *rerank*, không *inject*).

### M-30 · Retrieval chỉ rerank một lát cắt top-500 cố định ✅☑️

[backend/src/ZMovie.Infrastructure/Catalog/CatalogLibraryReader.cs:40](../backend/src/ZMovie.Infrastructure/Catalog/CatalogLibraryReader.cs#L40)

`GetRecommendationCandidatesAsync` lấy `Take(500)` theo `(Featured, Year)` — một lát cắt **độc lập với truy vấn**. Mọi phim xếp dưới hạng 500 không bao giờ trả về được, dù tên khớp chính xác. Đây cũng là nguyên nhân gốc của M-29: phim đã lưu/đã xem của user thường không nằm trong corpus.

Lưu ý thêm: `GetLibraryTitlesAsync` ngay trên đó (dòng 30) **không có `Take()`** — nạp toàn bộ bảng `titles`, đúng loại truy vấn mà comment ở dòng 34–36 cảnh báo gây timeout 524.

**Sửa:** đẩy bộ lọc token xuống database (ILIKE / full-text / Meilisearch) *trước* `Take(500)`, để 500 giới hạn khối lượng công việc mỗi truy vấn thay vì giới hạn phim nào có thể được tìm thấy.

### L-31 · Feedback không idempotent ☑️

[backend/src/ZMovie.Application/Assistant/AssistantLearningContracts.cs:48](../backend/src/ZMovie.Application/Assistant/AssistantLearningContracts.cs#L48)

Không handler nào kiểm tra feedback cùng `(userId, recommendationId, titleId, eventType)` đã tồn tại chưa — tra cứu impression chỉ xác nhận *có* impression, không xác nhận feedback là mới. Kết hợp M-19: replay `{eventType:"like"}` khoảng 20 lần là bão hoà clamp ±6 và ghim phim đó lên đầu mọi gợi ý sau này.

### M-32 · Feedback thất bại vẫn trả HTTP 200 với body `false` ☑️

[backend/src/ZMovie.Application/Assistant/AssistantLearningContracts.cs:54](../backend/src/ZMovie.Application/Assistant/AssistantLearningContracts.cs#L54)

Mọi đường thất bại trong store đều `return false`: `recommendationId` lạ/của người khác, event type không hỗ trợ, hay exception DB bị nuốt. Handler trả thẳng `false` như *giá trị thành công* của `ErrorOr<bool>` → `200 OK` với body `false`.

**Sửa:** map `false` thành `Error.NotFound("assistant.recommendation.not_found", …)` và tách biệt "ghi hỏng" khỏi "không tìm thấy impression".

### L-33 · `local-ai` gửi tới 8 synopsis đầy đủ, không `num_ctx` ☑️

[local-ai/server.mjs:71](../local-ai/server.mjs#L71)

User turn có thể tới ~32 KB synopsis (~10–15k token tiếng Việt). Context mặc định của Ollama là 4096 token khi `num_ctx` không được đặt, và system message là message **đầu tiên** trong mảng — tức phần bị cắt.

**Hệ quả:** với truy vấn trả 8 phim có synopsis dài, model mất luôn các chỉ dẫn "trả lời bằng tiếng Việt", "không được liệt kê/bịa phim", "dưới 40 từ".

**Sửa:** cắt mỗi synopsis (~300 ký tự), chỉ gửi 3 match thực sự hiển thị cho người dùng, và đặt `num_ctx` tường minh.

### L-34 · `LocalAiAssistantTextGenerator` không bắt `JsonException`/`NotSupportedException` ✅

[backend/src/ZMovie.Infrastructure/Assistant/LocalAiAssistantTextGenerator.cs:33](../backend/src/ZMovie.Infrastructure/Assistant/LocalAiAssistantTextGenerator.cs#L33)

Thiết kế (theo chính docs) là mọi lỗi local-AI đều degrade về `FallbackMessage`. Nhưng bộ ba catch hiện tại có lỗ hổng: proxy trả HTTP 200 với `text/html` → `IsSuccessStatusCode` pass → `ReadFromJsonAsync` ném `NotSupportedException` → `POST /v1/assistant/chat` trả 500 thay vì fallback.

---

## 5. Crawler và import

### M-35 · Không giới hạn số trang, không rate limit, không User-Agent ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:42](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L42)

`EndPage = null` → `MaxPages = null` → "mọi trang upstream báo có", không trần. Form admin mặc định để trống `endPage` và `includeEpisodes = true`. Kiểm soát lịch sự duy nhất là delay cố định 300ms **mỗi worker** trong semaphore 3 slot → ~6–10 req/s liên tục, không token bucket, không jitter, không User-Agent nhận diện ZMovie. `totalPages` lấy hoàn toàn từ số upstream báo, chỉ được `Math.Max(1, …)` chặn dưới.

**Hệ quả:** một cú click với form mặc định bắn hàng chục nghìn request vào ophim1.com ở ~10 rps trong nhiều giờ, thang retry (tối đa 4 lần/request) khuếch đại thêm khi upstream gặp sự cố.

**Sửa:** trần số trang cứng phía server trong `TryStart`; đặt User-Agent nhận diện; thay delay cố định bằng rate limiter dùng chung; sanity-check `TotalItemsPerPage`.

### M-36 · Một request detail lỗi giết cả crawl, và semaphore/HttpClient bị dispose khi task còn chạy ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:90](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L90)

Không có cô lập lỗi theo từng item. `EnsureSuccess` ném cho mọi payload không "success", `GetJsonWithRetryAsync` ném lại cho status không transient (404). `Task.WhenAll` propagate ngay, unwind `ImportDetailsAsync`, và `using` dispose `concurrencyGate` **trong khi ~20 task detail khác của trang vẫn đang chạy** — chúng gọi `WaitAsync`/`Release()` trên `SemaphoreSlim` đã dispose và tiếp tục request trên `HttpClient` mà `OPhimCrawlerService.RunAsync` cũng đã dispose. Tất cả thành unobserved task exception.

Test `Throws_when_a_detail_request_fails` đang **khoá cứng** hành vi all-or-nothing này.

**Hệ quả:** một phim bị xoá upstream (404) giết một crawl nhiều giờ ở trang 900/1200; `SaveChangesAsync` của trang đó không chạy nên mất luôn cả trang.

**Sửa:** bắt exception trong lambda từng phim, trả `FetchedDetail` null và cộng dồn số slug bỏ qua vào report; await hết task trước khi dispose semaphore.

### M-37 · `Slug` không được giới hạn độ dài, khác mọi trường khác ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:130](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L130)

Mọi chuỗi khác lấy từ OPhim đều qua `Limit(...)` hoặc `BuildImageUrl`, riêng `Slug` — trường vừa là unique key vừa đến thẳng từ JSON không tin cậy — thì không. `OPhimGenreImporter.cs:21` cũng vậy với `Slug` (160) và `Name` (100).

**Hệ quả:** slug >160 ký tự làm `SaveChangesAsync` ném Npgsql 22001, huỷ nguyên trang ~24 phim và abort crawl.

**Sửa:** *bỏ qua* item có slug vượt độ dài (không truncate — truncate sẽ làm hai phim khác nhau đụng cùng một unique slug).

### M-38 · Đánh số tập dùng chung một biến đếm cho mọi server ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:161](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L161)

OPhim trả cùng một danh sách tập lặp lại theo từng server; `SelectMany` gộp tất cả vào một chuỗi dùng chung một biến `ordinal`. Tên số ("1","2") gộp đúng, nhưng mọi tên không phải số — "Full", "Tập 1", "FHD" — rơi xuống `ordinal` vốn cứ tăng xuyên server.

**Hệ quả:** một phim lẻ phục vụ bởi 3 server với `name: "Full"` bị ghi thành 3 tập đánh số 1, 2, 3 — trang xem hiển thị "Tập 1/2/3" cho một phim một phần.

**Sửa:** reset `ordinal` theo từng server, dedupe theo số tập với một server ưu tiên.

### L-39 · `ParseMinutes` lấy cụm số đầu tiên ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:256](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L256)

`"24 phút"` parse đúng, nhưng `"2 giờ 5 phút"` cho ra **2**. Test hiện tại (`OPhimCatalogImporterTests.cs:66/86`) đưa vào đúng chuỗi đó và assert `RuntimeMinutes == 2` — tức test đang *ghim* hành vi sai thay vì bắt nó.

**Sửa:** match riêng `(?<h>\d+)\s*(giờ|h)` và `(?<m>\d+)\s*(phút|min|m)`, tính `h*60+m`. Sửa test thành 125.

### L-40 · Dedupe slug case-insensitive nhưng tra cứu DB case-sensitive ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:59](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L59)

Code có lường trước biến thể hoa thường (dòng 55 group bằng `OrdinalIgnoreCase`, test có `'dup'`/`'DUP'`), nhưng đường tra cứu dòng đã tồn tại hoàn toàn case-sensitive. Phim đã import là `phim-abc`, lần sau upstream trả `Phim-ABC` → chèn dòng thứ hai; unique index không bắt được vì hai chuỗi khác nhau ở tầng byte.

### L-41 · Genre importer không dedupe, không retry ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimGenreImporter.cs:20](../backend/src/ZMovie.Infrastructure/Catalog/OPhimGenreImporter.cs#L20)

`SingleOrDefaultAsync` không thấy được entity vừa `Add` trong cùng vòng lặp, nên upstream lặp slug → hai `CatalogGenre` cùng slug được stage → `SaveChangesAsync` vi phạm unique index và **không genre nào được cập nhật**. Không có retry (khác `OPhimCatalogImporter.GetJsonWithRetryAsync`). `Program.cs:90` còn gọi nó với `new HttpClient()` không dispose và `CancellationToken.None` nên Ctrl+C không dừng được.

### L-42 · `TotalPages` báo cáo là của cả catalog, không phải khoảng đã yêu cầu ☑️

[backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs:75](../backend/src/ZMovie.Infrastructure/Catalog/OPhimCatalogImporter.cs#L75)

Crawl trang 1–2 của catalog 1200 trang → thanh tiến độ đứng ở 0% suốt rồi nhảy lên 100%. Crawl trang 900–902 → hiện 75% ngay lập tức.

### L-43 · `RunAsync` finally có thể dispose CTS của crawl vừa mới bắt đầu ☑️

[backend/src/ZMovie.Api/Services/OPhimCrawlerService.cs:132](../backend/src/ZMovie.Api/Services/OPhimCrawlerService.cs#L132)

`IsRunning = false` được publish trong một lần lấy lock, còn cleanup CTS ở một lần lấy lock *sau đó*. Giữa hai thời điểm, `TryStart` có thể thấy `IsRunning == false`, tạo CTS mới và chạy `RunAsync` mới — rồi `finally` của run cũ dispose CTS của run mới. UI admin poll status mỗi 1.5s nên cửa sổ này bị chạm thường xuyên.

---

## 6. Frontend

### M-44 · `play()` reject bị nối thẳng vào modal "video lỗi" ☑️

[frontend/app/pages/watch/[slug].vue:437](../frontend/app/pages/watch/%5Bslug%5D.vue#L437)

`HTMLMediaElement.play()` thường xuyên reject với `AbortError` không nghiêm trọng ("The play() request was interrupted by…") và `NotAllowedError` do chính sách autoplay. Tất cả đều bị leo thang thành cùng một modal nghĩa là "phim này không có nguồn hợp lệ". `<video>` lại có `@click="togglePlayback"`, nên một cú double-click trong lúc buffer là đủ.

**Sửa:** `play().catch(e => { if (e?.name === 'NotAllowedError') …; else if (e?.name !== 'AbortError') showUnavailableDialog(); })`.

### M-45 · Thay đổi chưa commit đã bỏ toàn bộ checkpoint tiến độ giữa chừng ✅

[frontend/app/pages/watch/[slug].vue:387](../frontend/app/pages/watch/%5Bslug%5D.vue#L387)

Diff đang có trong working tree xoá khối lưu định kỳ khỏi `onTimeUpdate`. Các call site còn lại chỉ là `pause`/`ended`, `pagehide`, `visibilitychange` và `onBeforeUnmount` — không còn checkpoint nào trong lúc phát liên tục. Thêm nữa, handler `visibilitychange` **không kiểm tra `document.visibilityState`**, nên nó bắn cả khi người dùng *quay lại* tab, gấp đôi số request mà nó định giảm.

**Hệ quả:** xem hết phim 2 tiếng không dừng rồi tab/trình duyệt bị kill đột ngột (crash, OOM, force-quit — đặc biệt là force-quit trên iOS Safari nơi `pagehide` không bắn) → không có gì được lưu.

**Sửa:** khôi phục checkpoint có throttle (giữ guard 30s trong `onTimeUpdate` — guard `Math.abs(currentTime - lastProgressSaved) < 1` vừa thêm đã đủ chặn bão request), hoặc `setInterval` bật khi `play` và clear khi `pause`/unmount. Đồng thời gate handler theo `document.visibilityState === 'hidden'`.

### M-46 · `browse.vue` chỉ fetch phía client, debounce không huỷ và không chống response cũ ☑️

[frontend/app/pages/browse.vue:74](../frontend/app/pages/browse.vue#L74)

Ba vấn đề trong một khối: (1) với `ssr: true` và deploy `nuxt generate`, trang browse ship ra HTML server không có một dòng catalog nào vì fetch duy nhất nằm trong `onMounted` — bề mặt khám phá/SEO chính chỉ render "Đang tải phim..."; (2) `searchTimer` không được clear khi unmount; (3) debounce không serialize gì — mỗi timer bắn một request độc lập và kết quả được gán vô điều kiện, không sequence token, không `AbortController` → response chậm của truy vấn cũ ghi đè kết quả mới.

### M-47 · Fullscreen gọi `requestFullscreen` trên `<div>` — không hoạt động trên iOS ☑️

[frontend/app/pages/watch/[slug].vue:470](../frontend/app/pages/watch/%5Bslug%5D.vue#L470)

iOS Safari không hiện thực Fullscreen API trên phần tử bất kỳ; chỉ `HTMLVideoElement.webkitEnterFullscreen()` chạy được. `toggleFullscreen` là async gọi từ `@click` không có `.catch`.

**Hệ quả:** trên iPhone nút fullscreen là no-op (ném unhandled rejection). Vì `<video>` có `playsinline` và không có attribute `controls`, người dùng iPhone **không có cách nào** xem toàn màn hình — trên một site streaming Việt Nam ưu tiên mobile.

### M-48 · Nút đổi ngôn ngữ không làm gì ở 4 trang; key `useAsyncData` không chứa locale ☑️

[frontend/app/pages/watch/[slug].vue:540](../frontend/app/pages/watch/%5Bslug%5D.vue#L540) · [frontend/app/pages/index.vue:44](../frontend/app/pages/index.vue#L44)

`index`, `browse`, `genres`, `assistant`, `movies/[slug]` đều bind `@locale-change="setLocale"`; `watch/[slug]`, `my-list`, `profile`, `admin/crawler` thì không — event bị bỏ rơi và cookie `zmovie-locale` không được ghi. Ngay cả khi được wire, hai key `useAsyncData` của trang watch là chuỗi thuần chỉ chứa slug nên đổi locale cũng không invalidate cache.

Ở tầng rộng hơn: locale là một cookie được đọc độc lập trong từng page thay vì một composable dùng chung. Trên route đã prerender, payload `vi` được tái sử dụng cho người dùng `en` → nhãn tiếng Anh nhưng tiêu đề/synopsis tiếng Việt, kèm hydration mismatch.

**Sửa:** tách `useLocale()` ghi cookie và expose ref; đưa locale vào mọi key (`() => \`discovery-home-${locale.value}\``) với `watch: [locale]`; chuyển `AppNavbar` vào layout sở hữu state locale.

### M-49 · Form đăng nhập/đăng ký thu email + mật khẩu rồi vứt đi ✅☑️

[frontend/app/pages/login.vue:12](../frontend/app/pages/login.vue#L12)

```js
function submitLogin() {
  notice.value = 'Chức năng đăng nhập đang được kết nối. …'
}
```

Hai trang production hiển thị form credential đầy đủ với `required`, kèm link "Quên mật khẩu?" chết và checkbox "Ghi nhớ đăng nhập", nhưng không thực hiện xác thực nào. Chỉ nút Google bên dưới là thật.

**Hệ quả:** người dùng gõ mật khẩu (thường là mật khẩu dùng lại) vào một site không có xác thực bằng mật khẩu, và trình duyệt/password manager sẽ mời lưu credential cho site đó.

**Sửa:** bỏ hẳn form credential (chỉ giữ Google + một dòng giải thích), hoặc implement `/v1/auth/password`. Trong lúc chờ, các input nên `disabled` thay vì `required`.

### M-50 · Không có layout/state phiên dùng chung: navbar gọi lại `/v1/auth/me` mỗi lần đổi route ☑️

[frontend/app/components/AppNavbar.vue:69](../frontend/app/components/AppNavbar.vue#L69)

Danh tính người dùng là state cục bộ của component, được suy ra lại bằng một round-trip mạng cho mỗi lần mount. Điều hướng home → browse → movie → watch phát 4 lời gọi `/v1/auth/me`, và trong lúc chờ mỗi lần navbar render trạng thái chưa đăng nhập → chip tài khoản nhấp nháy về "Đăng nhập" ở mỗi lần chuyển trang.

**Sửa:** đưa `AppNavbar` vào `app/layouts/default.vue` và backing bằng `useState('user')` nạp một lần.

### M-51 · `apiBaseUrl: '/'` chỉ đúng dưới `nuxt dev` ✅☑️

[frontend/app/plugins/api.ts:4](../frontend/app/plugins/api.ts#L4) · [frontend/nuxt.config.ts:34](../frontend/nuxt.config.ts#L34)

`routeRules: { '/v1/**': { proxy: 'http://localhost:5275/v1/**' } }` chỉ tồn tại trong dev server; trong build tĩnh nó biến mất im lặng. Tính đúng đắn phụ thuộc hoàn toàn vào việc ai đó export `NUXT_PUBLIC_API_BASE_URL`, không có guard nào. Plugin còn hardcode `https://movie-api.ziet.dev` cho nhánh SSR.

**Hệ quả:** chạy đúng lệnh `npm run deploy` được ghi trong README (không set env) sinh ra bundle với `apiBaseUrl='/'` → mọi request của trình duyệt đi tới `https://movie.ziet.dev/v1/...` và rơi vào trang 404 tĩnh. Job production đang bị comment cũng để placeholder `https://api-domain`.

**Sửa:** `apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL ?? ''` và fail-fast trong plugin khi rỗng ở build non-dev; bỏ fallback hardcode; thêm `test -n` vào `before_script` của job deploy.

### L-52 · Google OAuth client ID bị hardcode trong bundle production ☑️

[frontend/nuxt.config.ts:36](../frontend/nuxt.config.ts#L36)

`?? '39010162417-…apps.googleusercontent.com'` làm mặc định im lặng, và không job CI nào set `NUXT_PUBLIC_GOOGLE_CLIENT_ID`, nên nhánh "Google Sign-In chưa được cấu hình" trong `GoogleSignInButton.vue` không bao giờ chạy. Nếu `Google:ClientId` phía API (từ Infisical) khác ID này, mọi lần đăng nhập fail vì audience mismatch và người dùng chỉ thấy "Không thể đăng nhập với Google".

### L-53 · `app/types/api.d.ts` sinh ra nhưng không được import ở đâu ☑️

[frontend/app/types/api.d.ts:6](../frontend/app/types/api.d.ts#L6)

Script `generate:api` tồn tại và sinh type từ OpenAPI, nhưng mọi page tự khai báo lại shape response, nên `bun run typecheck` trong CI kiểm chứng frontend so với *phỏng đoán viết tay* chứ không phải contract thật. Ví dụ ngay trong working tree: nếu backend đặt tên field khác `recommendationId`, typecheck vẫn pass còn handler ở `assistant.vue:133-148` im lặng no-op ở `if (!message.recommendationId) return`.

### L-54 · Browse và movie-detail tải toàn bộ catalog; nút "Tải thêm" chết ☑️

[frontend/app/pages/browse.vue:409](../frontend/app/pages/browse.vue#L409)

`/v1/catalog/titles` không có tham số paging phía server, nên mỗi lượt vào browse và mỗi lần SSR trang chi tiết đều chuyển và parse toàn bộ catalog; lọc theo genre/type và sắp xếp làm ở client trên mảng đầy đủ; field `total` API trả về không được dùng.

### L-55 · Popover cài đặt player thiếu dismissal và ngữ nghĩa a11y ☑️

[frontend/app/pages/watch/[slug].vue:674](../frontend/app/pages/watch/%5Bslug%5D.vue#L674)

Không có click-outside, không có Escape, không focus trap, trigger thiếu `aria-expanded`/`aria-haspopup`; `aria-label` nút mute cố định là "Mute" bất kể `isMuted`. Không nhất quán với phần còn lại của trang vốn đã dùng primitive reka-ui (`AlertDialog`) xử lý sẵn những điều này. `reka-ui` đã là dependency — nên dùng `PopoverRoot`/`DropdownMenu`.

---

## 7. Validator, mapping lỗi và hợp đồng API

### L-56 · `RecordWatchProgressCommand` không có validator ☑️

[backend/src/ZMovie.Application/Engagement/LibraryContracts.cs:121](../backend/src/ZMovie.Application/Engagement/LibraryContracts.cs#L121)

Đây là command người dùng ghi được duy nhất mang một `double` và nó không có `AbstractValidator` nào. `POST /v1/me/history/{slug}` với `{"progressSeconds": -1e300}` hoặc `1e300` được chấp nhận và ghi xuống DB, rồi echo lại trong `/v1/me/library` và `ContinueWatching` → player seek tới vị trí vô nghĩa. `Slug` và `EpisodeNumber` cũng không bị chặn.

### L-57 · `SearchCatalogQuery` không có validator; `ListTitlesValidator` chỉ chặn `Query` ☑️

[backend/src/ZMovie.Application/Search/SearchQueries.cs:8](../backend/src/ZMovie.Application/Search/SearchQueries.cs#L8)

`Type`/`Genre` đi thẳng vào filter string Meilisearch, và ở nhánh fallback vào một query EF **không có `Take()`**. `GET /v1/search?type=<1 MB>` xây filter cỡ megabyte gửi đi mỗi request.

### L-58 · Filter Meilisearch nối chuỗi, chỉ escape dấu nháy chứ không escape backslash ☑️

[backend/src/ZMovie.Infrastructure/Search/SearchCatalogStore.cs:18](../backend/src/ZMovie.Infrastructure/Search/SearchCatalogStore.cs#L18)

`type.Replace("'", "\\'")` không xử lý backslash literal, nên `?type=a\` sinh ra `type = 'a\'` — chuỗi chưa đóng. Meilisearch từ chối, `EnsureSuccessStatusCode()` ném, catch nuốt, và mọi request kiểu đó **âm thầm degrade** xuống fallback Postgres không index.

**Sửa:** validate `type`/`genre` theo danh sách enum/genre đã biết và trả 400; nếu vẫn nối chuỗi thì escape backslash trước rồi mới tới dấu nháy.

### L-59 · `GoogleIdentityVerifier` chỉ bắt `InvalidJwtException` ✅☑️

[backend/src/ZMovie.Infrastructure/Identity/GoogleIdentityVerifier.cs:19](../backend/src/ZMovie.Infrastructure/Identity/GoogleIdentityVerifier.cs#L19)

`GoogleJsonWebSignature.ValidateAsync` tải certificate của Google qua mạng; khi thất bại nó ném `HttpRequestException`/`TaskCanceledException`, không phải `InvalidJwtException`. Kết quả: sự cố thoáng qua phía Google làm `POST /v1/auth/google` trả 500, frontend không phân biệt được "token của bạn sai" với "thử lại sau một phút". `SignInWithGoogleCommand` cũng không có validator giới hạn độ dài `Credential` trên một endpoint ẩn danh.

### L-60 · `ErrorType.Failure` map sang 503 → catalog rỗng bị báo là service down ☑️

[backend/src/ZMovie.Application/Catalog/CatalogQueries.cs:28](../backend/src/ZMovie.Application/Catalog/CatalogQueries.cs#L28)

`GetHomeAsync` trả null thuần tuý như một *điều kiện dữ liệu* (không phim nào gắn `Featured` và slug hero hardcode không tồn tại). Điều kiện đó lại được biểu diễn bằng `Error.Failure` — cũng chính là error type mặc định của ErrorOr — và `ApiResults` map nó thành **503 Service Unavailable**.

**Hệ quả:** môi trường đã import phim nhưng chưa gắn featured sẽ trả 503 từ `/v1/discovery/home`; load balancer, uptime monitor và CDN edge coi 503 là tín hiệu sức khoẻ instance và sẽ rút instance khỏi rotation.

### L-61 · `TitleListResponse.Total` là kích thước trang đã bị cắt, không phải tổng số khớp ☑️

[backend/src/ZMovie.Application/Catalog/CatalogContracts.cs:25](../backend/src/ZMovie.Application/Catalog/CatalogContracts.cs#L25)

`ListAsync`/`SearchAsync` không nhận skip/take nên contract không có khái niệm trang; cả hai implementation đặt `Total = items.Count`. Với truy vấn khớp >500 phim, API báo `Total: 500` và client không có cách nào biết mình đang nhìn một view bị cắt.

---

## 8. CI/CD, kiểm thử, tài liệu, vệ sinh repo

### M-62 · Job verify chỉ chạy trên pipeline merge request ✅☑️

[.gitlab/ci/backend.gitlab-ci.yml:18](../.gitlab/ci/backend.gitlab-ci.yml#L18)

Mọi job stage `verify` đều scope vào `merge_request_event`. Pipeline push-to-main (build và push image `:latest`/`:beta`) và pipeline tag (promote lên `:stable`/`:v*`) **không có build, không test, không lint**. `frontend:deploy:staging` còn khai báo `needs: []`.

Hook `pre-push` trong `lefthook.yml` chỉ chặn nhánh `dev/*`, nên push thẳng lên `main` không bị cản.

**Sửa:** mở rộng anchor `.backend-rules`/`.frontend-rules` cho push-to-main và tag; đặt `backend:image:build` `needs: [backend:test]`.

### M-63 · Job deploy frontend dùng `npm install` trong khi repo chỉ có `bun.lock` ☑️

[.gitlab/ci/frontend.gitlab-ci.yml:40](../.gitlab/ci/frontend.gitlab-ci.yml#L40)

Job verify cài bằng `bun install --frozen-lockfile`; job deploy chạy `npm install` trong thư mục **không có `package-lock.json`** → npm bỏ qua `bun.lock` và giải lại toàn bộ caret range theo bản mới nhất tại thời điểm deploy. Mọi dependency trong `package.json` đều là caret, gồm `nuxt: ^4.5.0` và `vue-router: ^5.2.0`.

Đồng thời **không job verify nào chạy production build** — `nuxt generate` lần đầu tiên chạy là ở chính job deploy (job này thậm chí có nhánh fallback vì layout output không đáng tin).

**Sửa:** dùng `bun install --frozen-lockfile` + `bun run generate` trong deploy, và thêm `bun run generate` vào job `frontend:lint`.

### M-64 · Không test nào chạm tầng HTTP, và coverage config loại trừ đúng phần đó ☑️

[backend/coverage.runsettings:8](../backend/coverage.runsettings#L8)

Test project không tham chiếu `Mvc.Testing`/`WebApplicationFactory` dù `Program.cs` đã khai báo `public partial class Program` đúng cho mục đích đó; rồi coverage config loại trừ mọi file trong `Endpoints/` cùng `Program.cs` khỏi báo cáo cobertura mà regex `coverage:` của CI đọc — nên con số coverage **che giấu** khoảng trống này.

Ví dụ trực tiếp: `POST /v1/assistant/feedback` vừa thêm chạy `Guid.Parse(...!)` inline trong delegate; cookie thiếu claim sẽ ném `ArgumentNullException`. Không dòng test nào chạm tới.

### L-65 · Script test frontend trỏ vào chỗ không có gì ☑️

[frontend/package.json:15](../frontend/package.json#L15)

`bun test app/lib` quét một thư mục chỉ có `utils.ts` (không phải file test) và exit sạch báo 0 test; `playwright test` sẽ fail command-not-found vì Playwright không phải dependency và không có config. Không cái nào được nối vào pipeline. Kết quả: 0 assertion phủ 13 page, gồm cả `watch/[slug].vue` và `assistant.vue` vừa sửa trong working tree này.

### L-66 · `.gitignore` gốc chỉ có một dòng; `.DS_Store` đã bị commit ☑️

[.gitignore:1](../.gitignore#L1)

File gốc chỉ chứa `.wrangler/`. Bằng chứng đã rò rỉ: `.DS_Store` và `backend/.DS_Store` đang được git track. Một `.env` ở thư mục gốc (đúng khuôn mẫu mà `frontend/README.md` và `docs/backend-architecture.md` mô tả, liệt kê `INFISICAL_CLIENT_SECRET` và `ConnectionStrings__ZMovie`) sẽ bị `git add -A` đưa vào commit.

**Sửa:** baseline `.DS_Store`, `.env`, `.env.*`, `!.env.example`, `node_modules/`, `**/bin/`, `**/obj/`, `.wrangler/`, `*.log`, `coverage/`, `test-results/`; rồi `git rm --cached .DS_Store backend/.DS_Store`.

### M-67 · Không có `.dockerignore`, Dockerfile dùng `COPY . .` ☑️

[backend/src/ZMovie.Api/Dockerfile:20](../backend/src/ZMovie.Api/Dockerfile#L20)

Không có `.dockerignore` ở bất kỳ đâu trong repo, nên `COPY . .` copy nguyên thư mục backend vào layer image — build artifact của máy dev, state wrangler, và bất kỳ file secret nào bị gitignore như `backend/.env` hay export user-secrets. Đây đúng là các giá trị mà `docs/backend-architecture.md` nói không bao giờ được vào container image.

**Sửa:** thêm `backend/.dockerignore` với `**/bin/`, `**/obj/`, `**/.wrangler/`, `**/.DS_Store`, `**/.env*`, `**/node_modules/`, `.git/`.

### L-68 · `backend:test` bắt buộc có Docker socket mà không test nào dùng ☑️

[.gitlab/ci/backend.gitlab-ci.yml:92](../.gitlab/ci/backend.gitlab-ci.yml#L92)

Dòng script đầu tiên là `test -S /var/run/docker.sock` và job export `DOCKER_HOST`, ngụ ý có integration test với database — nhưng không có Testcontainers ở đâu và mọi test dùng EF InMemory qua `TestDatabase`. Cổng chặn này không bảo vệ gì trong khi vẫn có thể fail cả stage verify.

### L-69 · `NUXT_SSR="false"` trong job deploy không có tác dụng ☑️

[.gitlab/ci/frontend.gitlab-ci.yml:59](../.gitlab/ci/frontend.gitlab-ci.yml#L59)

Biến CI này diễn đạt một ý định (ship SPA thuần, điều sẽ làm C-03 và C-04 không còn là vấn đề) mà Nuxt không bao giờ tôn trọng, trong khi build log lại in "SSR=false". Người debug tại sao `/movies/<slug>` 404 sẽ đọc log, thấy "SSR=false" và loại trừ prerendering — dù mọi trang được deploy đều là snapshot prerender.

### M-70 · `docs/backend-architecture.md` có 5 khẳng định đã sai ✅☑️

[docs/backend-architecture.md](./backend-architecture.md)

| Dòng | Tài liệu nói | Thực tế trong mã |
|---|---|---|
| ~13 | Module Catalog dùng schema PostgreSQL `catalog` | Migration `202607240001`/`202607240003` đã chuyển hết sang `public` và drop schema cũ |
| 28 | "Database migration and seed calls in `Program.cs` remain intentionally disabled" | Chúng **chạy tự động ở Development** (Program.cs:117–123) và **không chạy ở đâu khác** — tài liệu sai theo cả hai chiều |
| 87 | "Authentication has not yet been wired into the API or Nuxt BFF" | Cookie auth + Google ID token đã hiện thực đầy đủ (`AuthEndpoints.cs`, `GoogleIdentityVerifier.cs`), frontend đã đăng nhập được |
| 87–95 | Liệt kê `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI`… là cấu hình cần có | Luồng hiện tại là Google Identity Services phía client + xác thực ID token, chỉ cần `Google__ClientId` |
| ~97 | Identity và Engagement nằm trong "Planned modules" | Cả hai đã hiện thực (`ZMovie.Domain/Identity`, `ZMovie.Domain/Engagement`, `EfUserLibraryStore`, review, watch history) |

Danh sách endpoint ở phần "Implemented vertical slice" cũng thiếu toàn bộ `/v1/auth/*`, `/v1/me/*`, `/v1/assistant/*`, `/v1/discovery/for-you`, `/v1/discovery/top/*`.

**Sửa:** viết lại bốn mục "Implemented vertical slice", "Current runtime", "Authentication direction", "Planned modules" theo mã hiện tại.

---

## Phụ lục A — Hai giả thuyết đã bị bác bỏ

Ghi lại để không ai điều tra lại:

1. **"Chuyển tập ghi vị trí của tập cũ sang tập mới"** — giả thuyết cho rằng `element.pause()` trong `loadEpisode()` kích hoạt `onVideoPause` → POST `currentTime` cũ kèm `episodeNumber` mới. Không đúng: `recordWatchProgress` bail khi `currentTime.value < 5`, và `currentTime` đã bị đưa về 0 trước khi bất kỳ pause handler nào chạy — nhánh hls.js do `hls.destroy()` → `detachMedia` gọi `removeAttribute('src')` + `load()` đồng bộ; nhánh native HLS do `element.load()` reset vị trí trước khi event `pause` được dispatch.
2. **"Test probe không assertion sẽ chạy trong CI"** — file được nêu là file scratch chưa track do chính quá trình rà soát tạo ra, không thuộc repo. CI build từ git checkout nên nó không bao giờ được biên dịch.

## Phụ lục B — Thứ tự xử lý đề xuất

**Trước lần deploy tiếp theo**
1. C-01 migration khi deploy — nếu không, C-01 làm toàn bộ mảng learning vô hiệu trong im lặng
2. M-22 thu hẹp catch trong `EfAssistantLearningStore` — để C-01 không tái diễn kiểu "hỏng mà không ai biết"
3. C-02 validator cho `GetAssistantContextQuery` + rate limiter
4. C-05 phân biệt 401 với lỗi mạng ở trang xem
5. M-10 persist Data Protection keys

**Sprint kế tiếp**
6. C-03 + C-04 + M-51 — quyết định dứt điểm mô hình render/deploy của frontend (SPA fallback hay SSR thật), ba mục này cùng một gốc
7. C-06 danh sách tập đầy đủ + đồng bộ URL
8. M-27 chuẩn hoá mood matching — tính năng mood-aware hiện gần như không kích hoạt
9. M-19 unique index + retention cho `assistant_learning_events`
10. M-62 chạy verify trên pipeline main/tag

**Nợ kỹ thuật có thời hạn**
11. M-23 test tích hợp Postgres thật + ModelSnapshot (điều kiện tiên quyết để tin vào mọi migration sau này)
12. M-20 index `(featured, year)` trước lần import `--all` tiếp theo
13. M-18 upsert `ON CONFLICT`
14. M-30 đẩy filter xuống DB thay vì rerank top-500
15. M-70 cập nhật `docs/backend-architecture.md`
