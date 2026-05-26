<?php

use App\Http\Controllers\Api\ContentLicenseController;
use App\Http\Controllers\Api\ContentProviderController;
use App\Http\Controllers\Api\AuthController;
use App\Http\Controllers\Api\DatabaseDebugController;
use App\Http\Controllers\Api\LegalDocumentController;
use App\Http\Controllers\Api\LookupController;
use App\Http\Controllers\Api\MovieController;
use App\Http\Controllers\Api\MovieUploadController;
use App\Http\Controllers\Api\RbacController;
use App\Http\Controllers\Api\SearchController;
use App\Http\Controllers\Api\VideoStreamController;
use Illuminate\Support\Facades\Route;

// DEBUG_VERSION: 2026-05-16-02-40
Route::get('/health', fn () => ['status' => 'ok']);
Route::get('/probe', fn () => [
    'status' => 'ok',
    'app' => config('app.name'),
    'env' => config('app.env'),
    'time' => now()->toISOString(),
]);
Route::get('/debug/db-connection', [DatabaseDebugController::class, 'show']);

Route::post('/auth/login', [AuthController::class, 'login']);
Route::post('/auth/register', [AuthController::class, 'register']);
Route::get('/auth/demo-accounts', [AuthController::class, 'demoAccounts']);

Route::get('/lookups', [LookupController::class, 'index']);
Route::get('/search/movies', [SearchController::class, 'movies']);

Route::get('/movies', [MovieController::class, 'index']);
Route::get('/movies/{movie}', [MovieController::class, 'show']);
Route::get('/video-sources/{videoSource}/stream', [VideoStreamController::class, 'show']);

Route::middleware('auth:sanctum')->group(function () {
    Route::get('/auth/me', [AuthController::class, 'me']);
    Route::post('/auth/logout', [AuthController::class, 'logout']);
});

Route::middleware(['auth:sanctum', 'permission:movies.manage'])->group(function () {
    Route::post('/movies', [MovieController::class, 'store']);
    Route::put('/movies/{movie}', [MovieController::class, 'update']);
    Route::delete('/movies/{movie}', [MovieController::class, 'destroy']);
});

Route::post('/movies/{movie}/publish', [MovieController::class, 'publish'])
    ->middleware(['auth:sanctum', 'permission:movies.publish']);

Route::apiResource('content-providers', ContentProviderController::class)
    ->middleware(['auth:sanctum', 'permission:providers.manage']);

Route::post('/content-providers/{contentProvider}/members', [ContentProviderController::class, 'attachMember'])
    ->middleware(['auth:sanctum', 'permission:providers.members.manage']);

Route::apiResource('content-licenses', ContentLicenseController::class)
    ->only(['index', 'show'])
    ->middleware(['auth:sanctum', 'permission:legal.submit|legal.review|licenses.approve']);

Route::apiResource('content-licenses', ContentLicenseController::class)
    ->only(['store', 'update', 'destroy'])
    ->middleware(['auth:sanctum', 'permission:legal.submit']);

Route::post('/content-licenses/{contentLicense}/approve', [ContentLicenseController::class, 'approve'])
    ->middleware(['auth:sanctum', 'permission:licenses.approve']);

Route::apiResource('legal-documents', LegalDocumentController::class)
    ->only(['index', 'show'])
    ->middleware(['auth:sanctum', 'permission:legal.submit|legal.review']);

Route::apiResource('legal-documents', LegalDocumentController::class)
    ->only(['store'])
    ->middleware(['auth:sanctum', 'permission:legal.submit']);

Route::put('/legal-documents/{legalDocument}', [LegalDocumentController::class, 'update'])
    ->middleware(['auth:sanctum', 'permission:legal.submit|legal.review']);

Route::apiResource('movie-uploads', MovieUploadController::class)
    ->only(['index', 'show'])
    ->middleware(['auth:sanctum', 'permission:uploads.create|uploads.manage|uploads.view|movies.review']);

Route::apiResource('movie-uploads', MovieUploadController::class)
    ->only(['store', 'update', 'destroy'])
    ->middleware(['auth:sanctum', 'permission:uploads.create|uploads.manage']);

Route::post('/movie-uploads/{movieUpload}/submit', [MovieUploadController::class, 'submit'])
    ->middleware(['auth:sanctum', 'permission:uploads.create']);

Route::post('/movie-uploads/{movieUpload}/approve', [MovieUploadController::class, 'approve'])
    ->middleware(['auth:sanctum', 'permission:movies.review']);

Route::post('/movie-uploads/{movieUpload}/transcode', [MovieUploadController::class, 'transcode'])
    ->middleware(['auth:sanctum', 'permission:movies.review']);

Route::get('/roles', [RbacController::class, 'roles'])->middleware(['auth:sanctum', 'permission:roles.manage']);
Route::get('/permissions', [RbacController::class, 'permissions'])->middleware(['auth:sanctum', 'permission:roles.manage']);
