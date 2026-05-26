<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\LegalDocument;
use Illuminate\Support\Facades\Storage;

class LegalDocumentPreviewController extends Controller
{
    public function show(LegalDocument $legalDocument)
    {
        if (preg_match('/^https?:\/\//i', $legalDocument->path)) {
            return redirect()->away($legalDocument->path);
        }

        $disk = $legalDocument->disk ?: 'private';
        $path = ltrim($legalDocument->path, '/');

        if (! Storage::disk($disk)->exists($path)) {
            return response($this->missingDocumentPreview($legalDocument), 200, [
                'Content-Type' => 'text/html; charset=UTF-8',
                'Cache-Control' => 'private, max-age=60',
            ]);
        }

        $absolutePath = Storage::disk($disk)->path($path);
        $filename = $legalDocument->original_filename ?: basename($path);

        return response()->file($absolutePath, [
            'Content-Type' => $legalDocument->mime_type ?: 'application/octet-stream',
            'Content-Disposition' => 'inline; filename="'.$filename.'"',
            'Cache-Control' => 'private, max-age=300',
        ]);
    }

    private function missingDocumentPreview(LegalDocument $document): string
    {
        $title = e($document->title);
        $path = e($document->path);
        $type = e($document->document_type);
        $status = e($document->status);

        return <<<HTML
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <title>{$title}</title>
  <style>
    body { margin: 0; background: #0d0f17; color: #f8fafc; font-family: Inter, Arial, sans-serif; }
    main { max-width: 760px; margin: 48px auto; padding: 28px; border: 1px solid rgba(255,255,255,.12); border-radius: 14px; background: #171922; }
    p { color: #cbd5e1; line-height: 1.7; }
    code { display: block; padding: 12px; border-radius: 10px; background: rgba(0,0,0,.32); color: #ffe182; overflow-wrap: anywhere; }
  </style>
</head>
<body>
  <main>
    <p>Preview hồ sơ pháp lý</p>
    <h1>{$title}</h1>
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
