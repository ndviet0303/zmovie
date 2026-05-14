<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('roles', function (Blueprint $table) {
            $table->id();
            $table->string('name');
            $table->string('slug')->unique();
            $table->string('guard_name')->default('web');
            $table->text('description')->nullable();
            $table->boolean('is_system')->default(false);
            $table->timestamps();
        });

        Schema::create('permissions', function (Blueprint $table) {
            $table->id();
            $table->string('name');
            $table->string('slug')->unique();
            $table->string('group')->nullable();
            $table->text('description')->nullable();
            $table->timestamps();
        });

        Schema::create('permission_role', function (Blueprint $table) {
            $table->foreignId('permission_id')->constrained()->cascadeOnDelete();
            $table->foreignId('role_id')->constrained()->cascadeOnDelete();
            $table->primary(['permission_id', 'role_id']);
        });

        Schema::create('role_user', function (Blueprint $table) {
            $table->foreignId('role_id')->constrained()->cascadeOnDelete();
            $table->foreignId('user_id')->constrained()->cascadeOnDelete();
            $table->primary(['role_id', 'user_id']);
        });

        Schema::create('content_providers', function (Blueprint $table) {
            $table->id();
            $table->string('name');
            $table->string('slug')->unique();
            $table->string('legal_name')->nullable();
            $table->string('tax_code', 80)->nullable();
            $table->string('business_registration_number', 120)->nullable();
            $table->string('country_code', 3)->nullable();
            $table->string('contact_name')->nullable();
            $table->string('contact_email')->nullable();
            $table->string('contact_phone', 30)->nullable();
            $table->enum('type', ['studio', 'distributor', 'aggregator', 'independent', 'internal'])->default('distributor');
            $table->enum('verification_status', ['pending', 'verified', 'rejected', 'suspended'])->default('pending');
            $table->json('settings')->nullable();
            $table->foreignId('verified_by')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamp('verified_at')->nullable();
            $table->timestamps();
            $table->softDeletes();
        });

        Schema::create('content_provider_user', function (Blueprint $table) {
            $table->id();
            $table->foreignId('content_provider_id')->constrained()->cascadeOnDelete();
            $table->foreignId('user_id')->constrained()->cascadeOnDelete();
            $table->enum('provider_role', ['owner', 'admin', 'uploader', 'legal', 'finance', 'viewer'])->default('uploader');
            $table->enum('status', ['invited', 'active', 'disabled'])->default('active');
            $table->json('permission_overrides')->nullable();
            $table->foreignId('invited_by')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamp('joined_at')->nullable();
            $table->timestamps();
            $table->unique(['content_provider_id', 'user_id']);
        });

        Schema::table('movies', function (Blueprint $table) {
            $table->foreignId('content_provider_id')->nullable()->after('id')->constrained()->nullOnDelete();
            $table->enum('rights_status', ['unknown', 'pending', 'cleared', 'expired', 'disputed', 'blocked'])->default('unknown')->after('status');
        });

        Schema::create('content_licenses', function (Blueprint $table) {
            $table->id();
            $table->foreignId('content_provider_id')->constrained()->cascadeOnDelete();
            $table->foreignId('movie_id')->nullable()->constrained()->cascadeOnDelete();
            $table->string('contract_number')->nullable();
            $table->string('licensor_name');
            $table->enum('license_type', ['exclusive', 'non_exclusive', 'owned', 'public_domain', 'user_generated']);
            $table->enum('status', ['draft', 'pending_review', 'approved', 'rejected', 'expired', 'terminated'])->default('draft');
            $table->json('rights')->nullable();
            $table->date('valid_from');
            $table->date('valid_until')->nullable();
            $table->boolean('allows_streaming')->default(true);
            $table->boolean('allows_download')->default(false);
            $table->boolean('allows_ads')->default(true);
            $table->boolean('allows_subscription')->default(true);
            $table->boolean('allows_free_access')->default(false);
            $table->enum('territory_mode', ['worldwide', 'include', 'exclude'])->default('worldwide');
            $table->foreignId('reviewed_by')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamp('approved_at')->nullable();
            $table->text('review_note')->nullable();
            $table->timestamps();
            $table->index(['content_provider_id', 'status']);
            $table->index(['movie_id', 'status']);
            $table->index(['valid_from', 'valid_until']);
        });

        Schema::create('content_license_country', function (Blueprint $table) {
            $table->foreignId('content_license_id')->constrained()->cascadeOnDelete();
            $table->foreignId('country_id')->constrained()->cascadeOnDelete();
            $table->primary(['content_license_id', 'country_id']);
        });

        Schema::create('legal_documents', function (Blueprint $table) {
            $table->id();
            $table->foreignId('content_provider_id')->constrained()->cascadeOnDelete();
            $table->foreignId('movie_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('content_license_id')->nullable()->constrained()->cascadeOnDelete();
            $table->foreignId('uploaded_by')->nullable()->constrained('users')->nullOnDelete();
            $table->enum('document_type', [
                'contract',
                'copyright_certificate',
                'distribution_agreement',
                'censorship_certificate',
                'identity_document',
                'tax_document',
                'other',
            ]);
            $table->enum('status', ['pending', 'verified', 'rejected', 'expired'])->default('pending');
            $table->string('title');
            $table->string('disk')->default('private');
            $table->string('path');
            $table->string('original_filename')->nullable();
            $table->string('mime_type', 100)->nullable();
            $table->unsignedBigInteger('file_size_bytes')->nullable();
            $table->string('checksum_sha256', 64)->nullable();
            $table->date('issued_at')->nullable();
            $table->date('expires_at')->nullable();
            $table->foreignId('verified_by')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamp('verified_at')->nullable();
            $table->text('review_note')->nullable();
            $table->timestamps();
            $table->index(['content_provider_id', 'document_type', 'status']);
        });

        Schema::create('movie_uploads', function (Blueprint $table) {
            $table->id();
            $table->foreignId('content_provider_id')->constrained()->cascadeOnDelete();
            $table->foreignId('movie_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('content_license_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('uploaded_by')->constrained('users')->cascadeOnDelete();
            $table->foreignId('reviewed_by')->nullable()->constrained('users')->nullOnDelete();
            $table->string('title');
            $table->enum('upload_type', ['new_movie', 'new_series', 'new_episode', 'replace_source', 'metadata_update'])->default('new_movie');
            $table->enum('status', [
                'draft',
                'submitted',
                'uploading',
                'transcoding',
                'legal_review',
                'content_review',
                'approved',
                'rejected',
                'published',
                'canceled',
            ])->default('draft');
            $table->json('metadata')->nullable();
            $table->text('rejection_reason')->nullable();
            $table->timestamp('submitted_at')->nullable();
            $table->timestamp('reviewed_at')->nullable();
            $table->timestamp('published_at')->nullable();
            $table->timestamps();
            $table->index(['content_provider_id', 'status']);
            $table->index(['uploaded_by', 'status']);
        });

        Schema::create('upload_files', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_upload_id')->constrained()->cascadeOnDelete();
            $table->foreignId('movie_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('episode_id')->nullable()->constrained()->nullOnDelete();
            $table->enum('file_type', ['master_video', 'trailer', 'poster', 'backdrop', 'still', 'subtitle', 'legal_document']);
            $table->enum('status', ['pending', 'uploaded', 'processing', 'ready', 'failed', 'rejected'])->default('pending');
            $table->string('disk')->default('private');
            $table->string('path');
            $table->string('original_filename')->nullable();
            $table->string('mime_type', 100)->nullable();
            $table->unsignedBigInteger('file_size_bytes')->nullable();
            $table->string('checksum_sha256', 64)->nullable();
            $table->string('quality', 20)->nullable();
            $table->unsignedInteger('duration_seconds')->nullable();
            $table->json('technical_metadata')->nullable();
            $table->string('processing_job_id')->nullable();
            $table->text('failure_reason')->nullable();
            $table->timestamps();
            $table->index(['movie_upload_id', 'file_type', 'status']);
        });

        Schema::create('takedown_requests', function (Blueprint $table) {
            $table->id();
            $table->foreignId('movie_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('episode_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('content_provider_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('requested_by')->nullable()->constrained('users')->nullOnDelete();
            $table->foreignId('handled_by')->nullable()->constrained('users')->nullOnDelete();
            $table->enum('reason', ['copyright_claim', 'license_expired', 'territory_violation', 'court_order', 'provider_request', 'policy_violation', 'other']);
            $table->enum('status', ['open', 'reviewing', 'accepted', 'rejected', 'restored'])->default('open');
            $table->string('claimant_name')->nullable();
            $table->string('claimant_email')->nullable();
            $table->text('legal_basis')->nullable();
            $table->text('description')->nullable();
            $table->timestamp('handled_at')->nullable();
            $table->timestamps();
            $table->index(['movie_id', 'status']);
            $table->index(['content_provider_id', 'status']);
        });

        Schema::create('content_audit_logs', function (Blueprint $table) {
            $table->id();
            $table->foreignId('user_id')->nullable()->constrained()->nullOnDelete();
            $table->foreignId('content_provider_id')->nullable()->constrained()->nullOnDelete();
            $table->nullableMorphs('auditable');
            $table->string('action');
            $table->json('old_values')->nullable();
            $table->json('new_values')->nullable();
            $table->string('ip_address', 45)->nullable();
            $table->text('user_agent')->nullable();
            $table->timestamps();
            $table->index(['content_provider_id', 'action']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('content_audit_logs');
        Schema::dropIfExists('takedown_requests');
        Schema::dropIfExists('upload_files');
        Schema::dropIfExists('movie_uploads');
        Schema::dropIfExists('legal_documents');
        Schema::dropIfExists('content_license_country');
        Schema::dropIfExists('content_licenses');

        Schema::table('movies', function (Blueprint $table) {
            $table->dropConstrainedForeignId('content_provider_id');
            $table->dropColumn('rights_status');
        });

        Schema::dropIfExists('content_provider_user');
        Schema::dropIfExists('content_providers');
        Schema::dropIfExists('role_user');
        Schema::dropIfExists('permission_role');
        Schema::dropIfExists('permissions');
        Schema::dropIfExists('roles');
    }
};
