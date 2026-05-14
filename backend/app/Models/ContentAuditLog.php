<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class ContentAuditLog extends Model
{
    protected $guarded = [];

    protected function casts(): array
    {
        return [
            'old_values' => 'array',
            'new_values' => 'array',
        ];
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function contentProvider()
    {
        return $this->belongsTo(ContentProvider::class);
    }

    public function auditable()
    {
        return $this->morphTo();
    }
}
