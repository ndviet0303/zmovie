<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Symfony\Component\HttpFoundation\Response;

class ResolveApiUser
{
    public function handle(Request $request, Closure $next): Response
    {
        $userId = $request->headers->get('X-User-Id')
            ?: $request->server('HTTP_X_USER_ID')
            ?: $request->query('user_id');

        if ($userId) {
            Auth::loginUsingId((int) $userId);
        }

        return $next($request);
    }
}
