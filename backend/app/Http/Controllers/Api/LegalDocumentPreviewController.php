<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\LegalDocument;
use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpFoundation\BinaryFileResponse;

class LegalDocumentPreviewController extends Controller
{
    public function show(LegalDocument $legalDocument): BinaryFileResponse
    {
        if (preg_match('/^https?:\/\//i', $legalDocument->path)) {
            abort(422, 'External legal documents cannot be previewed through the local API.');
        }

        $disk = $legalDocument->disk ?: 'private';
        $path = ltrim($legalDocument->path, '/');

        abort_unless(Storage::disk($disk)->exists($path), 404);

        $absolutePath = Storage::disk($disk)->path($path);
        $filename = $legalDocument->original_filename ?: basename($path);

        return response()->file($absolutePath, [
            'Content-Type' => $legalDocument->mime_type ?: 'application/octet-stream',
            'Content-Disposition' => 'inline; filename="'.$filename.'"',
            'Cache-Control' => 'private, max-age=300',
        ]);
    }
}
