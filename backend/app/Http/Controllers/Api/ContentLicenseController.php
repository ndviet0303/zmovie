<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\ContentLicense;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class ContentLicenseController extends Controller
{
    public function index(Request $request)
    {
        return response()->json(ContentLicense::query()
            ->with(['contentProvider:id,name,slug', 'movie:id,title,slug', 'countries:id,name,code'])
            ->when($request->query('provider_id'), fn ($query, $id) => $query->where('content_provider_id', $id))
            ->when($request->query('movie_id'), fn ($query, $id) => $query->where('movie_id', $id))
            ->when($request->query('status'), fn ($query, $status) => $query->where('status', $status))
            ->latest('id')
            ->paginate((int) $request->query('per_page', 15)));
    }

    public function store(Request $request)
    {
        $data = $this->validatedLicense($request);

        $license = DB::transaction(function () use ($data) {
            $license = ContentLicense::create(collect($data)->except('country_ids')->all());

            if (array_key_exists('country_ids', $data)) {
                $license->countries()->sync($data['country_ids']);
            }

            return $license;
        });

        return response()->json($license->load('countries'), 201);
    }

    public function show(ContentLicense $contentLicense)
    {
        return response()->json($contentLicense->load(['contentProvider', 'movie', 'countries', 'legalDocuments']));
    }

    public function update(Request $request, ContentLicense $contentLicense)
    {
        $data = $this->validatedLicense($request);

        DB::transaction(function () use ($contentLicense, $data) {
            $contentLicense->update(collect($data)->except('country_ids')->all());

            if (array_key_exists('country_ids', $data)) {
                $contentLicense->countries()->sync($data['country_ids']);
            }
        });

        return response()->json($contentLicense->fresh()->load('countries'));
    }

    public function destroy(ContentLicense $contentLicense)
    {
        $contentLicense->delete();

        return response()->noContent();
    }

    public function approve(Request $request, ContentLicense $contentLicense)
    {
        $data = $request->validate([
            'review_note' => ['nullable', 'string'],
            'clear_movie_rights' => ['sometimes', 'boolean'],
        ]);

        DB::transaction(function () use ($contentLicense, $data) {
            $contentLicense->update([
                'status' => 'approved',
                'reviewed_by' => Auth::id(),
                'approved_at' => now(),
                'review_note' => $data['review_note'] ?? null,
            ]);

            if (($data['clear_movie_rights'] ?? true) && $contentLicense->movie) {
                $contentLicense->movie->update(['rights_status' => 'cleared']);
            }
        });

        return response()->json($contentLicense->fresh()->load('movie'));
    }

    private function validatedLicense(Request $request): array
    {
        return $request->validate([
            'content_provider_id' => ['required', 'exists:content_providers,id'],
            'movie_id' => ['nullable', 'exists:movies,id'],
            'contract_number' => ['nullable', 'string', 'max:255'],
            'licensor_name' => ['required', 'string', 'max:255'],
            'license_type' => ['required', Rule::in(['exclusive', 'non_exclusive', 'owned', 'public_domain', 'user_generated'])],
            'status' => ['sometimes', Rule::in(['draft', 'pending_review', 'approved', 'rejected', 'expired', 'terminated'])],
            'rights' => ['nullable', 'array'],
            'valid_from' => ['required', 'date'],
            'valid_until' => ['nullable', 'date', 'after_or_equal:valid_from'],
            'allows_streaming' => ['sometimes', 'boolean'],
            'allows_download' => ['sometimes', 'boolean'],
            'allows_ads' => ['sometimes', 'boolean'],
            'allows_subscription' => ['sometimes', 'boolean'],
            'allows_free_access' => ['sometimes', 'boolean'],
            'territory_mode' => ['sometimes', Rule::in(['worldwide', 'include', 'exclude'])],
            'review_note' => ['nullable', 'string'],
            'country_ids' => ['sometimes', 'array'],
            'country_ids.*' => ['integer', 'exists:countries,id'],
        ]);
    }
}
