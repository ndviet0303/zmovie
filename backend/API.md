# ZMovie API

Base URL:

```text
http://127.0.0.1:8000
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
POST /auth/login
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
POST /auth/logout
```

## Public

```http
GET /health
GET /lookups
GET /movies
GET /movies/{id}
GET /search/movies?q=batman&type=movie&sort=latest
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
POST /movies
PUT /movies/{id}
DELETE /movies/{id}
```

Can `movies.publish`:

```http
POST /movies/{id}/publish
```

Publish yeu cau `rights_status = cleared`.

## Content Providers

Can `providers.manage`:

```http
GET /content-providers
POST /content-providers
GET /content-providers/{id}
PUT /content-providers/{id}
DELETE /content-providers/{id}
```

Can `providers.members.manage`:

```http
POST /content-providers/{id}/members
```

## Legal

Can `legal.submit`:

```http
GET /content-licenses
POST /content-licenses
GET /content-licenses/{id}
PUT /content-licenses/{id}
DELETE /content-licenses/{id}

GET /legal-documents
POST /legal-documents
GET /legal-documents/{id}
PUT /legal-documents/{id}
```

Can `licenses.approve`:

```http
POST /content-licenses/{id}/approve
```

## Uploads

Can `uploads.create`:

```http
GET /movie-uploads
POST /movie-uploads
GET /movie-uploads/{id}
PUT /movie-uploads/{id}
DELETE /movie-uploads/{id}
POST /movie-uploads/{id}/submit
```

Can `movies.review`:

```http
POST /movie-uploads/{id}/approve
POST /movie-uploads/{id}/transcode
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
GET /roles
GET /permissions
```
