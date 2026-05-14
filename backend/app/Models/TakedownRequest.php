<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class TakedownRequest extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'handled_at' => 'datetime',
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

    public function contentProvider()
    {
        return $this->belongsTo(ContentProvider::class);
    }

    public function requester()
    {
        return $this->belongsTo(User::class, 'requested_by');
    }

    public function handler()
    {
        return $this->belongsTo(User::class, 'handled_by');
    }
}
