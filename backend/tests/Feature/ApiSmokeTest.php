<?php

namespace Tests\Feature;

use App\Models\ContentLicense;
use App\Models\ContentProvider;
use App\Models\Movie;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Laravel\Sanctum\Sanctum;
use Tests\TestCase;

class ApiSmokeTest extends TestCase
{
    use RefreshDatabase;

    public function test_admin_can_create_clear_and_publish_movie(): void
    {
        $this->seed();
        $admin = User::where('email', 'admin@zmovie.local')->firstOrFail();
        Sanctum::actingAs($admin);

        $movieId = $this->postJson('/api/v1/movies', [
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

        $licenseId = $this->postJson('/api/v1/content-licenses', [
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

        $this->postJson("/api/v1/content-licenses/{$licenseId}/approve", [
                'review_note' => 'ok',
            ])
            ->assertOk();

        $this->postJson("/api/v1/movies/{$movieId}/publish")
            ->assertOk()
            ->assertJsonPath('status', 'published')
            ->assertJsonPath('rights_status', 'cleared');

        $this->assertSame('approved', ContentLicense::find($licenseId)->status);
        $this->assertSame('published', Movie::find($movieId)->status);
    }

    public function test_provider_owner_can_create_movie_upload(): void
    {
        $this->seed();
        $providerOwner = User::where('email', 'provider@zmovie.local')->firstOrFail();
        $providerId = ContentProvider::where('slug', 'demo-content-partner')->value('id');
        Sanctum::actingAs($providerOwner);

        $this->postJson('/api/v1/movie-uploads', [
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
            ->assertJsonPath('uploaded_by', $providerOwner->id)
            ->assertJsonCount(1, 'files');
    }

    public function test_login_returns_bearer_token_and_me_requires_it(): void
    {
        $this->seed();

        $this->getJson('/api/v1/auth/me')->assertUnauthorized();

        $token = $this->postJson('/api/v1/auth/login', [
            'email' => 'admin@zmovie.local',
            'password' => 'password',
        ])
            ->assertOk()
            ->assertJsonPath('token_type', 'Bearer')
            ->assertJsonPath('user.email', 'admin@zmovie.local')
            ->json('access_token');

        $this->withToken($token)
            ->getJson('/api/v1/auth/me')
            ->assertOk()
            ->assertJsonPath('user.email', 'admin@zmovie.local');
    }

    public function test_public_movie_catalog_hides_drafts_even_when_filtered(): void
    {
        $this->seed();

        Movie::create([
            'title' => 'Public Movie',
            'slug' => 'public-movie',
            'status' => 'published',
            'rights_status' => 'cleared',
        ]);

        Movie::create([
            'title' => 'Draft Movie',
            'slug' => 'draft-movie',
            'status' => 'draft',
            'rights_status' => 'cleared',
        ]);

        $this->getJson('/api/v1/movies?status=draft')
            ->assertOk()
            ->assertJsonMissing(['slug' => 'draft-movie'])
            ->assertJsonFragment(['slug' => 'public-movie']);
    }
}
