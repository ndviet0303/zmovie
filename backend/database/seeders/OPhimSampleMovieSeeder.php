<?php

namespace Database\Seeders;

use App\Models\Country;
use App\Models\Genre;
use App\Models\Language;
use App\Models\Movie;
use App\Models\VideoSource;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Storage;
use Illuminate\Support\Str;

class OPhimSampleMovieSeeder extends Seeder
{
    private const API_BASE_URL = 'https://ophim1.com/v1/api/phim';

    private const SAMPLE_SLUGS = [
        'lat-mat-6-tam-ve-dinh-menh',
        'lat-mat-2015',
    ];

    private const DEMO_VIDEO_URL = 'demo-videos/sintel-trailer-720p.mp4';

    public function run(): void
    {
        foreach (self::SAMPLE_SLUGS as $slug) {
            $payload = Http::timeout(15)
                ->acceptJson()
                ->get(self::API_BASE_URL.'/'.$slug)
                ->throw()
                ->json();

            $item = data_get($payload, 'data.item');
            $imageBaseUrl = $this->normalizeImageBaseUrl(
                data_get($payload, 'data.APP_DOMAIN_CDN_IMAGE', 'https://img.ophim.live/uploads/movies')
            );

            if (! $item) {
                $this->command?->warn("OPhim item not found: {$slug}");
                continue;
            }

            DB::transaction(function () use ($item, $imageBaseUrl) {
                $posterPath = $this->downloadImage(
                    $this->imageUrl($imageBaseUrl, $item['poster_url'] ?? null),
                    $item['slug'],
                    'poster',
                );
                $backdropPath = $this->downloadImage(
                    $this->imageUrl($imageBaseUrl, $item['thumb_url'] ?? null),
                    $item['slug'],
                    'backdrop',
                );

                $movie = Movie::updateOrCreate(
                    ['slug' => $item['slug']],
                    [
                        'title' => $item['name'],
                        'original_title' => $item['origin_name'] ?? $item['name'],
                        'type' => $this->normalizeType($item['type'] ?? null),
                        'status' => 'published',
                        'rights_status' => 'cleared',
                        'overview' => $this->cleanOverview($item['content'] ?? null),
                        'release_year' => $item['year'] ?? null,
                        'runtime_minutes' => $this->parseRuntime($item['time'] ?? null),
                        'poster_path' => $posterPath,
                        'backdrop_path' => $backdropPath,
                        'trailer_url' => $this->validUrl($item['trailer_url'] ?? null),
                        'is_featured' => true,
                        'average_rating' => (float) data_get($item, 'tmdb.vote_average', data_get($item, 'imdb.vote_average', 0)),
                        'rating_count' => (int) data_get($item, 'tmdb.vote_count', data_get($item, 'imdb.vote_count', 0)),
                        'view_count' => (int) ($item['view'] ?? 0),
                        'published_at' => now(),
                    ],
                );

                $movie->genres()->sync($this->genreIds($item['category'] ?? []));
                $movie->countries()->sync($this->countryIds($item['country'] ?? []));

                $language = $this->languageFromOPhim($item['lang'] ?? null, $item['lang_key'] ?? []);
                if ($language) {
                    $movie->languages()->syncWithoutDetaching([
                        $language->id => ['kind' => $this->languageKind($item['lang'] ?? null)],
                    ]);
                }

                VideoSource::updateOrCreate(
                    [
                        'movie_id' => $movie->id,
                        'label' => 'Demo legal sample',
                    ],
                    [
                        'source_type' => 'mp4',
                        'quality' => '720p',
                        'url' => self::DEMO_VIDEO_URL,
                        'cdn_provider' => 'local-public-storage',
                        'is_default' => true,
                        'is_active' => true,
                    ],
                );

                $this->command?->info("Seeded OPhim metadata: {$movie->title}");
            });
        }
    }

    private function normalizeType(?string $type): string
    {
        return match ($type) {
            'series', 'hoathinh', 'tvshows' => 'series',
            default => 'movie',
        };
    }

