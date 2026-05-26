<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\UploadFile;
use Illuminate\Support\Facades\Storage;

class UploadFilePreviewController extends Controller
{
    public function show(UploadFile $uploadFile)
    {
        if (preg_match('/^https?:\/\//i', $uploadFile->path)) {
            return redirect()->away($uploadFile->path);
        }

        $disk = $uploadFile->disk ?: 'private';
        $path = ltrim($uploadFile->path, '/');

        if (! Storage::disk($disk)->exists($path)) {
            return response($this->missingFilePreview($uploadFile), 200, [
                'Content-Type' => 'text/html; charset=UTF-8',
                'Cache-Control' => 'private, max-age=60',
            ]);
        }

        $absolutePath = Storage::disk($disk)->path($path);
        $filename = $uploadFile->original_filename ?: basename($path);

        return response()->file($absolutePath, [
            'Content-Type' => $uploadFile->mime_type ?: 'application/octet-stream',
            'Content-Disposition' => 'inline; filename="'.$filename.'"',
            'Cache-Control' => 'private, max-age=300',
        ]);
    }

    private function missingFilePreview(UploadFile $file): string
    {
        $name = e($file->original_filename ?: basename($file->path));
        $path = e($file->path);
        $type = e($file->file_type);
        $status = e($file->status);

        return <<<HTML
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <title>{$name}</title>
  <style>
    body { margin: 0; background: #0d0f17; color: #f8fafc; font-family: Inter, Arial, sans-serif; }
    main { max-width: 760px; margin: 48px auto; padding: 28px; border: 1px solid rgba(255,255,255,.12); border-radius: 14px; background: #171922; }
    p { color: #cbd5e1; line-height: 1.7; }
    code { display: block; padding: 12px; border-radius: 10px; background: rgba(0,0,0,.32); color: #ffe182; overflow-wrap: anywhere; }
  </style>
</head>
<body>
  <main>
    <p>Preview file upload</p>
    <h1>{$name}</h1>
    <p>File vật lý chưa có trong storage của môi trường này, nên ZMovie hiển thị metadata để admin vẫn kiểm tra được record.</p>
    <p><strong>Loại:</strong> {$type}</p>
    <p><strong>Trạng thái:</strong> {$status}</p>
    <p><strong>Đường dẫn:</strong></p>
    <code>{$path}</code>
  </main>
</body>
</html>
HTML;
    }
}
