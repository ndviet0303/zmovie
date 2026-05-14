<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Language extends Model
{
    protected $guarded = [];

    public function movies()
    {
        return $this->belongsToMany(Movie::class, 'movie_language')->withPivot('kind');
    }

    public function subtitles()
    {
        return $this->hasMany(Subtitle::class);
    }
}
