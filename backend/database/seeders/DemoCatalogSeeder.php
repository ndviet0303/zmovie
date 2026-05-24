<?php

namespace Database\Seeders;

use App\Models\ContentLicense;
use App\Models\ContentProvider;
use App\Models\ContentRating;
use App\Models\Country;
use App\Models\Favorite;
use App\Models\Genre;
use App\Models\Language;
use App\Models\LegalDocument;
use App\Models\MediaAsset;
use App\Models\Movie;
use App\Models\MovieUpload;
use App\Models\PaymentTransaction;
use App\Models\Person;
use App\Models\Plan;
use App\Models\Playlist;
use App\Models\Rating;
use App\Models\Review;
use App\Models\Role;
use App\Models\Season;
use App\Models\Studio;
use App\Models\Subscription;
use App\Models\Subtitle;
use App\Models\Tag;
use App\Models\TakedownRequest;
use App\Models\User;
use App\Models\UserNotification;
use App\Models\VideoSource;
use App\Models\WatchHistory;
use App\Models\WatchlistItem;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

class DemoCatalogSeeder extends Seeder
{
    private const DEMO_VIDEO_URL = 'demo-videos/sintel-trailer-720p.mp4';

    private const BACKDROP_URLS = [
        'https://images.unsplash.com/photo-1485846234645-a62644f84728?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1478720568477-152d9b164e26?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?auto=format&fit=crop&w=1800&q=85',
        'https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=1800&q=85',
    ];

    private const POSTER_URLS = [
        'https://images.unsplash.com/photo-1608889825205-eebdb9fc5806?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1616530940355-351fabd9524b?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1581905764498-f1b60bae941a?auto=format&fit=crop&w=520&q=85',
        'https://images.unsplash.com/photo-1594909122845-11baa439b7bf?auto=format&fit=crop&w=520&q=85',
    ];

    public function run(): void
    {
        DB::transaction(function () {
            $genres = $this->seedGenres();
            $countries = $this->seedCountries();
            $languages = $this->seedLanguages();
            $ratings = $this->seedContentRatings();
            $tags = $this->seedTags();
            $studios = $this->seedStudios();
            $people = $this->seedPeople();
            $providers = $this->seedProviders();
            $users = $this->seedAudienceUsers();
            $plans = $this->seedPlans();

            $movies = $this->seedMovies($genres, $countries, $languages, $ratings, $tags, $studios, $people, $providers);
            $this->seedLegalWorkflow($movies, $providers);
            $this->seedUploadWorkflow($movies, $providers);
            $this->seedSubscriptions($users, $plans);
            $this->seedEngagement($users, $movies);
        });
    }

    private function seedGenres(): array
    {
        return collect([
            ['Hành động', 'hanh-dong'],
            ['Tâm lý', 'tam-ly'],
            ['Khoa học viễn tưởng', 'khoa-hoc-vien-tuong'],
            ['Hài', 'hai'],
            ['Kinh dị', 'kinh-di'],
            ['Gia đình', 'gia-dinh'],
            ['Phiêu lưu', 'phieu-luu'],
            ['Tài liệu', 'tai-lieu'],
            ['Hoạt hình', 'hoat-hinh'],
            ['Tội phạm', 'toi-pham'],
        ])->mapWithKeys(fn (array $genre) => [
            $genre[1] => Genre::updateOrCreate(
                ['slug' => $genre[1]],
                ['name' => $genre[0], 'description' => "Demo catalog genre: {$genre[0]}."],
            ),
        ])->all();
    }

    private function seedCountries(): array
    {
        return collect([
            ['VN', 'Việt Nam'],
            ['US', 'United States'],
            ['KR', 'Hàn Quốc'],
            ['JP', 'Nhật Bản'],
            ['TH', 'Thái Lan'],
            ['FR', 'Pháp'],
        ])->mapWithKeys(fn (array $country) => [
            $country[0] => Country::updateOrCreate(['code' => $country[0]], ['name' => $country[1]]),
        ])->all();
    }

