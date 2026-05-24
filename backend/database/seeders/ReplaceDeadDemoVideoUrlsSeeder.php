<?php

namespace Database\Seeders;

use App\Models\VideoSource;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Storage;

class ReplaceDeadDemoVideoUrlsSeeder extends Seeder
{
    private const TRAILER_URL = 'https://download.blender.org/durian/trailer/sintel_trailer-720p.mp4';
    private const STORAGE_PATH = 'demo-videos/sintel-trailer-720p.mp4';

    public function run(): void
    {
        $fileSize = $this->ensureTrailer();

        $updated = VideoSource::query()
            ->where('url', 'like', '%commondatastorage.googleapis.com/gtv-videos-bucket/sample/%')
            ->update([
                'url' => self::STORAGE_PATH,
                'cdn_provider' => 'local-public-storage',
                'source_type' => 'mp4',
                'file_size_bytes' => $fileSize,
                'is_active' => true,
            ]);

        $this->command?->info("Replaced {$updated} dead demo video source URL(s).");
    }

    private function ensureTrailer(): ?int
    {
        if (Storage::disk('public')->exists(self::STORAGE_PATH)) {
            return Storage::disk('public')->size(self::STORAGE_PATH);
        }

        $this->command?->info('Downloading Sintel trailer...');

        $response = Http::timeout(120)->retry(2, 1000)->get(self::TRAILER_URL);

        if (! $response->successful()) {
            $response->throw();
        }

        Storage::disk('public')->put(self::STORAGE_PATH, $response->body());

        return Storage::disk('public')->size(self::STORAGE_PATH);
    }
}
