<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\LegalDocument;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Validation\Rule;

class LegalDocumentController extends Controller
{
    public function index(Request $request)
    {
        return response()->json(LegalDocument::query()
            ->with(['contentProvider:id,name,slug', 'movie:id,title,slug', 'license:id,contract_number,status'])
            ->when($request->query('provider_id'), fn ($query, $id) => $query->where('content_provider_id', $id))
            ->when($request->query('movie_id'), fn ($query, $id) => $query->where('movie_id', $id))
            ->when($request->query('status'), fn ($query, $status) => $query->where('status', $status))
            ->latest('id')
            ->paginate((int) $request->query('per_page', 15)));
    }

    public function store(Request $request)
    {
        $data = $this->validatedDocument($request);
        $data['uploaded_by'] = Auth::id();

        $document = LegalDocument::create($data);

        return response()->json($document, 201);
    }

    public function show(LegalDocument $legalDocument)
    {
        return response()->json($legalDocument->load(['contentProvider', 'movie', 'license', 'uploader']));
    }

    public function update(Request $request, LegalDocument $legalDocument)
    {
        $data = $this->validatedDocument($request, true);

        if (($data['status'] ?? null) === 'verified') {
            $data['verified_by'] = Auth::id();
            $data['verified_at'] = now();
        }

        $legalDocument->update($data);

        return response()->json($legalDocument->fresh());
    }

    private function validatedDocument(Request $request, bool $partial = false): array
    {
        $required = $partial ? 'sometimes' : 'required';

        return $request->validate([
            'content_provider_id' => [$required, 'exists:content_providers,id'],
            'movie_id' => ['nullable', 'exists:movies,id'],
            'content_license_id' => ['nullable', 'exists:content_licenses,id'],
            'document_type' => [$required, Rule::in(['contract', 'copyright_certificate', 'distribution_agreement', 'censorship_certificate', 'identity_document', 'tax_document', 'other'])],
            'status' => ['sometimes', Rule::in(['pending', 'verified', 'rejected', 'expired'])],
            'title' => [$required, 'string', 'max:255'],
            'disk' => ['sometimes', 'string', 'max:255'],
            'path' => [$required, 'string', 'max:255'],
            'original_filename' => ['nullable', 'string', 'max:255'],
            'mime_type' => ['nullable', 'string', 'max:100'],
            'file_size_bytes' => ['nullable', 'integer', 'min:1'],
            'checksum_sha256' => ['nullable', 'string', 'size:64'],
            'issued_at' => ['nullable', 'date'],
            'expires_at' => ['nullable', 'date'],
            'review_note' => ['nullable', 'string'],
        ]);
    }
}
