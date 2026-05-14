<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class ContentLicense extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'rights' => 'array',
            'valid_from' => 'date',
            'valid_until' => 'date',
            'allows_streaming' => 'boolean',
            'allows_download' => 'boolean',
            'allows_ads' => 'boolean',
            'allows_subscription' => 'boolean',
            'allows_free_access' => 'boolean',
            'approved_at' => 'datetime',
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

    public function countries()
    {
        return $this->belongsToMany(Country::class);
    }

    public function legalDocuments()
    {
        return $this->hasMany(LegalDocument::class);
    }
}
