<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->string('username')->unique()->nullable()->after('name');
            $table->string('phone', 30)->nullable()->after('email');
            $table->string('avatar_path')->nullable()->after('password');
            $table->enum('role', ['user', 'moderator', 'admin'])->default('user')->after('avatar_path');
            $table->enum('status', ['active', 'banned', 'pending'])->default('active')->after('role');
            $table->date('date_of_birth')->nullable()->after('status');
            $table->timestamp('last_login_at')->nullable()->after('date_of_birth');
        });
    }

    public function down(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->dropColumn([
                'username',
                'phone',
                'avatar_path',
                'role',
                'status',
                'date_of_birth',
                'last_login_at',
            ]);
        });
    }
};
