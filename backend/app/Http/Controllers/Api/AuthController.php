<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\User;
use Database\Seeders\DemoAccountSeeder;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\ValidationException;

class AuthController extends Controller
{
    public function login(Request $request)
    {
        $credentials = $request->validate([
            'email' => ['required', 'email'],
            'password' => ['required', 'string'],
        ]);

        $user = User::query()
            ->with('roles.permissions')
            ->where('email', $credentials['email'])
            ->first();

        if (! $user || ! Hash::check($credentials['password'], $user->password)) {
            throw ValidationException::withMessages([
                'email' => ['Email hoặc mật khẩu không đúng.'],
            ]);
        }

        if ($user->status !== 'active') {
            throw ValidationException::withMessages([
                'email' => ['Tài khoản đã bị khóa hoặc chưa được kích hoạt.'],
            ]);
        }

        $user->update(['last_login_at' => now()]);
        $user->tokens()->where('name', 'admin-console')->delete();

        $permissions = $this->permissions($user);
        $token = $user->createToken('admin-console', $permissions)->plainTextToken;

        return response()->json([
            'token_type' => 'Bearer',
            'access_token' => $token,
            'user' => $user,
            'permissions' => $permissions,
        ]);
    }

    public function demoAccounts()
    {
        return response()->json(DemoAccountSeeder::accounts());
    }

    public function me(Request $request)
    {
        $user = $request->user()?->load('roles.permissions');

        abort_unless($user, 401);

        return response()->json([
            'user' => $user,
            'permissions' => $this->permissions($user),
        ]);
    }

    public function logout(Request $request)
    {
        $request->user()?->currentAccessToken()?->delete();

        return response()->noContent();
    }

    private function permissions(User $user): array
    {
        return $user->roles
            ->flatMap(fn ($role) => $role->permissions)
            ->pluck('slug')
            ->unique()
            ->values()
            ->all();
    }
}
