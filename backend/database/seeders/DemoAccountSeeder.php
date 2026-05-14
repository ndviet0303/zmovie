<?php

namespace Database\Seeders;

use App\Models\ContentProvider;
use App\Models\Role;
use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DemoAccountSeeder extends Seeder
{
    private const PASSWORD = 'password';

    private const ACCOUNTS = [
        [
            'name' => 'Super Admin',
            'email' => 'admin@zmovie.local',
            'role' => 'super-admin',
            'label' => 'Super Admin',
            'description' => 'Toàn quyền hệ thống, roles, phim, pháp lý, provider.',
        ],
        [
            'name' => 'Content Admin',
            'email' => 'content@zmovie.local',
            'role' => 'content-admin',
            'label' => 'Content Admin',
            'description' => 'Quản lý, duyệt và publish phim.',
        ],
        [
            'name' => 'Legal Reviewer',
            'email' => 'legal@zmovie.local',
            'role' => 'legal-reviewer',
            'label' => 'Legal Reviewer',
            'description' => 'Duyệt giấy tờ bản quyền và license.',
        ],
        [
            'name' => 'Provider Owner',
            'email' => 'provider@zmovie.local',
            'role' => 'provider-owner',
            'label' => 'Provider Owner',
            'description' => 'Chủ đối tác nội dung, upload và quản lý thành viên provider.',
        ],
        [
            'name' => 'Provider Uploader',
            'email' => 'uploader@zmovie.local',
            'role' => 'provider-uploader',
            'label' => 'Provider Uploader',
            'description' => 'Upload phim và theo dõi trạng thái upload.',
        ],
        [
            'name' => 'Provider Legal',
            'email' => 'provider-legal@zmovie.local',
            'role' => 'provider-legal',
            'label' => 'Provider Legal',
            'description' => 'Nộp giấy tờ bản quyền cho provider.',
        ],
        [
            'name' => 'Provider Viewer',
            'email' => 'viewer@zmovie.local',
            'role' => 'provider-viewer',
            'label' => 'Provider Viewer',
            'description' => 'Chỉ xem trạng thái upload và báo cáo provider.',
        ],
    ];

    public function run(): void
    {
        $provider = ContentProvider::query()->firstOrCreate(
            ['slug' => 'demo-content-partner'],
            [
                'name' => 'Demo Content Partner',
                'legal_name' => 'Demo Content Partner Ltd.',
                'contact_name' => 'Provider Owner',
                'contact_email' => 'provider@zmovie.local',
                'type' => 'distributor',
                'verification_status' => 'verified',
                'verified_at' => now(),
            ],
        );

        foreach (self::ACCOUNTS as $account) {
            $role = Role::query()->where('slug', $account['role'])->first();

            if (! $role) {
                $this->command?->warn("Missing role: {$account['role']}. Run DatabaseSeeder first.");
                continue;
            }

            $user = User::query()->updateOrCreate(
                ['email' => $account['email']],
                [
                    'name' => $account['name'],
                    'password' => Hash::make(self::PASSWORD),
                    'role' => str_contains($account['role'], 'admin') ? 'admin' : 'user',
                    'status' => 'active',
                    'email_verified_at' => now(),
                ],
            );

            $user->roles()->syncWithoutDetaching([$role->id]);

            if (str_starts_with($account['role'], 'provider-')) {
                $providerRole = match ($account['role']) {
                    'provider-owner' => 'owner',
                    'provider-uploader' => 'uploader',
                    'provider-legal' => 'legal',
                    default => 'viewer',
                };

                $provider->users()->syncWithoutDetaching([
                    $user->id => [
                        'provider_role' => $providerRole,
                        'status' => 'active',
                        'joined_at' => now(),
                    ],
                ]);
            }

            $this->command?->info("Seeded {$account['label']}: {$account['email']} / ".self::PASSWORD);
        }
    }

    public static function accounts(): array
    {
        return collect(self::ACCOUNTS)
            ->map(fn (array $account) => [
                'name' => $account['name'],
                'email' => $account['email'],
                'password' => self::PASSWORD,
                'role' => $account['role'],
                'label' => $account['label'],
                'description' => $account['description'],
            ])
            ->all();
    }
}
