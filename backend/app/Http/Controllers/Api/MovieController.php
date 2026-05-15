<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Movie;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class MovieController extends Controller
{
    public function index(Request $request)
    {
        $user = Auth::guard('sanctum')->user();
        $canManageMovies = $user?->hasPermission('movies.manage') ?? false;

        $movies = Movie::query()
            ->with([
                'contentProvider:id,name,slug',
                'genres:id,name,slug',
                'countries:id,name,code',
                'seasons.episodes:id,season_id,title,slug,episode_number,status,published_at',
                'videoSources',
            ])
            ->when(! $canManageMovies, fn ($query) => $query
                ->where('status', 'published')
                ->where('rights_status', 'cleared'))
            ->when($request->query('q'), fn ($query, $q) => $query->where('title', 'like', "%{$q}%"))
            ->when($request->query('type'), fn ($query, $type) => $query->where('type', $type))
            ->when($canManageMovies && $request->query('status'), fn ($query, $status) => $query->where('status', $status))
            ->when($canManageMovies && $request->query('rights_status'), fn ($query, $status) => $query->where('rights_status', $status))
            ->latest('published_at')
            ->latest('id')
            ->paginate((int) $request->query('per_page', 15));

        return response()->json($movies);
    }

    public function store(Request $request)
    {
        $data = $this->validatedMovie($request);

        $movie = DB::transaction(function () use ($data) {
            $movie = Movie::create(collect($data)->except([
                'genre_ids',
                'country_ids',
                'language_ids',
                'tag_ids',
            ])->all());

            $this->syncRelations($movie, $data);

            return $movie;
        });

        return response()->json($movie->load(['genres', 'countries', 'languages', 'tags']), 201);
    }

    public function show(string $movie)
    {
        $user = Auth::guard('sanctum')->user();
        $canManageMovies = $user?->hasPermission('movies.manage') ?? false;

        $movie = Movie::query()
            ->when(ctype_digit($movie), fn ($query) => $query->whereKey((int) $movie))
            ->orWhere('slug', $movie)
            ->firstOrFail();

        abort_unless($canManageMovies || $this->isPubliclyPlayable($movie), 404);

        return response()->json($movie->load([
            'contentProvider',
            'contentRating',
            'genres',
            'countries',
            'languages',
            'studios',
            'tags',
            'people',
            'seasons.episodes.videoSources',
            'videoSources',
            'subtitles',
            'licenses.countries',
            'legalDocuments',
        ]));
    }

    public function update(Request $request, Movie $movie)
    {
        $data = $this->validatedMovie($request, $movie);

        DB::transaction(function () use ($movie, $data) {
            $movie->update(collect($data)->except([
                'genre_ids',
                'country_ids',
                'language_ids',
                'tag_ids',
            ])->all());

            $this->syncRelations($movie, $data);
        });

        $movie->fresh()->searchable();

        return response()->json($movie->fresh()->load(['genres', 'countries', 'languages', 'tags']));
    }

    public function destroy(Movie $movie)
    {
        $movie->delete();

        return response()->noContent();
    }

    public function publish(Movie $movie)
    {
        if ($movie->rights_status !== 'cleared') {
            return response()->json([
                'message' => 'Movie rights must be cleared before publishing.',
            ], 422);
        }

        $movie->update([
            'status' => 'published',
            'published_at' => now(),
        ]);

        $movie->searchable();

        return response()->json($movie->fresh());
    }

    private function validatedMovie(Request $request, ?Movie $movie = null): array
    {
        return $request->validate([
            'content_provider_id' => ['nullable', 'exists:content_providers,id'],
            'content_rating_id' => ['nullable', 'exists:content_ratings,id'],
            'title' => ['required', 'string', 'max:255'],
            'original_title' => ['nullable', 'string', 'max:255'],
            'slug' => ['required', 'string', 'max:255', Rule::unique('movies', 'slug')->ignore($movie)],
            'type' => ['required', Rule::in(['movie', 'series', 'short'])],
            'status' => ['sometimes', Rule::in(['draft', 'published', 'archived'])],
            'rights_status' => ['sometimes', Rule::in(['unknown', 'pending', 'cleared', 'expired', 'disputed', 'blocked'])],
            'overview' => ['nullable', 'string'],
            'release_year' => ['nullable', 'integer', 'min:1888', 'max:2100'],
            'release_date' => ['nullable', 'date'],
            'runtime_minutes' => ['nullable', 'integer', 'min:1'],
            'poster_path' => ['nullable', 'string', 'max:255'],
            'backdrop_path' => ['nullable', 'string', 'max:255'],
            'trailer_url' => ['nullable', 'url', 'max:255'],
            'is_featured' => ['sometimes', 'boolean'],
            'genre_ids' => ['sometimes', 'array'],
            'genre_ids.*' => ['integer', 'exists:genres,id'],
            'country_ids' => ['sometimes', 'array'],
            'country_ids.*' => ['integer', 'exists:countries,id'],
            'language_ids' => ['sometimes', 'array'],
            'language_ids.*' => ['integer', 'exists:languages,id'],
            'tag_ids' => ['sometimes', 'array'],
            'tag_ids.*' => ['integer', 'exists:tags,id'],
        ]);
    }

    private function syncRelations(Movie $movie, array $data): void
    {
        if (array_key_exists('genre_ids', $data)) {
            $movie->genres()->sync($data['genre_ids']);
        }

        if (array_key_exists('country_ids', $data)) {
            $movie->countries()->sync($data['country_ids']);
        }

        if (array_key_exists('language_ids', $data)) {
            $movie->languages()->sync(collect($data['language_ids'])->mapWithKeys(fn ($id) => [$id => ['kind' => 'original']])->all());
        }

        if (array_key_exists('tag_ids', $data)) {
            $movie->tags()->sync($data['tag_ids']);
        }
    }

    private function isPubliclyPlayable(Movie $movie): bool
    {
        return $movie->status === 'published' && $movie->rights_status === 'cleared';
    }
}
