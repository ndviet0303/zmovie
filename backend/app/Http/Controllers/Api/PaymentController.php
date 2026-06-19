<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\PaymentTransaction;
use App\Services\SubscriptionService;
use Illuminate\Http\Request;

class PaymentController extends Controller
{
    public function __construct(private SubscriptionService $subscriptions)
    {
    }

    public function confirm(Request $request, string $reference)
    {
        $transaction = PaymentTransaction::where('reference_code', $reference)
            ->where('user_id', $request->user()->id)
            ->firstOrFail();

        $subscription = $this->subscriptions->confirmPayment($transaction);

        return response()->json([
            'transaction' => $transaction->fresh(),
            'subscription' => $subscription,
        ]);
    }
}