    private function seedLanguages(): array
    {
        return collect([
            ['vi-sub', 'Vietsub'],
            ['vi-dub', 'Lồng tiếng Việt'],
            ['en', 'English'],
            ['ko', 'Korean'],
            ['ja', 'Japanese'],
            ['th', 'Thai'],
        ])->mapWithKeys(fn (array $language) => [
            $language[0] => Language::updateOrCreate(['code' => $language[0]], ['name' => $language[1]]),
        ])->all();
    }

    private function seedContentRatings(): array
    {
        return collect([
            ['P', 'Phổ biến', null],
            ['K', 'Khuyến nghị có người lớn đi kèm', 13],
            ['T16', 'Từ 16 tuổi trở lên', 16],
            ['T18', 'Từ 18 tuổi trở lên', 18],
        ])->mapWithKeys(fn (array $rating) => [
            $rating[0] => ContentRating::updateOrCreate(
                ['code' => $rating[0]],
                ['name' => $rating[1], 'minimum_age' => $rating[2]],
            ),
        ])->all();
    }

    private function seedTags(): array
    {
        return collect(['Hot', 'Original', 'Độc quyền', 'Gia đình', 'Cuối tuần', 'Oscar', 'K-drama', 'Anime'])
            ->mapWithKeys(fn (string $tag) => [
                Str::slug($tag) => Tag::updateOrCreate(['slug' => Str::slug($tag)], ['name' => $tag]),
            ])->all();
    }

    private function seedStudios(): array
    {
        return collect([
            'zmovie-originals' => 'ZMovie Originals',
            'saigon-frame' => 'Saigon Frame Studio',
            'han-river' => 'Han River Pictures',
            'neon-lotus' => 'Neon Lotus Animation',
        ])->mapWithKeys(fn (string $name, string $slug) => [
            $slug => Studio::updateOrCreate(
                ['slug' => $slug],
                ['name' => $name, 'description' => "{$name} demo studio."],
            ),
        ])->all();
    }

    private function seedPeople(): array
    {
        return collect([
            'an-nhien' => 'An Nhiên',
            'minh-khoi' => 'Minh Khôi',
            'linh-dan' => 'Linh Đan',
            'quoc-bao' => 'Quốc Bảo',
            'maya-lee' => 'Maya Lee',
            'kai-nakamura' => 'Kai Nakamura',
            'nara-kim' => 'Nara Kim',
            'thana-wong' => 'Thana Wong',
        ])->mapWithKeys(fn (string $name, string $slug) => [
            $slug => Person::updateOrCreate(
                ['slug' => $slug],
                ['name' => $name, 'biography' => "{$name} là nhân vật demo dùng để kiểm thử cast và crew."],
            ),
        ])->all();
    }

    private function seedProviders(): array
    {
        $admin = User::where('email', 'admin@zmovie.local')->first();

        return collect([
            'demo-content-partner' => ['Demo Content Partner', 'distributor', 'VN', 'verified'],
            'zmovie-studio' => ['ZMovie Studio', 'internal', 'VN', 'verified'],
            'riverlight-media' => ['Riverlight Media', 'aggregator', 'KR', 'verified'],
            'indie-asia-lab' => ['Indie Asia Lab', 'independent', 'TH', 'pending'],
        ])->mapWithKeys(function (array $provider, string $slug) use ($admin) {
            return [$slug => ContentProvider::updateOrCreate(
                ['slug' => $slug],
                [
                    'name' => $provider[0],
                    'legal_name' => "{$provider[0]} Ltd.",
                    'country_code' => $provider[2],
                    'contact_name' => "{$provider[0]} Ops",
                    'contact_email' => "{$slug}@zmovie.local",
                    'contact_phone' => '+84000000000',
                    'type' => $provider[1],
                    'verification_status' => $provider[3],
                    'verified_by' => $provider[3] === 'verified' ? $admin?->id : null,
                    'verified_at' => $provider[3] === 'verified' ? now()->subDays(12) : null,
                    'settings' => ['demo' => true, 'default_rights_window_months' => 18],
                ],
            )];
        })->all();
    }

