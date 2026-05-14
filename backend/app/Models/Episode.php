<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\SoftDeletes;

class Episode extends Model
{
    use SoftDeletes;

    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'published_at' => 'datetime',
        ];
    }

    public function season()
    {
        return $this->belongsTo(Season::class);
    }

    public function videoSources()
    {
        return $this->hasMany(VideoSource::class);
    }

    public function subtitles()
    {
        return $this->hasMany(Subtitle::class);
    }
}
