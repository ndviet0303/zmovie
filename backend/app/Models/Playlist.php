<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Playlist extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'is_public' => 'boolean',
        ];
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function items()
    {
        return $this->hasMany(PlaylistItem::class);
    }

    public function movies()
    {
        return $this->belongsToMany(Movie::class, 'playlist_items')->withPivot('sort_order');
    }
}