    private function seedAudienceUsers(): array
    {
        $users = collect([
            ['Lan Anh', 'lan.anh@example.com'],
            ['Hoàng Nam', 'hoang.nam@example.com'],
            ['Minh Tú', 'minh.tu@example.com'],
            ['Gia Hân', 'gia.han@example.com'],
        ])->mapWithKeys(fn (array $user) => [
            $user[1] => User::updateOrCreate(
                ['email' => $user[1]],
                [
                    'name' => $user[0],
                    'password' => Hash::make('password'),
                    'role' => 'user',
                    'status' => 'active',
                    'email_verified_at' => now(),
                ],
            ),
        ]);

        $role = Role::where('slug', 'provider-uploader')->first();
        $provider = ContentProvider::where('slug', 'riverlight-media')->first();
        $uploader = User::where('email', 'uploader@zmovie.local')->first();

        if ($role && $provider && $uploader) {
            $uploader->roles()->syncWithoutDetaching([$role->id]);
            $provider->users()->syncWithoutDetaching([
                $uploader->id => ['provider_role' => 'uploader', 'status' => 'active', 'joined_at' => now()->subDays(9)],
            ]);
        }

        return $users->all();
    }

    private function seedPlans(): array
    {
        return collect([
            'free' => ['Free', 0, 1, 720, false],
            'standard' => ['Standard', 59000, 2, 1080, false],
            'premium' => ['Premium', 99000, 4, 2160, true],
        ])->mapWithKeys(fn (array $plan, string $slug) => [
            $slug => Plan::updateOrCreate(
                ['slug' => $slug],
                [
                    'name' => $plan[0],
                    'description' => "Gói {$plan[0]} demo cho kiểm thử subscription.",
                    'price_cents' => $plan[1] * 100,
                    'currency' => 'VND',
                    'billing_cycle' => 'monthly',
                    'max_devices' => $plan[2],
                    'max_quality' => $plan[3],
                    'allow_downloads' => $plan[4],
                    'is_active' => true,
                ],
            ),
        ])->all();
    }

