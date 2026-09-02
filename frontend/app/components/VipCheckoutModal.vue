<script setup lang="ts">
import {
  Check,
  CheckCircle2,
  Copy,
  Crown,
  QrCode,
  ShieldCheck,
  Sparkles,
  Zap,
  X,
} from "@lucide/vue";
import { onBeforeUnmount, ref } from "vue";

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  close: [];
  success: [];
}>();

const selectedPlan = ref<"monthly" | "yearly">("yearly");
const step = ref<"select" | "payment" | "confirmed">("select");
const isCopiedStk = ref(false);
const isCopiedContent = ref(false);
const timerSeconds = ref(15 * 60);
let countdownInterval: ReturnType<typeof setInterval> | null = null;

const plans = {
  monthly: {
    name: "VIP Tháng",
    price: "59.000đ",
    amount: 59000,
    duration: "1 tháng",
    badge: null,
  },
  yearly: {
    name: "VIP Năm",
    price: "599.000đ",
    amount: 599000,
    duration: "12 tháng",
    badge: "Tiết kiệm 20%",
  },
};

const bankInfo = ref({
  bankName: "MBBank (Ngân hàng Quân Đội)",
  accountNumber: "0988888888",
  accountName: "CONG TY ZMOVIE VIETNAM",
  orderCode: "ZM_VIP_88899",
  qrUrl: "",
});

function startCountdown() {
  timerSeconds.value = 15 * 60;
  if (countdownInterval) clearInterval(countdownInterval);
  countdownInterval = setInterval(() => {
    if (timerSeconds.value > 0) {
      timerSeconds.value--;
    } else {
      if (countdownInterval) clearInterval(countdownInterval);
    }
  }, 1000);
}

function proceedToPayment() {
  const plan = plans[selectedPlan.value];
  const order = `ZM_VIP_${Math.floor(10000 + Math.random() * 90000)}`;
  bankInfo.value.orderCode = order;
  bankInfo.value.qrUrl = `https://img.vietqr.io/image/970422-0988888888-compact2.png?amount=${plan.amount}&addInfo=${encodeURIComponent(order)}&accountName=${encodeURIComponent("CONG TY ZMOVIE VIETNAM")}`;
  step.value = "payment";
  startCountdown();
}

function copyStk() {
  void navigator.clipboard.writeText(bankInfo.value.accountNumber);
  isCopiedStk.value = true;
  setTimeout(() => (isCopiedStk.value = false), 2000);
}

function copyContent() {
  void navigator.clipboard.writeText(bankInfo.value.orderCode);
  isCopiedContent.value = true;
  setTimeout(() => (isCopiedContent.value = false), 2000);
}

function confirmPayment() {
  step.value = "confirmed";
  setTimeout(() => {
    emit("success");
    emit("close");
    step.value = "select";
  }, 2500);
}

