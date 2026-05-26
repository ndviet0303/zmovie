<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\UploadFile;
use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpFoundation\BinaryFileResponse;

class UploadFilePreviewController extends Controller
{
    public function show(UploadFile $uploadFile): BinaryFileResponse
    {
        if (preg_match('/^https?:\/\//i', $uploadFile->path)) {
            abort(422, 'External upload files cannot be previewed through the local API.');
        }

        $disk = $uploadFile->disk ?: 'private';
        $path = ltrim($uploadFile->path, '/');

        abort_unless(Storage::disk($disk)->exists($path), 404);

        $absolutePath = Storage::disk($disk)->path($path);
        $filename = $uploadFile->original_filename ?: basename($path);

        return response()->file($absolutePath, [
            'Content-Type' => $uploadFile->mime_type ?: 'application/octet-stream',
            'Content-Disposition' => 'inline; filename="'.$filename.'"',
            'Cache-Control' => 'private, max-age=300',
        ]);
    }
}