    private function seedMovies(array $genres, array $countries, array $languages, array $ratings, array $tags, array $studios, array $people, array $providers): array
    {
        $movies = [
            [
                'title' => 'Đêm Sài Gòn Không Ngủ',
                'original_title' => 'Sleepless Saigon',
                'slug' => 'dem-sai-gon-khong-ngu',
                'type' => 'movie',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['hanh-dong', 'toi-pham'],
                'countries' => ['VN'],
                'languages' => ['vi-sub', 'vi-dub'],
                'tags' => ['hot', 'original'],
                'studio' => 'saigon-frame',
                'provider' => 'zmovie-studio',
                'rating' => 'T16',
                'year' => 2026,
                'runtime' => 112,
                'featured' => true,
            ],
            [
                'title' => 'Trạm Sao Băng',
                'original_title' => 'Meteor Station',
                'slug' => 'tram-sao-bang',
                'type' => 'series',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['khoa-hoc-vien-tuong', 'phieu-luu'],
                'countries' => ['US'],
                'languages' => ['en', 'vi-sub'],
                'tags' => ['doc-quyen', 'cuoi-tuan'],
                'studio' => 'zmovie-originals',
                'provider' => 'zmovie-studio',
                'rating' => 'K',
                'year' => 2025,
                'runtime' => 48,
                'featured' => true,
                'episodes' => 6,
            ],
            [
                'title' => 'Bếp Nhà Mình',
                'original_title' => 'Our Little Kitchen',
                'slug' => 'bep-nha-minh',
                'type' => 'series',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['gia-dinh', 'hai'],
                'countries' => ['VN'],
                'languages' => ['vi-dub'],
                'tags' => ['gia-dinh', 'cuoi-tuan'],
                'studio' => 'saigon-frame',
                'provider' => 'demo-content-partner',
                'rating' => 'P',
                'year' => 2024,
                'runtime' => 42,
                'episodes' => 8,
            ],
            [
                'title' => 'Án Mạng Sông Hàn',
                'original_title' => 'Han River Case',
                'slug' => 'an-mang-song-han',
                'type' => 'movie',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['toi-pham', 'tam-ly'],
                'countries' => ['KR'],
                'languages' => ['ko', 'vi-sub'],
                'tags' => ['k-drama'],
                'studio' => 'han-river',
                'provider' => 'riverlight-media',
                'rating' => 'T18',
                'year' => 2023,
                'runtime' => 118,
            ],
            [
                'title' => 'Mùa Hè Của Mây',
                'original_title' => 'Cloudy Summer',
                'slug' => 'mua-he-cua-may',
                'type' => 'movie',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['tam-ly', 'gia-dinh'],
                'countries' => ['VN'],
                'languages' => ['vi-sub'],
                'tags' => ['oscar'],
                'studio' => 'saigon-frame',
                'provider' => 'demo-content-partner',
                'rating' => 'K',
                'year' => 2022,
                'runtime' => 96,
            ],
            [
                'title' => 'Rừng Neon',
                'original_title' => 'Neon Forest',
                'slug' => 'rung-neon',
                'type' => 'series',
                'status' => 'published',
                'rights_status' => 'cleared',
                'genres' => ['hoat-hinh', 'phieu-luu'],
                'countries' => ['JP'],
                'languages' => ['ja', 'vi-sub'],
                'tags' => ['anime', 'doc-quyen'],
                'studio' => 'neon-lotus',
                'provider' => 'riverlight-media',
                'rating' => 'P',
                'year' => 2026,
                'runtime' => 24,
                'episodes' => 10,
            ],
            [
                'title' => 'Hồ Sơ Chưa Đóng',
                'original_title' => 'Open File',
                'slug' => 'ho-so-chua-dong',
                'type' => 'movie',
                'status' => 'draft',
                'rights_status' => 'pending',
                'genres' => ['tai-lieu', 'toi-pham'],
                'countries' => ['FR', 'VN'],
                'languages' => ['vi-sub'],
                'tags' => ['hot'],
                'studio' => 'zmovie-originals',
                'provider' => 'indie-asia-lab',
                'rating' => 'T16',
                'year' => 2026,
                'runtime' => 90,
            ],
            [
                'title' => 'Căn Hộ Tầng 13',
                'original_title' => 'Apartment 13',
                'slug' => 'can-ho-tang-13',
                'type' => 'movie',
                'status' => 'archived',
                'rights_status' => 'expired',
                'genres' => ['kinh-di'],
                'countries' => ['TH'],
                'languages' => ['th', 'vi-sub'],
                'tags' => ['cuoi-tuan'],
                'studio' => 'neon-lotus',
                'provider' => 'indie-asia-lab',
                'rating' => 'T18',
                'year' => 2021,
                'runtime' => 101,
            ],
        ];

        return collect($movies)->mapWithKeys(function (array $item, int $index) use ($genres, $countries, $languages, $ratings, $tags, $studios, $people, $providers) {
            $movie = Movie::updateOrCreate(
                ['slug' => $item['slug']],
                [
                    'content_provider_id' => $providers[$item['provider']]->id,
                    'content_rating_id' => $ratings[$item['rating']]->id,
                    'title' => $item['title'],
                    'original_title' => $item['original_title'],
                    'type' => $item['type'],
                    'status' => $item['status'],
                    'rights_status' => $item['rights_status'],
                    'overview' => $this->overview($item),
                    'release_year' => $item['year'],
                    'runtime_minutes' => $item['runtime'],
                    'poster_path' => self::POSTER_URLS[$index % count(self::POSTER_URLS)],
                    'backdrop_path' => self::BACKDROP_URLS[$index % count(self::BACKDROP_URLS)],
                    'trailer_url' => self::DEMO_VIDEO_URL,
                    'is_featured' => $item['featured'] ?? false,
                    'average_rating' => round(6.8 + ($index % 5) * 0.4, 2),
                    'rating_count' => 120 + ($index * 37),
                    'view_count' => 2500 + ($index * 825),
                    'published_at' => $item['status'] === 'published' ? now()->subDays($index + 1) : null,
                ],
            );

            $movie->genres()->sync(collect($item['genres'])->map(fn (string $slug) => $genres[$slug]->id));
            $movie->countries()->sync(collect($item['countries'])->map(fn (string $code) => $countries[$code]->id));
            $movie->languages()->sync(collect($item['languages'])->mapWithKeys(fn (string $code) => [
                $languages[$code]->id => ['kind' => Str::contains($code, 'dub') ? 'dubbed' : ($code === 'vi-sub' ? 'subtitle' : 'original')],
            ]));
            $movie->tags()->sync(collect($item['tags'])->map(fn (string $slug) => $tags[$slug]->id));
            $movie->studios()->sync([$studios[$item['studio']]->id => ['role' => 'production']]);
            $movie->people()->sync($this->peopleSyncPayload($people, $index));

            $this->seedMedia($movie);
            $this->seedVideo($movie, $languages);

            if (($item['episodes'] ?? 0) > 0) {
                $this->seedEpisodes($movie, $item['episodes'], $languages);
            }

            return [$item['slug'] => $movie];
        })->all();
    }

