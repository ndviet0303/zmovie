<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('movies', function (Blueprint $table) {
            $table->id();
            $table->foreignId('content_rating_id')->nullable()->constrained()->nullOnDelete();
            $table->string('title');
            $table->string('original_title')->nullable();
            $table->string('slug')->unique();
            $table->enum('type', ['movie', 'series', 'short'])->default('movie');
            $table->enum('status', ['draft', 'published', 'archived'])->default('draft');
            $table->text('overview')->nullable();
            $table->unsignedSmallInteger('release_year')->nullable();
            $table->date('release_date')->nullable();
            $table->unsignedSmallInteger('runtime_minutes')->nullable();
            $table->string('poster_path')->nullable();
            $table->string('backdrop_path')->nullable();
            $table->string('trailer_url')->nullable();
            $table->boolean('is_featured')->default(false);
            $table->decimal('average_rating', 3, 2)->default(0);
            $table->unsignedInteger('rating_count')->default(0);
            $table->unsignedBigInteger('view_count')->default(0);
            $table->timestamp('published_at')->nullable();
            $table->timestamps();
            $table->softDeletes();

            $table->index(['status', 'published_at']);
            $table->index(['type', 'release_year']);
        });

        Schema::create('movie_genre', function (Blueprint $table) {
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('genre_id')->constrained()->cascadeOnDelete();
            $table->primary(['movie_id', 'genre_id']);
        });

        Schema::create('movie_country', function (Blueprint $table) {
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('country_id')->constrained()->cascadeOnDelete();
            $table->primary(['movie_id', 'country_id']);
        });

        Schema::create('movie_language', function (Blueprint $table) {
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('language_id')->constrained()->cascadeOnDelete();
            $table->enum('kind', ['original', 'dubbed', 'subtitle'])->default('original');
            $table->primary(['movie_id', 'language_id', 'kind']);
        });

        Schema::create('movie_studio', function (Blueprint $table) {
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('studio_id')->constrained()->cascadeOnDelete();
            $table->enum('role', ['production', 'distribution'])->default('production');
            $table->primary(['movie_id', 'studio_id', 'role']);
        });

        Schema::create('movie_tag', function (Blueprint $table) {
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('tag_id')->constrained()->cascadeOnDelete();
            $table->primary(['movie_id', 'tag_id']);
        });

        Schema::create('movie_person', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->foreignId('person_id')->constrained('people')->cascadeOnDelete();
            $table->enum('role', ['actor', 'director', 'writer', 'producer']);
            $table->string('character_name')->nullable();
            $table->unsignedSmallInteger('sort_order')->default(0);
            $table->unique(['movie_id', 'person_id', 'role', 'character_name']);
        });

        Schema::create('seasons', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_id')->constrained()->cascadeOnDelete();
            $table->unsignedSmallInteger('season_number');
            $table->string('title')->nullable();
            $table->text('overview')->nullable();
            $table->string('poster_path')->nullable();
            $table->date('release_date')->nullable();
            $table->timestamps();
            $table->unique(['movie_id', 'season_number']);
        });

        Schema::create('episodes', function (Blueprint $table) {
            $table->id();
            $table->foreignId('season_id')->constrained()->cascadeOnDelete();
            $table->string('title');
            $table->string('slug');
            $table->unsignedSmallInteger('episode_number');
            $table->text('overview')->nullable();
            $table->unsignedSmallInteger('runtime_minutes')->nullable();
            $table->string('still_path')->nullable();
            $table->enum('status', ['draft', 'published', 'archived'])->default('draft');
            $table->timestamp('published_at')->nullable();
            $table->timestamps();
            $table->softDeletes();
            $table->unique(['season_id', 'episode_number']);
            $table->unique(['season_id', 'slug']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('episodes');
        Schema::dropIfExists('seasons');
        Schema::dropIfExists('movie_person');
        Schema::dropIfExists('movie_tag');
        Schema::dropIfExists('movie_studio');
        Schema::dropIfExists('movie_language');
        Schema::dropIfExists('movie_country');
        Schema::dropIfExists('movie_genre');
        Schema::dropIfExists('movies');
    }
};
