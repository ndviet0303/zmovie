<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class DatabaseDebugController extends Controller
{
    public function show(Request $request)
    {
        $connection = config('database.default');
        $config = config("database.connections.{$connection}");
        $runtime = DB::selectOne('
            select
                current_database() as database,
                current_user as username,
                current_schema() as current_schema,
                current_setting(\'search_path\') as search_path
        ');

        return response()->json([
            'connection' => $connection,
            'configured' => [
                'driver' => $config['driver'] ?? null,
                'host' => $config['host'] ?? null,
                'port' => $config['port'] ?? null,
                'database' => $config['database'] ?? null,
                'username' => $config['username'] ?? null,
                'schema' => $config['search_path'] ?? null,
                'sslmode' => $config['sslmode'] ?? null,
            ],
            'runtime' => $runtime,
            'counts' => [
                'movies' => $this->safeCount('movies'),
                'genres' => $this->safeCount('genres'),
                'countries' => $this->safeCount('countries'),
                'languages' => $this->safeCount('languages'),
                'episodes' => $this->safeCount('episodes'),
                'video_sources' => $this->safeCount('video_sources'),
                'users' => $this->safeCount('users'),
            ],
        ]);
    }

    private function safeCount(string $table): ?int
    {
        try {
            return DB::table($table)->count();
        } catch (\Throwable) {
            return null;
        }
    }
}
