<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class MovieUpload extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'metadata' => 'array',
            'submitted_at' => 'datetime',
            'reviewed_at' => 'datetime',
            'published_at' => 'datetime',
        ];
    }

    public function contentProvider()
    {
        return $this->belongsTo(ContentProvider::class);
    }

    public function movie()
    {
        return $this->belongsTo(Movie::class);
    }

    public function license()
    {
        return $this->belongsTo(ContentLicense::class, 'content_license_id');
    }

    public function uploader()
    {
        return $this->belongsTo(User::class, 'uploaded_by');
    }

    public function reviewer()
    {
        return $this->belongsTo(User::class, 'reviewed_by');
    }

    public function files()
    {
        return $this->hasMany(UploadFile::class);
    }
}
