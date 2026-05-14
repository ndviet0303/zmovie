<?php

namespace Database\Seeders;

use App\Models\Movie;
use App\Models\VideoSource;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Storage;

class LegalDemoVideoSourceSeeder extends Seeder
{
    private const SOURCES = [
        [
            'label' => 'Demo video 5s',
            'source_type' => 'mp4',
            'quality' => '720p',
            'remote_url' => 'https://samplelib.com/lib/preview/mp4/sample-5s.mp4',
            'storage_path' => 'demo-videos/sample-5s.mp4',
        ],
        [
            'label' => 'Demo video 10s',
            'source_type' => 'mp4',
            'quality' => '720p',
            'remote_url' => 'https://samplelib.com/lib/preview/mp4/sample-10s.mp4',
            'storage_path' => 'demo-videos/sample-10s.mp4',
        ],
        [
            'label' => 'Demo video 15s',
            'source_type' => 'mp4',
            'quality' => '720p',
            'remote_url' => 'https://samplelib.com/lib/preview/mp4/sample-15s.mp4',
            'storage_path' => 'demo-videos/sample-15s.mp4',
        ],
    ];

    public function run(): void
    {
        $movies = Movie::query()
            ->where('status', 'published')
            ->where('rights_status', 'cleared')
            ->orderBy('id')
            ->get();

        if ($movies->isEmpty()) {
            $this->command?->warn('No published cleared movies found. Seed movies first.');
            return;
        }

        foreach ($movies as $index => $movie) {
            $source = self::SOURCES[$index % count(self::SOURCES)];
            $fileSize = $this->ensureDemoVideo($source);

            VideoSource::query()
                ->where('movie_id', $movie->id)
                ->update(['is_default' => false]);

            VideoSource::updateOrCreate(
                [
                    'movie_id' => $movie->id,
                    'label' => $source['label'],
                ],
                [
                    'source_type' => $source['source_type'],
                    'quality' => $source['quality'],
                    'url' => $source['storage_path'],
                    'cdn_provider' => 'local-public-storage',
                    'file_size_bytes' => $fileSize,
                    'is_default' => true,
                    'is_active' => true,
                ],
            );

            $this->command?->info("Attached legal demo source to {$movie->title}: {$source['label']}");
        }
    }

    private function ensureDemoVideo(array $source): ?int
    {
        if (Storage::disk('public')->exists($source['storage_path'])) {
            return Storage::disk('public')->size($source['storage_path']);
        }

        $this->command?->info("Downloading {$source['label']}...");

        $response = Http::timeout(90)->retry(2, 1000)->get($source['remote_url']);

        if (! $response->successful()) {
            $response->throw();
        }

        Storage::disk('public')->put($source['storage_path'], $response->body());

        return Storage::disk('public')->size($source['storage_path']);
    }
}