    private function peopleSyncPayload(array $people, int $index): array
    {
        $ids = array_values($people);

        return [
            $ids[$index % count($ids)]->id => ['role' => 'director', 'character_name' => null, 'sort_order' => 1],
            $ids[($index + 1) % count($ids)]->id => ['role' => 'actor', 'character_name' => 'Lead', 'sort_order' => 2],
            $ids[($index + 2) % count($ids)]->id => ['role' => 'actor', 'character_name' => 'Support', 'sort_order' => 3],
        ];
    }

    private function seedMedia(Movie $movie): void
    {
        MediaAsset::updateOrCreate(
            ['movie_id' => $movie->id, 'type' => 'poster', 'sort_order' => 1],
            ['disk' => 'public', 'path' => $movie->poster_path, 'mime_type' => 'image/jpeg', 'width' => 520, 'height' => 780],
        );

        MediaAsset::updateOrCreate(
            ['movie_id' => $movie->id, 'type' => 'backdrop', 'sort_order' => 1],
            ['disk' => 'public', 'path' => $movie->backdrop_path, 'mime_type' => 'image/jpeg', 'width' => 1800, 'height' => 1012],
        );
    }

    private function seedVideo(Movie $movie, array $languages): VideoSource
    {
        $source = VideoSource::updateOrCreate(
            ['movie_id' => $movie->id, 'label' => 'Demo stream 720p'],
            [
                'source_type' => 'mp4',
                'quality' => '720p',
                'url' => self::DEMO_VIDEO_URL,
                'cdn_provider' => 'local-public-storage',
                'duration_seconds' => ($movie->runtime_minutes ?: 90) * 60,
                'is_default' => true,
                'is_active' => $movie->status === 'published',
            ],
        );

        Subtitle::updateOrCreate(
            ['video_source_id' => $source->id, 'language_id' => $languages['vi-sub']->id],
            [
                'movie_id' => $movie->id,
                'label' => 'Tiếng Việt',
                'url' => "subtitles/demo/{$movie->slug}.vi.vtt",
                'format' => 'vtt',
                'is_default' => true,
            ],
        );

        return $source;
    }