    private function cleanOverview(?string $content): ?string
    {
        if (! $content) {
            return null;
        }

        return trim(html_entity_decode(strip_tags($content)));
    }

    private function parseRuntime(?string $time): ?int
    {
        if (! $time) {
            return null;
        }

        if (preg_match('/(\d+)\s*h(?:\s*(\d+)\s*m)?/i', $time, $matches)) {
            return ((int) $matches[1] * 60) + (int) ($matches[2] ?? 0);
        }

        if (preg_match('/(\d+)/', $time, $matches)) {
            return (int) $matches[1];
        }

        return null;
    }

    private function imageUrl(string $baseUrl, ?string $path): ?string
    {
        if (! $path) {
            return null;
        }

        if (Str::startsWith($path, ['http://', 'https://'])) {
            return $path;
        }

        return $baseUrl.'/'.ltrim($path, '/');
    }

    private function downloadImage(?string $url, string $movieSlug, string $role): ?string
    {
        if (! $url) {
            return null;
        }

        $extension = pathinfo(parse_url($url, PHP_URL_PATH) ?? '', PATHINFO_EXTENSION) ?: 'jpg';
        $path = "ophim/{$movieSlug}/{$role}.{$extension}";

        if (Storage::disk('public')->exists($path)) {
            return $path;
        }

        $response = Http::timeout(20)->get($url)->throw();
        Storage::disk('public')->put($path, $response->body());

        return $path;
    }

    private function normalizeImageBaseUrl(string $baseUrl): string
    {
        $baseUrl = rtrim($baseUrl, '/');

        if (! Str::contains($baseUrl, '/uploads/movies')) {
            return $baseUrl.'/uploads/movies';
        }

        return $baseUrl;
    }

    private function validUrl(?string $url): ?string
    {
        if (! $url || ! filter_var($url, FILTER_VALIDATE_URL)) {
            return null;
        }

        return $url;
    }

    /**
     * @param  array<int, array{name?: string, slug?: string}>  $categories
     * @return array<int>
     */
    private function genreIds(array $categories): array
    {
        return collect($categories)
            ->filter(fn (array $category) => ! empty($category['name']))
            ->map(function (array $category) {
                return Genre::firstOrCreate(
                    ['slug' => $category['slug'] ?? Str::slug($category['name'])],
                    ['name' => $category['name']],
                )->id;
            })
            ->values()
            ->all();
    }

    /**
     * @param  array<int, array{name?: string, slug?: string}>  $countries
     * @return array<int>
     */
    private function countryIds(array $countries): array
    {
        return collect($countries)
            ->filter(fn (array $country) => ! empty($country['name']))
            ->map(function (array $country) {
                $code = $this->countryCode($country['slug'] ?? $country['name']);

                return Country::firstOrCreate(
                    ['code' => $code],
                    ['name' => $country['name']],
                )->id;
            })
            ->values()
            ->all();
    }

    private function countryCode(string $country): string
    {
        return match (Str::slug($country)) {
            'viet-nam' => 'VN',
            'han-quoc' => 'KR',
            'trung-quoc' => 'CN',
            'nhat-ban' => 'JP',
            'thai-lan' => 'TH',
            'au-my', 'my' => 'US',
            default => strtoupper(substr(Str::slug($country, ''), 0, 3)),
        };
    }

    /**
     * @param  array<int, string>  $langKeys
     */
    private function languageFromOPhim(?string $label, array $langKeys): ?Language
    {
        $label = $label ?: 'Vietsub';
        $code = in_array('lt', $langKeys, true) || Str::contains(Str::lower($label), 'lồng')
            ? 'vi-dub'
            : 'vi-sub';

        return Language::firstOrCreate(
            ['code' => $code],
            ['name' => $label],
        );
    }

    private function languageKind(?string $label): string
    {
        return Str::contains(Str::lower($label ?? ''), 'lồng') ? 'dubbed' : 'subtitle';
    }
}