function formatTimer(seconds: number) {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s < 10 ? "0" : ""}${s}`;
}

onBeforeUnmount(() => {
  if (countdownInterval) clearInterval(countdownInterval);
});
</script>

<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4"
    role="dialog"
    aria-modal="true"
    aria-label="Nâng cấp VIP ZMovie"
  >
    <!-- Backdrop -->
    <div
      class="fixed inset-0 bg-black/80 backdrop-blur-md transition-opacity"
      @click="emit('close')"
    />

    <!-- Modal Content -->
    <div
      class="relative w-full max-w-lg overflow-hidden rounded-3xl border border-amber-500/30 bg-surface-container-lowest shadow-2xl shadow-amber-500/10 p-6 z-10"
    >
      <!-- Close button -->
      <button
        class="absolute right-4 top-4 rounded-full p-2 text-muted-foreground transition hover:bg-white/10 hover:text-foreground"
        @click="emit('close')"
      >
        <X class="size-4" />
      </button>

      <!-- STEP 1: SELECT PLAN -->
      <div v-if="step === 'select'">
        <!-- Header -->
        <div class="text-center">
          <div
            class="mx-auto grid size-12 place-items-center rounded-2xl bg-gradient-to-tr from-amber-500 to-amber-300 text-black shadow-lg shadow-amber-500/20"
          >
            <Crown class="size-6 fill-current" />
          </div>
          <h2 class="mt-3 font-display text-xl font-bold text-foreground">
            Nâng cấp Hội viên VIP
          </h2>
          <p class="mt-1 text-xs text-muted-foreground">
            Thưởng thức trọn vẹn rạp phim trực tuyến không giới hạn
          </p>
        </div>

        <!-- VIP Perks Grid -->
        <div class="mt-5 grid grid-cols-2 gap-2 text-xs">
          <div
            class="flex items-center gap-2 rounded-xl bg-surface-container/60 p-2.5 border border-white/5"
          >
            <Zap class="size-4 text-amber-400 shrink-0" />
            <span class="text-muted-foreground">100% Không quảng cáo</span>
          </div>
          <div
            class="flex items-center gap-2 rounded-xl bg-surface-container/60 p-2.5 border border-white/5"
          >
            <Sparkles class="size-4 text-amber-400 shrink-0" />
            <span class="text-muted-foreground">Phim 4K R2 siêu tốc</span>
          </div>
          <div
            class="flex items-center gap-2 rounded-xl bg-surface-container/60 p-2.5 border border-white/5"
          >
            <Crown class="size-4 text-amber-400 shrink-0" />
            <span class="text-muted-foreground">Huy hiệu VIP độc quyền</span>
          </div>
          <div
            class="flex items-center gap-2 rounded-xl bg-surface-container/60 p-2.5 border border-white/5"
          >
            <ShieldCheck class="size-4 text-amber-400 shrink-0" />
            <span class="text-muted-foreground"
              >Watch Party không giới hạn</span
            >
          </div>
        </div>

        <!-- Plans Selection -->
        <div class="mt-5 space-y-3">
          <div
            class="cursor-pointer rounded-2xl border p-4 transition"
            :class="
              selectedPlan === 'yearly'
                ? 'border-amber-500 bg-amber-500/10 shadow-md shadow-amber-500/5'
                : 'border-white/10 bg-surface-container hover:border-white/20'
            "
            @click="selectedPlan = 'yearly'"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="font-semibold text-foreground text-sm"
                  >Gói 1 Năm</span
                >
                <span
                  class="rounded-full bg-amber-400/20 px-2 py-0.5 text-[10px] font-bold text-amber-300 border border-amber-400/30"
                >
                  Tiết kiệm 20%
                </span>
              </div>
              <span class="font-display text-base font-bold text-amber-400"
                >599.000đ</span
              >
            </div>
            <p class="mt-1 text-[11px] text-muted-foreground">
              Chỉ ~49.000đ / tháng. Đã bao gồm trọn bộ đặc quyền VIP.
            </p>
          </div>

          <div
            class="cursor-pointer rounded-2xl border p-4 transition"
            :class="
              selectedPlan === 'monthly'
                ? 'border-amber-500 bg-amber-500/10 shadow-md shadow-amber-500/5'
                : 'border-white/10 bg-surface-container hover:border-white/20'
            "
            @click="selectedPlan = 'monthly'"
          >
            <div class="flex items-center justify-between">
              <span class="font-semibold text-foreground text-sm"
                >Gói 1 Tháng</span
              >
              <span class="font-display text-base font-bold text-foreground"
                >59.000đ</span
              >
            </div>
            <p class="mt-1 text-[11px] text-muted-foreground">
              Thanh toán theo từng tháng linh hoạt.
            </p>
          </div>
        </div>

        <!-- Submit Button -->
        <button
          class="mt-6 flex w-full items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 py-3.5 text-xs font-bold text-black shadow-lg shadow-amber-500/25 transition hover:brightness-110 active:scale-[0.99]"
          @click="proceedToPayment"
        >
          <span>Tiếp tục thanh toán VietQR</span>
        </button>
      </div>

      <!-- STEP 2: VIETQR PAYMENT -->
      <div v-else-if="step === 'payment'">
        <div class="text-center">
          <h2 class="font-display text-lg font-bold text-foreground">
            Quét mã VietQR (Napas 247)
          </h2>
          <p class="text-xs text-muted-foreground">
            Mở ứng dụng ngân hàng bất kỳ để quét mã thanh toán tự động
          </p>
        </div>

        <!-- VietQR Image -->
        <div class="mt-4 flex flex-col items-center">
          <div
            class="rounded-2xl border border-white/15 bg-white p-2.5 shadow-xl"
          >
            <img
              :src="bankInfo.qrUrl"
              alt="VietQR Code"
              class="size-52 object-contain"
            />
          </div>
          <div class="mt-2 flex items-center gap-1.5 text-xs text-amber-400">
            <span class="size-2 rounded-full bg-amber-400 animate-pulse" />
            <span>Thời gian giữ mã: {{ formatTimer(timerSeconds) }}</span>
          </div>
        </div>

        <!-- Bank Details -->
        <div
          class="mt-4 space-y-2 rounded-2xl bg-surface-container p-3 text-xs"
        >
          <div class="flex items-center justify-between">
            <span class="text-muted-foreground">Ngân hàng:</span>
            <span class="font-medium text-foreground">{{
              bankInfo.bankName
            }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-muted-foreground">Số tài khoản:</span>
            <div
              class="flex items-center gap-1.5 font-mono font-semibold text-foreground"
            >
              <span>{{ bankInfo.accountNumber }}</span>
              <button class="text-primary hover:opacity-80" @click="copyStk">
                <component :is="isCopiedStk ? Check : Copy" class="size-3.5" />
              </button>
            </div>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-muted-foreground">Chủ tài khoản:</span>
            <span class="font-medium text-foreground">{{
              bankInfo.accountName
            }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-muted-foreground">Nội dung chuyển khoản:</span>
            <div
              class="flex items-center gap-1.5 font-mono font-bold text-amber-400"
            >
              <span>{{ bankInfo.orderCode }}</span>
              <button
                class="text-primary hover:opacity-80"
                @click="copyContent"
              >
                <component
                  :is="isCopiedContent ? Check : Copy"
                  class="size-3.5"
                />
              </button>
            </div>
          </div>
        </div>

        <!-- Confirm Button -->
        <button
          class="mt-5 flex w-full items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 py-3.5 text-xs font-bold text-black shadow-lg shadow-amber-500/25 transition hover:brightness-110 active:scale-[0.99]"
          @click="confirmPayment"
        >
          <span>Tôi đã chuyển khoản xong</span>
        </button>
      </div>

      <!-- STEP 3: CONFIRMED -->
      <div v-else class="py-8 text-center">
        <div
          class="mx-auto grid size-16 place-items-center rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30"
        >
          <CheckCircle2 class="size-8" />
        </div>
        <h3 class="mt-4 font-display text-lg font-bold text-foreground">
          Thanh toán thành công!
        </h3>
        <p class="mt-1 text-xs text-muted-foreground">
          Tài khoản của bạn đã được nâng cấp lên VIP. Chúc bạn xem phim vui vẻ!
        </p>
      </div>
    </div>
  </div>
</template>
