<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('media_assets', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('episode_id')->nullable()->constrained()->cascadeOnDelete();
            $table->enum('type', ['poster', 'backdrop', 'still', 'trailer', 'thumbnail']);
            $table->string('disk')->default('public');
            $table->string('path');
            $table->string('mime_type', 100)->nullable();
            $table->unsignedInteger('width')->nullable();
            $table->unsignedInteger('height')->nullable();
            $table->unsignedSmallInteger('sort_order')->default(0);
            $table->timestamps();
            $table->index(['movie_id', 'type']);
            $table->index(['episode_id', 'type']);
        });

        Schema::create('video_sources', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('episode_id')->nullable()->constrained()->cascadeOnDelete();
            $table->enum('source_type', ['hls', 'dash', 'mp4', 'external']);
            $table->string('quality', 20)->default('auto');
            $table->string('label')->nullable();
            $table->string('url');
            $table->string('cdn_provider')->nullable();
            $table->unsignedBigInteger('file_size_bytes')->nullable();
            $table->unsignedSmallInteger('duration_seconds')->nullable();
            $table->boolean('is_default')->default(false);
            $table->boolean('is_active')->default(true);
            $table->timestamps();
            $table->index(['movie_id', 'is_active']);
            $table->index(['episode_id', 'is_active']);
        });

        Schema::create('subtitles', function (Blueprint $table) {
            $table->id();
            $table->foreignId('video_source_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('movie_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('episode_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('language_id')->constrained()->cascadeOnDelete();
            $table->string('label')->nullable();
            $table->string('url');
            $table->enum('format', ['vtt', 'srt', 'ass'])->default('vtt');
            $table->boolean('is_default')->default(false);
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('subtitles');
        Schema::dropIfExists('video_sources');
        Schema::dropIfExists('media_assets');
    }
};
