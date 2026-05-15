<?php

namespace Tests\Unit;

use App\Support\DemoVideoStorageQuota;
use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpKernel\Exception\HttpException;
use Tests\TestCase;

class DemoVideoStorageQuotaTest extends TestCase
{
    public function test_quota_throws_diskfull_when_limit_is_exceeded(): void
    {
        Storage::fake('private');
        Storage::fake('public');

        config([
            'transcoding.demo_quota.enabled' => true,
            'transcoding.demo_quota.limit_bytes' => 10,
            'transcoding.demo_quota.paths' => ['private:uploads', 'public:hls'],
        ]);

        Storage::disk('private')->put('uploads/movie/master.mp4', '12345678');

        $this->expectException(HttpException::class);
        $this->expectExceptionMessage('diskfull');

        app(DemoVideoStorageQuota::class)->assertHasSpace(3);
    }

    public function test_quota_can_be_disabled(): void
    {
        Storage::fake('private');

        config([
            'transcoding.demo_quota.enabled' => false,
            'transcoding.demo_quota.limit_bytes' => 1,
            'transcoding.demo_quota.paths' => ['private:uploads'],
        ]);

        Storage::disk('private')->put('uploads/movie/master.mp4', '12345678');

        app(DemoVideoStorageQuota::class)->assertHasSpace(100);

        $this->assertTrue(true);
    }
}
