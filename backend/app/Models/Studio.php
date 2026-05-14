<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Studio extends Model
{
    protected $guarded = [];

    public function movies()
    {
        return $this->belongsToMany(Movie::class)->withPivot('role');
    }
}
