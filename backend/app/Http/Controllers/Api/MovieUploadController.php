<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\MovieUpload;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class MovieUploadController extends Controller
{
    public function index(Request $request)
    {
        return response()->json(MovieUpload::query()
            ->with(['contentProvider:id,name,slug', 'movie:id,title,slug', 'uploader:id,name,email'])
            ->withCount('files')
            ->when($request->query('provider_id'), fn ($query, $id) => $query->where('content_provider_id', $id))
            ->when($request->query('status'), fn ($query, $status) => $query->where('status', $status))
            ->latest('id')
            ->paginate((int) $request->query('per_page', 15)));
    }

    public function store(Request $request)
    {
        $data = $this->validatedUpload($request);
        $files = $data['files'] ?? [];
        unset($data['files']);

        $upload = DB::transaction(function () use ($data, $files) {
            $upload = MovieUpload::create([
                ...$data,
                'uploaded_by' => Auth::id(),
            ]);

            foreach ($files as $file) {
                $upload->files()->create($file);
            }

            return $upload;
        });

        return response()->json($upload->load('files'), 201);
    }

    public function show(MovieUpload $movieUpload)
    {
        return response()->json($movieUpload->load(['contentProvider', 'movie', 'license', 'uploader', 'reviewer', 'files']));
    }

    public function update(Request $request, MovieUpload $movieUpload)
    {
        $data = $this->validatedUpload($request, true);
        unset($data['files']);

        $movieUpload->update($data);

        return response()->json($movieUpload->fresh()->load('files'));
    }

    public function destroy(MovieUpload $movieUpload)
    {
        $movieUpload->delete();

        return response()->noContent();
    }

    public function submit(MovieUpload $movieUpload)
    {
        $movieUpload->update([
            'status' => 'submitted',
            'submitted_at' => now(),
        ]);

        return response()->json($movieUpload->fresh());
    }

    public function approve(MovieUpload $movieUpload)
    {
        $movieUpload->update([
            'status' => 'approved',
            'reviewed_by' => Auth::id(),
            'reviewed_at' => now(),
        ]);

        return response()->json($movieUpload->fresh()->load(['movie', 'files']));
    }

    private function validatedUpload(Request $request, bool $partial = false): array
    {
        $required = $partial ? 'sometimes' : 'required';

        return $request->validate([
            'content_provider_id' => [$required, 'exists:content_providers,id'],
            'movie_id' => ['nullable', 'exists:movies,id'],
            'content_license_id' => ['nullable', 'exists:content_licenses,id'],
            'title' => [$required, 'string', 'max:255'],
            'upload_type' => ['sometimes', Rule::in(['new_movie', 'new_series', 'new_episode', 'replace_source', 'metadata_update'])],
            'status' => ['sometimes', Rule::in(['draft', 'submitted', 'uploading', 'transcoding', 'legal_review', 'content_review', 'approved', 'rejected', 'published', 'canceled'])],
            'metadata' => ['nullable', 'array'],
            'rejection_reason' => ['nullable', 'string'],
            'files' => ['sometimes', 'array'],
            'files.*.movie_id' => ['nullable', 'exists:movies,id'],
            'files.*.episode_id' => ['nullable', 'exists:episodes,id'],
            'files.*.file_type' => ['required_with:files', Rule::in(['master_video', 'trailer', 'poster', 'backdrop', 'still', 'subtitle', 'legal_document'])],
            'files.*.status' => ['sometimes', Rule::in(['pending', 'uploaded', 'processing', 'ready', 'failed', 'rejected'])],
            'files.*.disk' => ['sometimes', 'string', 'max:255'],
            'files.*.path' => ['required_with:files', 'string', 'max:255'],
            'files.*.original_filename' => ['nullable', 'string', 'max:255'],
            'files.*.mime_type' => ['nullable', 'string', 'max:100'],
            'files.*.file_size_bytes' => ['nullable', 'integer', 'min:1'],
            'files.*.checksum_sha256' => ['nullable', 'string', 'size:64'],
            'files.*.quality' => ['nullable', 'string', 'max:20'],
            'files.*.duration_seconds' => ['nullable', 'integer', 'min:1'],
            'files.*.technical_metadata' => ['nullable', 'array'],
        ]);
    }
}