    private function seedEpisodes(Movie $movie, int $episodeCount, array $languages): void
    {
        $season = Season::updateOrCreate(
            ['movie_id' => $movie->id, 'season_number' => 1],
            [
                'title' => 'Season 1',
                'overview' => "Mùa đầu tiên của {$movie->title}.",
                'poster_path' => $movie->poster_path,
                'release_date' => now()->subMonths(2)->toDateString(),
            ],
        );

        for ($episodeNumber = 1; $episodeNumber <= $episodeCount; $episodeNumber++) {
            $episode = $season->episodes()->updateOrCreate(
                ['episode_number' => $episodeNumber],
                [
                    'title' => "Tập {$episodeNumber}",
                    'slug' => "tap-{$episodeNumber}",
                    'overview' => "Tập {$episodeNumber} của {$movie->title}.",
                    'runtime_minutes' => $movie->runtime_minutes,
                    'still_path' => $movie->backdrop_path,
                    'status' => 'published',
                    'published_at' => now()->subDays($episodeCount - $episodeNumber + 1),
                ],
            );

            $source = VideoSource::updateOrCreate(
                ['episode_id' => $episode->id, 'label' => 'Demo episode stream'],
                [
                    'source_type' => 'mp4',
                    'quality' => '720p',
                    'url' => self::DEMO_VIDEO_URL,
                    'cdn_provider' => 'local-public-storage',
                    'duration_seconds' => ($movie->runtime_minutes ?: 40) * 60,
                    'is_default' => true,
                    'is_active' => true,
                ],
            );

            Subtitle::updateOrCreate(
                ['video_source_id' => $source->id, 'language_id' => $languages['vi-sub']->id],
                [
                    'episode_id' => $episode->id,
                    'label' => 'Tiếng Việt',
                    'url' => "subtitles/demo/{$movie->slug}-{$episode->slug}.vi.vtt",
                    'format' => 'vtt',
                    'is_default' => true,
                ],
            );
        }
    }

    private function seedLegalWorkflow(array $movies, array $providers): void
    {
        $legalUser = User::where('email', 'legal@zmovie.local')->first();
        $providerLegal = User::where('email', 'provider-legal@zmovie.local')->first();

        foreach (array_values($movies) as $index => $movie) {
            $license = ContentLicense::updateOrCreate(
                ['contract_number' => 'DEMO-'.strtoupper($movie->slug)],
                [
                    'content_provider_id' => $movie->content_provider_id,
                    'movie_id' => $movie->id,
                    'licensor_name' => $movie->contentProvider?->legal_name ?: 'Demo Licensor',
                    'license_type' => $movie->rights_status === 'cleared' ? 'non_exclusive' : 'owned',
                    'status' => $movie->rights_status === 'cleared' ? 'approved' : 'pending_review',
                    'rights' => ['streaming' => true, 'download' => $index % 3 === 0],
                    'valid_from' => now()->subMonths(8)->toDateString(),
                    'valid_until' => now()->addMonths($movie->rights_status === 'expired' ? -1 : 18)->toDateString(),
                    'allows_streaming' => true,
                    'allows_download' => $index % 3 === 0,
                    'allows_ads' => true,
                    'allows_subscription' => true,
                    'allows_free_access' => $index % 2 === 0,
                    'territory_mode' => 'worldwide',
                    'reviewed_by' => $movie->rights_status === 'cleared' ? $legalUser?->id : null,
                    'approved_at' => $movie->rights_status === 'cleared' ? now()->subDays($index + 3) : null,
                    'review_note' => $movie->rights_status === 'cleared' ? 'Demo approved rights package.' : null,
                ],
            );

            LegalDocument::updateOrCreate(
                ['content_license_id' => $license->id, 'document_type' => 'contract'],
                [
                    'content_provider_id' => $movie->content_provider_id,
                    'movie_id' => $movie->id,
                    'uploaded_by' => $providerLegal?->id,
                    'status' => $license->status === 'approved' ? 'verified' : 'pending',
                    'title' => "Hợp đồng {$movie->title}",
                    'disk' => 'private',
                    'path' => "legal/demo/{$movie->slug}-contract.pdf",
                    'original_filename' => "{$movie->slug}-contract.pdf",
                    'mime_type' => 'application/pdf',
                    'file_size_bytes' => 256000 + ($index * 2048),
                    'checksum_sha256' => hash('sha256', "demo-contract-{$movie->slug}"),
                    'issued_at' => now()->subMonths(9)->toDateString(),
                    'expires_at' => $license->valid_until,
                    'verified_by' => $license->status === 'approved' ? $legalUser?->id : null,
                    'verified_at' => $license->status === 'approved' ? now()->subDays($index + 2) : null,
                    'review_note' => $license->status === 'approved' ? 'Tài liệu demo hợp lệ.' : null,
                ],
            );
        }

        TakedownRequest::updateOrCreate(
            ['movie_id' => $movies['can-ho-tang-13']->id, 'reason' => 'license_expired'],
            [
                'content_provider_id' => $providers['indie-asia-lab']->id,
                'requested_by' => $providerLegal?->id,
                'status' => 'reviewing',
                'claimant_name' => 'Indie Asia Lab',
                'claimant_email' => 'legal@indie-asia.example',
                'legal_basis' => 'License window expired in demo data.',
                'description' => 'Yêu cầu ẩn phim khỏi public catalog.',
            ],
        );
    }

