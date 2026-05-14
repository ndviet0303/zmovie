<?php

use App\Http\Controllers\Api\ContentLicenseController;
use App\Http\Controllers\Api\ContentProviderController;
use App\Http\Controllers\Api\AuthController;
use App\Http\Controllers\Api\LegalDocumentController;
use App\Http\Controllers\Api\LookupController;
use App\Http\Controllers\Api\MovieController;
use App\Http\Controllers\Api\MovieUploadController;
use App\Http\Controllers\Api\RbacController;
use App\Http\Controllers\Api\SearchController;
use App\Http\Controllers\Api\VideoStreamController;
use Illuminate\Support\Facades\Route;

Route::get('/health', fn () => ['status' => 'ok']);

Route::prefix('v1')->middleware('api.user')->group(function () {
    Route::post('/auth/login', [AuthController::class, 'login']);
    Route::get('/auth/me', [AuthController::class, 'me']);
    Route::get('/auth/demo-accounts', [AuthController::class, 'demoAccounts']);

    Route::get('/lookups', [LookupController::class, 'index']);
    Route::get('/search/movies', [SearchController::class, 'movies']);

    Route::get('/movies', [MovieController::class, 'index']);
    Route::get('/movies/{movie}', [MovieController::class, 'show']);
    Route::get('/video-sources/{videoSource}/stream', [VideoStreamController::class, 'show']);

    Route::middleware('permission:movies.manage')->group(function () {
        Route::post('/movies', [MovieController::class, 'store']);
        Route::put('/movies/{movie}', [MovieController::class, 'update']);
        Route::delete('/movies/{movie}', [MovieController::class, 'destroy']);
    });

    Route::post('/movies/{movie}/publish', [MovieController::class, 'publish'])
        ->middleware('permission:movies.publish');

    Route::apiResource('content-providers', ContentProviderController::class)
        ->middleware('permission:providers.manage');

    Route::post('/content-providers/{contentProvider}/members', [ContentProviderController::class, 'attachMember'])
        ->middleware('permission:providers.members.manage');

    Route::apiResource('content-licenses', ContentLicenseController::class)
        ->middleware('permission:legal.submit');

    Route::post('/content-licenses/{contentLicense}/approve', [ContentLicenseController::class, 'approve'])
        ->middleware('permission:licenses.approve');

    Route::apiResource('legal-documents', LegalDocumentController::class)
        ->only(['index', 'store', 'show', 'update'])
        ->middleware('permission:legal.submit');

    Route::apiResource('movie-uploads', MovieUploadController::class)
        ->middleware('permission:uploads.create');

    Route::post('/movie-uploads/{movieUpload}/submit', [MovieUploadController::class, 'submit'])
        ->middleware('permission:uploads.create');

    Route::post('/movie-uploads/{movieUpload}/approve', [MovieUploadController::class, 'approve'])
        ->middleware('permission:movies.review');

    Route::get('/roles', [RbacController::class, 'roles'])->middleware('permission:roles.manage');
    Route::get('/permissions', [RbacController::class, 'permissions'])->middleware('permission:roles.manage');
});
