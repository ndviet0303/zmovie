<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class ContentRating extends Model
{
    protected $guarded = [];

    public function movies()
    {
        return $this->hasMany(Movie::class);
    }
}
