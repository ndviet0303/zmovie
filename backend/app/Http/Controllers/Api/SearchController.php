<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Movie;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;

class SearchController extends Controller
{
    public function movies(Request $request)
    {
        $data = $request->validate([
            'q' => ['nullable', 'string', 'max:255'],
            'type' => ['nullable', Rule::in(['movie', 'series', 'short'])],
            'genre' => ['nullable', 'string', 'max:255'],
            'country' => ['nullable', 'string', 'max:3'],
            'release_year' => ['nullable', 'integer', 'min:1888', 'max:2100'],
            'sort' => ['nullable', Rule::in(['relevance', 'latest', 'rating', 'views', 'year'])],
            'per_page' => ['nullable', 'integer', 'min:1', 'max:50'],
        ]);

        $search = Movie::search($data['q'] ?? '')
            ->where('status', 'published')
            ->where('rights_status', 'cleared');

        if (! empty($data['type'])) {
            $search->where('type', $data['type']);
        }

        if (! empty($data['genre'])) {
            $search->where('genre_slugs', $data['genre']);
        }

        if (! empty($data['country'])) {
            $search->where('country_codes', strtoupper($data['country']));
        }

        if (! empty($data['release_year'])) {
            $search->where('release_year', $data['release_year']);
        }

        match ($data['sort'] ?? 'relevance') {
            'latest' => $search->orderBy('published_at_timestamp', 'desc'),
            'rating' => $search->orderBy('average_rating', 'desc'),
            'views' => $search->orderBy('view_count', 'desc'),
            'year' => $search->orderBy('release_year', 'desc'),
            default => null,
        };

        $results = $search
            ->query(fn ($query) => $query->with(['contentProvider:id,name,slug', 'genres:id,name,slug', 'countries:id,name,code']))
            ->paginate($data['per_page'] ?? 15);

        return response()->json($results);
    }
}
