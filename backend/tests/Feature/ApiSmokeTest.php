<?php

namespace Tests\Feature;

use App\Models\ContentLicense;
use App\Models\ContentProvider;
use App\Models\Movie;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class ApiSmokeTest extends TestCase
{
    use RefreshDatabase;

    public function test_admin_can_create_clear_and_publish_movie(): void
    {
        $this->seed();

        $movieId = $this->withHeader('X-User-Id', '1')
            ->postJson('/api/v1/movies', [
                'content_provider_id' => 1,
                'title' => 'API Smoke Movie',
                'slug' => 'api-smoke-movie',
                'type' => 'movie',
                'status' => 'draft',
                'rights_status' => 'pending',
                'release_year' => 2026,
            ])
            ->assertCreated()
            ->json('id');

        $licenseId = $this->withHeader('X-User-Id', '1')
            ->postJson('/api/v1/content-licenses', [
                'content_provider_id' => 1,
                'movie_id' => $movieId,
                'licensor_name' => 'Smoke Licensor',
                'license_type' => 'non_exclusive',
                'status' => 'pending_review',
                'valid_from' => '2026-01-01',
                'valid_until' => '2028-01-01',
            ])
            ->assertCreated()
            ->json('id');

        $this->withHeader('X-User-Id', '1')
            ->postJson("/api/v1/content-licenses/{$licenseId}/approve", [
                'review_note' => 'ok',
            ])
            ->assertOk();

        $this->withHeader('X-User-Id', '1')
            ->postJson("/api/v1/movies/{$movieId}/publish")
            ->assertOk()
            ->assertJsonPath('status', 'published')
            ->assertJsonPath('rights_status', 'cleared');

        $this->assertSame('approved', ContentLicense::find($licenseId)->status);
        $this->assertSame('published', Movie::find($movieId)->status);
    }

    public function test_provider_owner_can_create_movie_upload(): void
    {
        $this->seed();
        $providerOwnerId = User::where('email', 'provider@zmovie.local')->value('id');
        $providerId = ContentProvider::where('slug', 'demo-content-partner')->value('id');

        $this->withHeader('X-User-Id', (string) $providerOwnerId)
            ->postJson('/api/v1/movie-uploads', [
                'content_provider_id' => $providerId,
                'title' => 'Provider Upload',
                'upload_type' => 'new_movie',
                'files' => [
                    [
                        'file_type' => 'master_video',
                        'status' => 'uploaded',
                        'disk' => 'private',
                        'path' => 'uploads/provider/master.mp4',
                        'mime_type' => 'video/mp4',
                    ],
                ],
            ])
            ->assertCreated()
            ->assertJsonPath('uploaded_by', $providerOwnerId)
            ->assertJsonCount(1, 'files');
    }
}
