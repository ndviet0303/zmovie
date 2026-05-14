<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\SoftDeletes;
use Laravel\Scout\Searchable;

class Movie extends Model
{
    use Searchable, SoftDeletes;

    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'is_featured' => 'boolean',
            'average_rating' => 'decimal:2',
            'release_date' => 'date',
            'published_at' => 'datetime',
        ];
    }

    public function contentProvider()
    {
        return $this->belongsTo(ContentProvider::class);
    }

    public function contentRating()
    {
        return $this->belongsTo(ContentRating::class);
    }

    public function genres()
    {
        return $this->belongsToMany(Genre::class, 'movie_genre');
    }

    public function countries()
    {
        return $this->belongsToMany(Country::class, 'movie_country');
    }

    public function languages()
    {
        return $this->belongsToMany(Language::class, 'movie_language')->withPivot('kind');
    }

    public function studios()
    {
        return $this->belongsToMany(Studio::class)->withPivot('role');
    }

    public function tags()
    {
        return $this->belongsToMany(Tag::class);
    }

    public function people()
    {
        return $this->belongsToMany(Person::class, 'movie_person')->withPivot(['role', 'character_name', 'sort_order']);
    }

    public function seasons()
    {
        return $this->hasMany(Season::class);
    }

    public function videoSources()
    {
        return $this->hasMany(VideoSource::class);
    }

    public function subtitles()
    {
        return $this->hasMany(Subtitle::class);
    }

    public function reviews()
    {
        return $this->hasMany(Review::class);
    }

    public function licenses()
    {
        return $this->hasMany(ContentLicense::class);
    }

    public function legalDocuments()
    {
        return $this->hasMany(LegalDocument::class);
    }

    public function uploads()
    {
        return $this->hasMany(MovieUpload::class);
    }

    public function searchableAs(): string
    {
        return 'movies';
    }

    public function shouldBeSearchable(): bool
    {
        return $this->status === 'published'
            && $this->rights_status === 'cleared'
            && ! $this->trashed();
    }

    public function toSearchableArray(): array
    {
        $this->loadMissing(['genres:id,name,slug', 'countries:id,name,code', 'tags:id,name,slug', 'contentProvider:id,name']);

        return [
            'id' => $this->id,
            'title' => $this->title,
            'original_title' => $this->original_title,
            'slug' => $this->slug,
            'type' => $this->type,
            'status' => $this->status,
            'rights_status' => $this->rights_status,
            'overview' => $this->overview,
            'release_year' => $this->release_year,
            'runtime_minutes' => $this->runtime_minutes,
            'poster_path' => $this->poster_path,
            'backdrop_path' => $this->backdrop_path,
            'average_rating' => (float) $this->average_rating,
            'view_count' => $this->view_count,
            'published_at' => $this->published_at?->toISOString(),
            'published_at_timestamp' => $this->published_at?->timestamp,
            'content_provider_id' => $this->content_provider_id,
            'provider_name' => $this->contentProvider?->name,
            'genre_names' => $this->genres->pluck('name')->values()->all(),
            'genre_slugs' => $this->genres->pluck('slug')->values()->all(),
            'country_names' => $this->countries->pluck('name')->values()->all(),
            'country_codes' => $this->countries->pluck('code')->values()->all(),
            'tag_names' => $this->tags->pluck('name')->values()->all(),
        ];
    }
}
