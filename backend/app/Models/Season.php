<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Season extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'release_date' => 'date',
        ];
    }

    public function movie()
    {
        return $this->belongsTo(Movie::class);
    }

    public function episodes()
    {
        return $this->hasMany(Episode::class);
    }
}
