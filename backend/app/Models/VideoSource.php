<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class VideoSource extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'is_default' => 'boolean',
            'is_active' => 'boolean',
        ];
    }

    public function movie()
    {
        return $this->belongsTo(Movie::class);
    }

    public function episode()
    {
        return $this->belongsTo(Episode::class);
    }

    public function subtitles()
    {
        return $this->hasMany(Subtitle::class);
    }

}
