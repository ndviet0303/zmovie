<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Subtitle extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'is_default' => 'boolean',
        ];
    }

    public function language()
    {
        return $this->belongsTo(Language::class);
    }

    public function videoSource()
    {
        return $this->belongsTo(VideoSource::class);
    }
}
