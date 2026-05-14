# ZMovie Backend Database Design

Laravel app duoc khoi tao trong thu muc `backend`. Mac dinh dang dung SQLite theo `.env`, co the doi sang MySQL/PostgreSQL bang cac bien `DB_*`.

## Chay database

```bash
cd backend
php artisan migrate:fresh
```

## Nhom bang chinh

### Tai khoan

- `users`: tai khoan, username, phone, avatar, role, status, ngay sinh, lan dang nhap cuoi.
- `roles`, `permissions`, `role_user`, `permission_role`: RBAC de phan quyen noi bo va ben thu ba.
- `password_reset_tokens`, `sessions`: bang mac dinh cua Laravel.

### Danh muc phim

- `movies`: phim le, series, short; slug, nam/ngay phat hanh, poster, backdrop, trailer, trang thai publish, rating tong hop, luot xem.
- `movies.content_provider_id`, `movies.rights_status`: gan phim voi doi tac noi dung va trang thai phap ly.
- `seasons`, `episodes`: cau truc series theo mua va tap.
- `genres`, `countries`, `languages`, `studios`, `people`, `content_ratings`, `tags`: metadata dung de loc/tim kiem/hien thi.
- Pivot tables: `movie_genre`, `movie_country`, `movie_language`, `movie_studio`, `movie_person`, `movie_tag`.

### Doi tac, upload va phap ly

- `content_providers`: studio/distributor/aggregator/independent co thong tin phap nhan, lien he, trang thai xac minh.
- `content_provider_user`: user thuoc doi tac nao, vai tro tai doi tac (`owner`, `admin`, `uploader`, `legal`, `finance`, `viewer`).
- `content_licenses`: hop dong/quyen khai thac phim, thoi han, loai license, kenh duoc phep phat, trang thai duyet.
- `content_license_country`: vung lanh tho duoc include/exclude theo license.
- `legal_documents`: hop dong, chung nhan ban quyen, giay phep kiem duyet, tai lieu thue/dinh danh.
- `movie_uploads`: batch upload phim/tap/metadata tu doi tac, co workflow submit, transcode, legal review, content review, publish.
- `upload_files`: file master video, trailer, poster, backdrop, subtitle, legal document trong tung batch upload.
- `takedown_requests`: yeu cau go/noi dung do claim ban quyen, het han license, sai vung phat hanh, lenh toa, policy.
- `content_audit_logs`: audit log cho hanh dong upload, duyet, sua license, publish, takedown.

### Phat video va media

- `media_assets`: poster, backdrop, still, thumbnail, trailer gan voi phim hoac tap.
- `video_sources`: HLS, DASH, MP4, external URL; ho tro nhieu quality/CDN.
- `subtitles`: phu de theo ngon ngu, co the gan voi source, phim hoac tap.

### Tuong tac nguoi dung

- `ratings`, `reviews`, `comments`: cham diem, review, comment da cap.
- `watchlist_items`, `favorites`, `watch_histories`: danh sach xem sau, yeu thich, lich su va tien do xem.
- `playlists`, `playlist_items`: bo suu tap phim ca nhan/cong khai.
- `user_notifications`: thong bao rieng cua ung dung, tach khoi convention `notifications` cua Laravel.
- `reports`: bao cao noi dung/comment/review va trang thai xu ly.

### Goi thanh vien va thanh toan

- `plans`: cac goi thanh vien, gia, chu ky, so thiet bi, chat luong toi da, download.
- `subscriptions`: dang ky goi cua user, trial, active, expired, canceled.
- `payment_transactions`: giao dich thanh toan, provider, transaction id, amount, status, payload.

## Nguyen tac thiet ke

- Phim le va series dung chung `movies`; series co them `seasons` va `episodes`.
- Nguon video va subtitle cho phep nullable `movie_id`/`episode_id`, phu hop phim le va tap phim.
- Cac bang noi dung co `status` de ho tro workflow admin/moderation.
- Cac bang user-content dung unique index de tranh trung lap rating, favorite, watchlist.
- `softDeletes` duoc bat cho movie, episode, review, comment de an noi dung ma khong mat lich su.
- Ben thu ba khong duoc cap quyen truc tiep bang `users.role`; quyen thuc te nam trong `roles/permissions` va membership `content_provider_user`.
- Phim chi nen publish khi `movies.status = published`, `movies.rights_status = cleared`, co license `approved` con hieu luc va hop le theo territory.
