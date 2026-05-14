<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Permission;
use App\Models\Role;

class RbacController extends Controller
{
    public function roles()
    {
        return response()->json(Role::with('permissions')->orderBy('name')->get());
    }

    public function permissions()
    {
        return response()->json(Permission::orderBy('group')->orderBy('name')->get());
    }
}
