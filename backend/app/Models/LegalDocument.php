<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class LegalDocument extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'issued_at' => 'date',
            'expires_at' => 'date',
            'verified_at' => 'datetime',
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
}
