<?php

namespace Database\Seeders;

use App\Models\ContentProvider;
use App\Models\Permission;
use App\Models\Role;
use App\Models\User;
use Illuminate\Database\Console\Seeds\WithoutModelEvents;
use Illuminate\Database\Seeder;

class DatabaseSeeder extends Seeder
{
    use WithoutModelEvents;

    /**
     * Seed the application's database.
     */
    public function run(): void
    {
        $permissions = collect([
            ['name' => 'Manage users', 'slug' => 'users.manage', 'group' => 'users'],
            ['name' => 'Manage roles', 'slug' => 'roles.manage', 'group' => 'users'],
            ['name' => 'Manage movies', 'slug' => 'movies.manage', 'group' => 'movies'],
            ['name' => 'Review movies', 'slug' => 'movies.review', 'group' => 'movies'],
            ['name' => 'Publish movies', 'slug' => 'movies.publish', 'group' => 'movies'],
            ['name' => 'Upload movies', 'slug' => 'uploads.create', 'group' => 'uploads'],
            ['name' => 'Manage provider uploads', 'slug' => 'uploads.manage', 'group' => 'uploads'],
            ['name' => 'View upload status', 'slug' => 'uploads.view', 'group' => 'uploads'],
            ['name' => 'Submit legal documents', 'slug' => 'legal.submit', 'group' => 'legal'],
            ['name' => 'Review legal rights', 'slug' => 'legal.review', 'group' => 'legal'],
            ['name' => 'Approve content licenses', 'slug' => 'licenses.approve', 'group' => 'legal'],
            ['name' => 'Manage content providers', 'slug' => 'providers.manage', 'group' => 'providers'],
            ['name' => 'Manage provider members', 'slug' => 'providers.members.manage', 'group' => 'providers'],
            ['name' => 'View provider reports', 'slug' => 'providers.reports.view', 'group' => 'providers'],
            ['name' => 'Manage payments', 'slug' => 'billing.manage', 'group' => 'billing'],
            ['name' => 'Handle takedowns', 'slug' => 'takedowns.manage', 'group' => 'legal'],
        ])->mapWithKeys(fn (array $permission) => [
            $permission['slug'] => Permission::create($permission),
        ]);

        $roles = [
            'super-admin' => [
                'name' => 'Super Admin',
                'permissions' => $permissions->keys()->all(),
            ],
            'content-admin' => [
                'name' => 'Content Admin',
                'permissions' => ['movies.manage', 'movies.review', 'movies.publish', 'uploads.manage', 'providers.manage', 'providers.reports.view'],
            ],
            'legal-reviewer' => [
                'name' => 'Legal Reviewer',
                'permissions' => ['legal.review', 'licenses.approve', 'takedowns.manage', 'providers.reports.view'],
            ],
            'provider-owner' => [
                'name' => 'Provider Owner',
                'permissions' => ['uploads.create', 'uploads.view', 'legal.submit', 'providers.members.manage', 'providers.reports.view'],
            ],
            'provider-uploader' => [
                'name' => 'Provider Uploader',
                'permissions' => ['uploads.create', 'uploads.view'],
            ],
            'provider-legal' => [
                'name' => 'Provider Legal',
                'permissions' => ['legal.submit', 'uploads.view'],
            ],
            'provider-viewer' => [
                'name' => 'Provider Viewer',
                'permissions' => ['uploads.view', 'providers.reports.view'],
            ],
        ];

        $createdRoles = collect($roles)->mapWithKeys(function (array $role, string $slug) use ($permissions) {
            $createdRole = Role::create([
                'name' => $role['name'],
                'slug' => $slug,
                'is_system' => true,
            ]);

            $createdRole->permissions()->sync(
                collect($role['permissions'])->map(fn (string $permission) => $permissions[$permission]->id)->all()
            );

            return [$slug => $createdRole];
        });

        $admin = User::factory()->create([
            'name' => 'Admin',
            'email' => 'admin@zmovie.local',
            'role' => 'admin',
        ]);

        $providerOwner = User::factory()->create([
            'name' => 'Provider Owner',
            'email' => 'provider@zmovie.local',
        ]);

        User::factory()->create([
            'name' => 'Test User',
            'email' => 'test@example.com',
        ]);

        $admin->roles()->attach($createdRoles['super-admin']->id);
        $providerOwner->roles()->attach($createdRoles['provider-owner']->id);

        $provider = ContentProvider::create([
            'name' => 'Demo Content Partner',
            'slug' => 'demo-content-partner',
            'legal_name' => 'Demo Content Partner Ltd.',
            'contact_name' => 'Provider Owner',
            'contact_email' => 'provider@zmovie.local',
            'type' => 'distributor',
            'verification_status' => 'verified',
            'verified_by' => $admin->id,
            'verified_at' => now(),
        ]);

        $provider->users()->attach($providerOwner->id, [
            'provider_role' => 'owner',
            'status' => 'active',
            'joined_at' => now(),
        ]);

        $this->call([
            DemoAccountSeeder::class,
            DemoCatalogSeeder::class,
        ]);
    }
}
