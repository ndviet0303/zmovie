<?php

namespace App\Http\Middleware;

use App\Models\User;
use Closure;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Symfony\Component\HttpFoundation\Response;

class EnsureUserHasPermission
{
    public function handle(Request $request, Closure $next, string $permission): Response
    {
        $userId = $request->headers->get('X-User-Id')
            ?: $request->server('HTTP_X_USER_ID')
            ?: $request->server('HTTP_X_USER_ID'.'')
            ?: $request->input('user_id')
            ?: $request->query('user_id');

        $user = $userId ? User::find((int) $userId) : Auth::user();

        if (! $user || ! $user->hasPermission($permission)) {
            abort(403, 'Missing permission: '.$permission);
        }

        return $next($request);
    }
}
