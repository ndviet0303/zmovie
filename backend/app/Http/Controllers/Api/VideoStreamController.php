<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Movie;
use App\Models\VideoSource;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpFoundation\StreamedResponse;

class VideoStreamController extends Controller
{
    public function show(Request $request, VideoSource $videoSource): StreamedResponse
    {
        abort_unless($videoSource->is_active, 404);
        abort_unless($this->canStream($videoSource), 404);
        abort_if(preg_match('/^https?:\/\//i', $videoSource->url), 422, 'External video sources are not streamed locally.');

        $relativePath = preg_replace('/^storage\//', '', ltrim($videoSource->url, '/'));
        abort_unless(Storage::disk('public')->exists($relativePath), 404);

        $path = Storage::disk('public')->path($relativePath);
        $size = filesize($path);
        $start = 0;
        $end = $size - 1;
        $status = 200;

        if ($range = $request->headers->get('Range')) {
            if (preg_match('/bytes=(\d*)-(\d*)/', $range, $matches)) {
                $rangeStart = $matches[1] === '' ? null : (int) $matches[1];
                $rangeEnd = $matches[2] === '' ? null : (int) $matches[2];

                if ($rangeStart === null && $rangeEnd !== null) {
                    $start = max(0, $size - $rangeEnd);
                } elseif ($rangeStart !== null) {
                    $start = $rangeStart;
                    $end = $rangeEnd ?? $end;
                }

                $end = min($end, $size - 1);

                if ($start > $end || $start >= $size) {
                    abort(416);
                }

                $status = 206;
            }
        }

        $length = $end - $start + 1;

        return response()->stream(function () use ($path, $start, $length) {
            $handle = fopen($path, 'rb');
            fseek($handle, $start);

            $remaining = $length;
            while ($remaining > 0 && ! feof($handle)) {
                $chunkSize = min(1024 * 1024, $remaining);
                echo fread($handle, $chunkSize);
                flush();
                $remaining -= $chunkSize;
            }

            fclose($handle);
        }, $status, [
            'Accept-Ranges' => 'bytes',
            'Content-Type' => 'video/mp4',
            'Content-Length' => (string) $length,
            'Content-Range' => "bytes {$start}-{$end}/{$size}",
            'Cache-Control' => 'public, max-age=31536000',
        ]);
    }

    private function canStream(VideoSource $videoSource): bool
    {
        $user = Auth::guard('sanctum')->user();

        if ($user?->hasPermission('movies.manage')) {
            return true;
        }

        $movie = $videoSource->movie
            ?: $videoSource->episode?->season?->movie;

        return $movie instanceof Movie
            && $movie->status === 'published'
            && $movie->rights_status === 'cleared';
    }
}