    private function seedUploadWorkflow(array $movies, array $providers): void
    {
        $uploader = User::where('email', 'uploader@zmovie.local')->first()
            ?: User::where('email', 'provider@zmovie.local')->first();
        $reviewer = User::where('email', 'content@zmovie.local')->first();

        $uploads = [
            ['slug' => 'ho-so-chua-dong', 'status' => 'legal_review', 'type' => 'new_movie'],
            ['slug' => 'tram-sao-bang', 'status' => 'approved', 'type' => 'metadata_update'],
            ['slug' => 'rung-neon', 'status' => 'transcoding', 'type' => 'new_episode'],
        ];

        foreach ($uploads as $index => $item) {
            $movie = $movies[$item['slug']];
            $upload = MovieUpload::updateOrCreate(
                ['title' => "Upload {$movie->title}", 'content_provider_id' => $movie->content_provider_id],
                [
                    'movie_id' => $movie->id,
                    'uploaded_by' => $uploader?->id,
                    'reviewed_by' => $item['status'] === 'approved' ? $reviewer?->id : null,
                    'upload_type' => $item['type'],
                    'status' => $item['status'],
                    'metadata' => ['source' => 'demo-catalog', 'priority' => $index + 1],
                    'submitted_at' => now()->subDays($index + 4),
                    'reviewed_at' => $item['status'] === 'approved' ? now()->subDays($index + 1) : null,
                    'published_at' => null,
                ],
            );

            $upload->files()->updateOrCreate(
                ['file_type' => 'master_video'],
                [
                    'movie_id' => $movie->id,
                    'status' => $item['status'] === 'transcoding' ? 'processing' : 'uploaded',
                    'disk' => 'private',
                    'path' => "uploads/demo/{$movie->slug}/master.mp4",
                    'original_filename' => "{$movie->slug}-master.mp4",
                    'mime_type' => 'video/mp4',
                    'file_size_bytes' => 1_200_000_000 + ($index * 100_000_000),
                    'checksum_sha256' => hash('sha256', "demo-master-{$movie->slug}"),
                    'quality' => '1080p',
                    'duration_seconds' => ($movie->runtime_minutes ?: 90) * 60,
                    'technical_metadata' => ['codec' => 'h264', 'audio' => 'aac'],
                ],
            );

            $upload->files()->updateOrCreate(
                ['file_type' => 'poster'],
                [
                    'movie_id' => $movie->id,
                    'status' => 'ready',
                    'disk' => 'public',
                    'path' => $movie->poster_path,
                    'original_filename' => "{$movie->slug}-poster.jpg",
                    'mime_type' => 'image/jpeg',
                    'file_size_bytes' => 480000,
                    'checksum_sha256' => hash('sha256', "demo-poster-{$movie->slug}"),
                ],
            );
        }
    }

