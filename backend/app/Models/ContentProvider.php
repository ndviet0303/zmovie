<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\SoftDeletes;

class ContentProvider extends Model
{
    use SoftDeletes;

    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'settings' => 'array',
            'verified_at' => 'datetime',
        ];
    }

    public function users()
    {
        return $this->belongsToMany(User::class)
            ->withPivot(['provider_role', 'status', 'permission_overrides', 'invited_by', 'joined_at'])
            ->withTimestamps();
    }

    public function movies()
    {
        return $this->hasMany(Movie::class);
    }

    public function licenses()
    {
        return $this->hasMany(ContentLicense::class);
    }

    public function legalDocuments()
    {
        return $this->hasMany(LegalDocument::class);
    }

    public function uploads()
    {
        return $this->hasMany(MovieUpload::class);
    }
}
