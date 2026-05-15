# ZMovie API

Base URL:

```text
http://127.0.0.1:8000/api/v1
```

Auth:

```text
Authorization: Bearer <access_token>
```

Trong seed mac dinh:

- `admin@zmovie.local` co role `super-admin`.
- `provider@zmovie.local` co role `provider-owner`.

Lay token bang endpoint:

```http
POST /api/v1/auth/login
```

Body:

```json
{
  "email": "admin@zmovie.local",
  "password": "password"
}
```

Dang xuat va thu hoi token hien tai:

```http
POST /api/v1/auth/logout
```

## Public

```http
GET /api/health
GET /api/v1/lookups
GET /api/v1/movies
GET /api/v1/movies/{id}
GET /api/v1/search/movies?q=batman&type=movie&sort=latest
```

Search phim dung Meilisearch/Laravel Scout. Chi phim `published` va `rights_status = cleared` moi duoc index va tra ve.

Query params:

- `q`: keyword.
- `type`: `movie`, `series`, `short`.
- `genre`: genre slug.
- `country`: country code.
- `release_year`: nam phat hanh.
- `sort`: `relevance`, `latest`, `rating`, `views`, `year`.
- `per_page`: 1-50.

## Movies

Can `movies.manage`:

```http
POST /api/v1/movies
PUT /api/v1/movies/{id}
DELETE /api/v1/movies/{id}
```

Can `movies.publish`:

```http
POST /api/v1/movies/{id}/publish
```

Publish yeu cau `rights_status = cleared`.

## Content Providers

Can `providers.manage`:

```http
GET /api/v1/content-providers
POST /api/v1/content-providers
GET /api/v1/content-providers/{id}
PUT /api/v1/content-providers/{id}
DELETE /api/v1/content-providers/{id}
```

Can `providers.members.manage`:

```http
POST /api/v1/content-providers/{id}/members
```

## Legal

Can `legal.submit`:

```http
GET /api/v1/content-licenses
POST /api/v1/content-licenses
GET /api/v1/content-licenses/{id}
PUT /api/v1/content-licenses/{id}
DELETE /api/v1/content-licenses/{id}

GET /api/v1/legal-documents
POST /api/v1/legal-documents
GET /api/v1/legal-documents/{id}
PUT /api/v1/legal-documents/{id}
```

Can `licenses.approve`:

```http
POST /api/v1/content-licenses/{id}/approve
```

## Uploads

Can `uploads.create`:

```http
GET /api/v1/movie-uploads
POST /api/v1/movie-uploads
GET /api/v1/movie-uploads/{id}
PUT /api/v1/movie-uploads/{id}
DELETE /api/v1/movie-uploads/{id}
POST /api/v1/movie-uploads/{id}/submit
```

Can `movies.review`:

```http
POST /api/v1/movie-uploads/{id}/approve
POST /api/v1/movie-uploads/{id}/transcode
```

Khi upload có file `master_video` và được gắn với `movie_id`, endpoint approve/transcode sẽ dispatch job `TranscodeUploadFileToHls`. Worker cần nghe queue `transcoding`:

```bash
php artisan queue:work --queue=transcoding,default
```

Môi trường demo giới hạn dung lượng video mặc định 1GB trên `private:uploads,public:hls`. Khi vượt quota API/job sẽ báo `diskfull` với HTTP 507. Có thể tắt hoặc đổi limit bằng:

```env
DEMO_VIDEO_STORAGE_QUOTA_ENABLED=true
DEMO_VIDEO_STORAGE_QUOTA_BYTES=1073741824
DEMO_VIDEO_STORAGE_QUOTA_PATHS=private:uploads,public:hls
```

## RBAC

Can `roles.manage`:

```http
GET /api/v1/roles
GET /api/v1/permissions
```