    private function seedSubscriptions(array $users, array $plans): void
    {
        foreach (array_values($users) as $index => $user) {
            $plan = $index % 2 === 0 ? $plans['premium'] : $plans['standard'];
            $subscription = Subscription::updateOrCreate(
                ['user_id' => $user->id, 'plan_id' => $plan->id],
                [
                    'status' => $index === 3 ? 'trialing' : 'active',
                    'trial_ends_at' => $index === 3 ? now()->addDays(10) : null,
                    'starts_at' => now()->subDays(20 - $index),
                    'ends_at' => now()->addMonth(),
                    'canceled_at' => null,
                ],
            );

            PaymentTransaction::updateOrCreate(
                ['provider_transaction_id' => "demo-payment-{$user->id}"],
                [
                    'user_id' => $user->id,
                    'subscription_id' => $subscription->id,
                    'provider' => 'manual',
                    'amount_cents' => $plan->price_cents,
                    'currency' => 'VND',
                    'status' => $index === 3 ? 'pending' : 'paid',
                    'payload' => ['demo' => true],
                    'paid_at' => $index === 3 ? null : now()->subDays(2),
                ],
            );
        }
    }

    private function seedEngagement(array $users, array $movies): void
    {
        $published = collect($movies)->filter(fn (Movie $movie) => $movie->status === 'published')->values();

        foreach (array_values($users) as $userIndex => $user) {
            $playlist = Playlist::updateOrCreate(
                ['user_id' => $user->id, 'slug' => 'cuoi-tuan-cua-toi'],
                [
                    'name' => 'Cuối tuần của tôi',
                    'description' => 'Playlist demo cho trải nghiệm cá nhân hóa.',
                    'is_public' => $userIndex % 2 === 0,
                ],
            );

            $published->take(4)->each(function (Movie $movie, int $movieIndex) use ($user, $userIndex, $playlist) {
                WatchlistItem::updateOrCreate(
                    ['user_id' => $user->id, 'movie_id' => $movie->id],
                    ['status' => ['planned', 'watching', 'completed'][$movieIndex % 3]],
                );

                if (($movieIndex + $userIndex) % 2 === 0) {
                    Favorite::updateOrCreate(['user_id' => $user->id, 'movie_id' => $movie->id]);
                }

                Rating::updateOrCreate(
                    ['user_id' => $user->id, 'movie_id' => $movie->id],
                    ['score' => 7 + (($movieIndex + $userIndex) % 4)],
                );

                Review::updateOrCreate(
                    ['user_id' => $user->id, 'movie_id' => $movie->id],
                    [
                        'title' => 'Ấn tượng tốt',
                        'body' => "Review demo của {$user->name} cho {$movie->title}.",
                        'status' => $movieIndex % 3 === 0 ? 'pending' : 'approved',
                    ],
                );

                WatchHistory::updateOrCreate(
                    ['user_id' => $user->id, 'movie_id' => $movie->id, 'episode_id' => null],
                    [
                        'position_seconds' => 1200 + ($movieIndex * 300),
                        'duration_seconds' => ($movie->runtime_minutes ?: 90) * 60,
                        'progress_percent' => min(95, 25 + ($movieIndex * 18) + ($userIndex * 5)),
                        'last_watched_at' => now()->subHours($movieIndex + $userIndex + 1),
                    ],
                );

                $playlist->items()->updateOrCreate(
                    ['movie_id' => $movie->id],
                    ['sort_order' => $movieIndex + 1],
                );
            });

            UserNotification::updateOrCreate(
                ['user_id' => $user->id, 'type' => 'demo.weekly_digest'],
                [
                    'title' => 'Phim mới hợp gu của bạn',
                    'body' => 'ZMovie vừa thêm một số phim demo để bạn kiểm thử recommendation.',
                    'data' => ['demo' => true],
                    'read_at' => $userIndex % 2 === 0 ? now()->subDay() : null,
                ],
            );
        }
    }

    private function overview(array $item): string
    {
        return "{$item['title']} là phim demo thuộc nhóm ".implode(', ', $item['genres']).'. '
            .'Dữ liệu này được tạo nội bộ để kiểm thử catalog, phân quyền, bản quyền, upload và trải nghiệm người dùng.';
    }
}
