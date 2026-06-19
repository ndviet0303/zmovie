<?php

namespace App\Services;

use App\Models\PaymentTransaction;
use App\Models\Plan;
use App\Models\Subscription;
use App\Models\User;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Str;

class SubscriptionService
{
    public function activeSubscription(User $user): ?Subscription
    {
        return $user->subscriptions()
            ->with('plan')
            ->whereIn('status', ['trialing', 'active'])
            ->where(fn ($q) => $q->whereNull('ends_at')->orWhere('ends_at', '>', now()))
            ->latest('id')
            ->first();
    }

    public function createPendingPayment(User $user, Plan $plan): PaymentTransaction
    {
        $reference = $this->generateReference();

        return PaymentTransaction::create([
            'user_id' => $user->id,
            'subscription_id' => null,
            'provider' => 'bank_transfer',
            'reference_code' => $reference,
            'amount_cents' => $plan->price_cents,
            'currency' => $plan->currency,
            'status' => 'pending',
            'payload' => [
                'plan_id' => $plan->id,
                'plan_slug' => $plan->slug,
                'plan_name' => $plan->name,
            ],
        ]);
    }

    public function confirmPayment(PaymentTransaction $transaction): Subscription
    {
        abort_if($transaction->status === 'paid', 422, 'Giao dịch đã được xác nhận.');

        $plan = Plan::findOrFail($transaction->payload['plan_id']);

        return DB::transaction(function () use ($transaction, $plan) {
            $subscription = $this->openSubscription($transaction->user, $plan);

            $transaction->update([
                'subscription_id' => $subscription->id,
                'status' => 'paid',
                'paid_at' => now(),
            ]);

            return $subscription->load('plan');
        });
    }

    public function cancel(User $user): void
    {
        $user->subscriptions()
            ->whereIn('status', ['trialing', 'active'])
            ->update(['status' => 'canceled', 'canceled_at' => now()]);
    }

    private function openSubscription(User $user, Plan $plan): Subscription
    {
        $user->subscriptions()
            ->whereIn('status', ['trialing', 'active'])
            ->update(['status' => 'canceled', 'canceled_at' => now()]);

        $startsAt = now();
        $endsAt = match ($plan->billing_cycle) {
            'quarterly' => $startsAt->copy()->addMonths(3),
            'yearly' => $startsAt->copy()->addYear(),
            default => $startsAt->copy()->addMonth(),
        };

        return $user->subscriptions()->create([
            'plan_id' => $plan->id,
            'status' => 'active',
            'starts_at' => $startsAt,
            'ends_at' => $endsAt,
        ]);
    }

    private function generateReference(): string
    {
        do {
            $code = 'ZM' . strtoupper(Str::random(8));
        } while (PaymentTransaction::where('reference_code', $code)->exists());

        return $code;
    }
}
