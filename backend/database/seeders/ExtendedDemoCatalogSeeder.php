<?php

namespace Database\Seeders;

use App\Models\Country;
use App\Models\Genre;
use App\Models\Language;
use App\Models\Movie;
use App\Models\Season;
use App\Models\VideoSource;
use Illuminate\Database\Seeder;
use Illuminate\Support\Str;

class ExtendedDemoCatalogSeeder extends Seeder
{
    private const VIDEOS = [
        'demo-videos/sintel-trailer-720p.mp4',
    ];

    private const POSTERS = [
        'https://images.unsplash.com/photo-1485846234645-a62644f84728?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1492691527719-9d1e07e534b4?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1518709268805-4e9042af2176?auto=format&fit=crop&w=520&q=85',
    ];

    private const BACKDROPS = [
        'https://images.unsplash.com/photo-1485846234645-a62644f84728?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1478720568477-152d9b164e26?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1518709911915-712d5fd04677?auto=format&fit=crop&w=1800&q=85',
    ];

    public function run(): void
    {
        $genres = $this->genres();
        $countries = $this->countries();
        $languages = $this->languages();

        foreach ($this->items() as $index => $item) {
            $movie = Movie::updateOrCreate(
                ['slug' => $item['slug']],
                [
                    'title' => $item['title'],
                    'original_title' => $item['original_title'],
                    'type' => $item['episodes'] ? 'series' : 'movie',
                    'status' => 'published',
                    'rights_status' => 'cleared',
                    'overview' => $item['overview'],
                    'release_year' => $item['year'],
                    'runtime_minutes' => $item['runtime'],
                    'poster_path' => self::POSTERS[$index % count(self::POSTERS)],
                    'backdrop_path' => self::BACKDROPS[$index % count(self::BACKDROPS)],
                    'is_featured' => $index < 8,
                    'average_rating' => $item['rating'],
                    'rating_count' => 120 + ($index * 17),
                    'view_count' => 1000 + ($index * 431),
                    'published_at' => now()->subDays($index + 1),
                ],
            );

            $movie->genres()->sync(collect($item['genres'])->map(fn ($slug) => $genres[$slug]->id)->all());
            $movie->countries()->sync([$countries[$item['country']]->id]);
            $movie->languages()->syncWithoutDetaching([
                $languages['vi-sub']->id => ['kind' => 'subtitle'],
            ]);

            if ($item['episodes']) {
                $this->seedEpisodes($movie, $item['episodes'], $index);
            } else {
                $this->seedMovieSource($movie, $index);
            }
        }
    }

    private function genres(): array
    {
        return collect([
            'hanh-dong' => 'Hành động',
            'tam-ly' => 'Tâm lý',
            'hai' => 'Hài',
            'kinh-di' => 'Kinh dị',
            'phieu-luu' => 'Phiêu lưu',
            'tinh-cam' => 'Tình cảm',
            'co-trang' => 'Cổ trang',
            'bi-an' => 'Bí ẩn',
            'anime' => 'Anime',
            'tai-lieu' => 'Tài liệu',
        ])->mapWithKeys(fn ($name, $slug) => [
            $slug => Genre::firstOrCreate(['slug' => $slug], ['name' => $name]),
        ])->all();
    }

    private function countries(): array
    {
        return collect([
            'VN' => 'Việt Nam',
            'US' => 'United States',
            'KR' => 'Hàn Quốc',
            'JP' => 'Nhật Bản',
            'CN' => 'Trung Quốc',
            'TH' => 'Thái Lan',
        ])->mapWithKeys(fn ($name, $code) => [
            $code => Country::firstOrCreate(['code' => $code], ['name' => $name]),
        ])->all();
    }

    private function languages(): array
    {
        return [
            'vi-sub' => Language::firstOrCreate(['code' => 'vi-sub'], ['name' => 'Vietsub']),
        ];
    }

    private function seedMovieSource(Movie $movie, int $index): void
    {
        VideoSource::updateOrCreate(
            ['movie_id' => $movie->id, 'label' => 'Demo stream'],
            [
                'source_type' => 'mp4',
                'quality' => '720p',
                'url' => self::VIDEOS[$index % count(self::VIDEOS)],
                'cdn_provider' => 'local-public-storage',
                'duration_seconds' => ($movie->runtime_minutes ?: 90) * 60,
                'is_default' => true,
                'is_active' => true,
            ],
        );
    }

