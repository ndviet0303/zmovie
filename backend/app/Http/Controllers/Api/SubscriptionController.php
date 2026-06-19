<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Plan;
use App\Services\SubscriptionService;
use Illuminate\Http\Request;

class SubscriptionController extends Controller
{
    public function __construct(private SubscriptionService $subscriptions)
    {
    }

    public function me(Request $request)
    {
        $subscription = $this->subscriptions->activeSubscription($request->user());

        return response()->json([
            'subscription' => $subscription,
            'is_vip' => $subscription !== null && (int) ($subscription->plan->max_quality ?? 0) >= 1080,
            'max_quality' => $subscription?->plan->max_quality ?? 480,
        ]);
    }

    public function subscribe(Request $request)
    {
        $data = $request->validate([
            'plan_id' => ['required', 'exists:plans,id'],
        ]);

        $plan = Plan::where('is_active', true)->findOrFail($data['plan_id']);
        $transaction = $this->subscriptions->createPendingPayment($request->user(), $plan);

        return response()->json([
            'transaction' => $transaction,
            'payment_instructions' => [
                'bank' => 'ZMovie Demo Bank',
                'account_number' => '0123456789',
                'account_name' => 'CONG TY ZMOVIE',
                'amount' => $transaction->amount_cents,
                'currency' => $transaction->currency,
                'reference_code' => $transaction->reference_code,
                'note' => "Chuyển khoản nội dung: {$transaction->reference_code}",
            ],
        ], 201);
    }

    public function cancel(Request $request)
    {
        $this->subscriptions->cancel($request->user());

        return response()->noContent();
    }
}
