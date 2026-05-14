<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class UploadFile extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'technical_metadata' => 'array',
        ];
    }

    public function movieUpload()
    {
        return $this->belongsTo(MovieUpload::class);
    }

    public function movie()
    {
        return $this->belongsTo(Movie::class);
    }

    public function episode()
    {
        return $this->belongsTo(Episode::class);
    }
}
