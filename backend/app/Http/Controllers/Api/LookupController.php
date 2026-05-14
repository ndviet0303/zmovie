<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\ContentRating;
use App\Models\Country;
use App\Models\Genre;
use App\Models\Language;
use App\Models\Tag;

class LookupController extends Controller
{
    public function index()
    {
        return response()->json([
            'genres' => Genre::orderBy('name')->get(),
            'countries' => Country::orderBy('name')->get(),
            'languages' => Language::orderBy('name')->get(),
            'content_ratings' => ContentRating::orderBy('minimum_age')->get(),
            'tags' => Tag::orderBy('name')->get(),
        ]);
    }
}
