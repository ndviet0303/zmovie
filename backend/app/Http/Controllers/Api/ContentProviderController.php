<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\ContentProvider;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;

class ContentProviderController extends Controller
{
    public function index(Request $request)
    {
        return response()->json(ContentProvider::query()
            ->withCount(['users', 'movies', 'uploads', 'licenses'])
            ->when($request->query('status'), fn ($query, $status) => $query->where('verification_status', $status))
            ->latest('id')
            ->paginate((int) $request->query('per_page', 15)));
    }

    public function store(Request $request)
    {
        $provider = ContentProvider::create($this->validatedProvider($request));

        return response()->json($provider, 201);
    }

    public function show(ContentProvider $contentProvider)
    {
        return response()->json($contentProvider->load(['users.roles', 'movies', 'licenses', 'legalDocuments']));
    }

    public function update(Request $request, ContentProvider $contentProvider)
    {
        $contentProvider->update($this->validatedProvider($request, $contentProvider));

        return response()->json($contentProvider->fresh());
    }

    public function destroy(ContentProvider $contentProvider)
    {
        $contentProvider->delete();

        return response()->noContent();
    }

    public function attachMember(Request $request, ContentProvider $contentProvider)
    {
        $data = $request->validate([
            'user_id' => ['required', 'exists:users,id'],
            'provider_role' => ['required', Rule::in(['owner', 'admin', 'uploader', 'legal', 'finance', 'viewer'])],
            'status' => ['sometimes', Rule::in(['invited', 'active', 'disabled'])],
            'permission_overrides' => ['nullable', 'array'],
        ]);

        $contentProvider->users()->syncWithoutDetaching([
            $data['user_id'] => [
                'provider_role' => $data['provider_role'],
                'status' => $data['status'] ?? 'active',
                'permission_overrides' => $data['permission_overrides'] ?? null,
                'joined_at' => now(),
            ],
        ]);

        return response()->json($contentProvider->load('users.roles'));
    }

    private function validatedProvider(Request $request, ?ContentProvider $provider = null): array
    {
        return $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'slug' => ['required', 'string', 'max:255', Rule::unique('content_providers', 'slug')->ignore($provider)],
            'legal_name' => ['nullable', 'string', 'max:255'],
            'tax_code' => ['nullable', 'string', 'max:80'],
            'business_registration_number' => ['nullable', 'string', 'max:120'],
            'country_code' => ['nullable', 'string', 'max:3'],
            'contact_name' => ['nullable', 'string', 'max:255'],
            'contact_email' => ['nullable', 'email', 'max:255'],
            'contact_phone' => ['nullable', 'string', 'max:30'],
            'type' => ['sometimes', Rule::in(['studio', 'distributor', 'aggregator', 'independent', 'internal'])],
            'verification_status' => ['sometimes', Rule::in(['pending', 'verified', 'rejected', 'suspended'])],
            'settings' => ['nullable', 'array'],
        ]);
    }
}