    private function seedEpisodes(Movie $movie, int $count, int $index): void
    {
        $season = Season::updateOrCreate(
            ['movie_id' => $movie->id, 'season_number' => 1],
            [
                'title' => 'Phần 1',
                'overview' => "Phần đầu tiên của {$movie->title}.",
                'poster_path' => $movie->poster_path,
                'release_date' => now()->subMonths(3)->toDateString(),
            ],
        );

        for ($episodeNumber = 1; $episodeNumber <= $count; $episodeNumber++) {
            $episode = $season->episodes()->updateOrCreate(
                ['episode_number' => $episodeNumber],
                [
                    'title' => "Tập {$episodeNumber}",
                    'slug' => "tap-{$episodeNumber}",
                    'overview' => "{$movie->title} - Tập {$episodeNumber}.",
                    'runtime_minutes' => $movie->runtime_minutes,
                    'still_path' => $movie->backdrop_path,
                    'status' => 'published',
                    'published_at' => now()->subDays($count - $episodeNumber + 1),
                ],
            );

            VideoSource::updateOrCreate(
                ['episode_id' => $episode->id, 'label' => 'Demo episode stream'],
                [
                    'source_type' => 'mp4',
                    'quality' => '720p',
                    'url' => self::VIDEOS[($index + $episodeNumber) % count(self::VIDEOS)],
                    'cdn_provider' => 'local-public-storage',
                    'duration_seconds' => ($movie->runtime_minutes ?: 42) * 60,
                    'is_default' => true,
                    'is_active' => true,
                ],
            );
        }
    }

    private function items(): array
    {
        $titles = [
            ['Biệt Đội Mưa Đêm', 'Night Rain Unit', 'hanh-dong', 'bi-an', 'VN', 2026, 12],
            ['Bản Giao Hưởng Số 0', 'Symphony Zero', 'tam-ly', 'tinh-cam', 'KR', 2025, 0],
            ['Vòng Lặp Tokyo', 'Tokyo Loop', 'anime', 'phieu-luu', 'JP', 2026, 10],
            ['Đường Dây Ngầm', 'The Hidden Line', 'hanh-dong', 'bi-an', 'US', 2024, 0],
            ['Nhà Có Ba Mùa', 'House of Three Seasons', 'hai', 'tinh-cam', 'VN', 2023, 16],
            ['Mật Lệnh Trăng Non', 'New Moon Cipher', 'co-trang', 'bi-an', 'CN', 2025, 24],
            ['Thị Trấn Không Tên', 'Nameless Town', 'kinh-di', 'tam-ly', 'TH', 2024, 8],
            ['Hồ Ký Ức', 'Lake of Memory', 'tai-lieu', 'tam-ly', 'VN', 2022, 0],
            ['Chuyến Tàu 05:17', 'Train 05:17', 'hanh-dong', 'phieu-luu', 'KR', 2025, 0],
            ['Mặt Trời Sau Bão', 'Sun After Storm', 'tinh-cam', 'tam-ly', 'JP', 2021, 12],
            ['Khoảng Trống Orion', 'Orion Gap', 'phieu-luu', 'bi-an', 'US', 2026, 14],
            ['Lời Hẹn Ven Sông', 'Riverside Promise', 'tinh-cam', 'hai', 'VN', 2024, 0],
            ['Đêm Trắng Busan', 'White Night Busan', 'hanh-dong', 'bi-an', 'KR', 2026, 8],
            ['Tầng Hầm 42', 'Basement 42', 'kinh-di', 'bi-an', 'US', 2023, 0],
            ['Ký Sự Hoa Đăng', 'Lantern Records', 'co-trang', 'tam-ly', 'CN', 2022, 20],
            ['Câu Lạc Bộ Thứ Sáu', 'Friday Club', 'hai', 'tinh-cam', 'TH', 2025, 12],
            ['Đảo Bên Kia Mây', 'Island Beyond Clouds', 'phieu-luu', 'tai-lieu', 'VN', 2026, 0],
            ['Mật Vụ Bên Cửa Sổ', 'Window Agent', 'hanh-dong', 'hai', 'JP', 2024, 10],
            ['Những Ngày Không Tên', 'Untitled Days', 'tam-ly', 'tinh-cam', 'KR', 2023, 0],
            ['Thành Phố Sau 0 Giờ', 'After Zero City', 'hanh-dong', 'kinh-di', 'US', 2026, 18],
            ['Bức Thư Không Gửi', 'Unsent Letter', 'tinh-cam', 'tam-ly', 'VN', 2021, 0],
            ['Rạp Chiếu Bỏ Quên', 'The Forgotten Theater', 'bi-an', 'tai-lieu', 'TH', 2022, 6],
            ['Sóng Dưới Lòng Đất', 'Underground Waves', 'phieu-luu', 'hanh-dong', 'CN', 2024, 0],
            ['Vệ Tinh Màu Lam', 'Blue Satellite', 'anime', 'phieu-luu', 'JP', 2025, 13],
        ];

        return collect($titles)->map(function (array $item, int $index) {
            return [
                'title' => $item[0],
                'original_title' => $item[1],
                'slug' => Str::slug($item[1]),
                'genres' => [$item[2], $item[3]],
                'country' => $item[4],
                'year' => $item[5],
                'episodes' => $item[6],
                'runtime' => $item[6] ? 42 : 100 + ($index % 25),
                'rating' => 6.4 + (($index % 16) / 10),
                'overview' => "{$item[0]} là nội dung demo mở rộng của ZMovie, dùng để kiểm thử danh sách phim, chi tiết, phát video và chọn tập.",
            ];
        })->all();
    }
}
