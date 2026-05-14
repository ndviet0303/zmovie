<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Person extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'birthday' => 'date',
        ];
    }

    public function movies()
    {
        return $this->belongsToMany(Movie::class, 'movie_person')->withPivot(['role', 'character_name', 'sort_order']);
    }
}
