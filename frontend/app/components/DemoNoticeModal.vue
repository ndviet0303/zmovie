<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { AlertTriangle, CheckCircle2, ShieldAlert, X } from "@lucide/vue";

const STORAGE_KEY = "zmovie_demo_modal_acknowledged";
const isOpen = ref(false);

function closeModal() {
  isOpen.value = false;
  try {
    localStorage.setItem(STORAGE_KEY, "true");
  } catch {
    // ignore storage quota / access errors
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === "Escape" && isOpen.value) {
    closeModal();
  }
}

onMounted(() => {
  try {
    const acknowledged = localStorage.getItem(STORAGE_KEY);
    if (!acknowledged) {
      setTimeout(() => {
        isOpen.value = true;
      }, 400);
    }
  } catch {
    isOpen.value = true;
  }
  window.addEventListener("keydown", onKeydown);
});

onUnmounted(() => {
  window.removeEventListener("keydown", onKeydown);
});
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-[100] flex items-center justify-center bg-black/80 p-4 backdrop-blur-md"
        role="dialog"
        aria-modal="true"
        aria-labelledby="demo-notice-title"
        @click.self="closeModal"
      >
        <Transition
          enter-active-class="transition duration-300 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="transition duration-200 ease-in"
          leave-from-class="opacity-100 scale-100 translate-y-0"
          leave-to-class="opacity-0 scale-95 translate-y-4"
        >
          <div
            v-if="isOpen"
            class="relative w-full max-w-lg overflow-hidden rounded-3xl border border-white/15 bg-gradient-to-b from-[#241a18] via-[#1a1413] to-[#14100f] p-6 sm:p-8 shadow-2xl shadow-black/80"
          >
            <!-- Decorative background accent glow -->
            <div
              class="pointer-events-none absolute -right-16 -top-16 size-48 rounded-full bg-primary/20 blur-3xl"
            />
            <div
              class="pointer-events-none absolute -bottom-16 -left-16 size-48 rounded-full bg-amber-500/10 blur-3xl"
            />

            <!-- Top header row with Badge & Close button -->
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span
                  class="inline-flex items-center gap-1.5 rounded-full border border-amber-400/40 bg-gradient-to-r from-amber-500/25 to-orange-500/20 px-3 py-1 text-[11px] font-extrabold uppercase tracking-wider text-amber-300 shadow-sm"
                >
                  <AlertTriangle class="size-3.5 text-amber-400" />
                  DEMO ONLY
                </span>
                <span class="text-xs font-medium text-white/50"
                  >Thông báo trải nghiệm</span
                >
              </div>
              <button
                class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/60 transition hover:border-white/30 hover:bg-white/10 hover:text-white"
                aria-label="Đóng thông báo"
                @click="closeModal"
              >
                <X class="size-4" />
              </button>
            </div>

            <!-- Title & description -->
            <div class="mt-5">
              <h2
                id="demo-notice-title"
                class="font-display text-xl font-bold tracking-tight text-white sm:text-2xl"
              >
                Website Demo — Không Phải Dịch Vụ Thật
              </h2>
              <p
                class="mt-3 text-sm leading-relaxed text-white/70 sm:text-base"
              >
                Chào mừng bạn đến với ZMovie! Trang web này được xây dựng hoàn
                toàn phục vụ mục đích nghiên cứu công nghệ, thử nghiệm tính năng
                và trình diễn giao diện.
              </p>
            </div>

            <!-- Highlights / bullets -->
            <div
              class="mt-5 space-y-2.5 rounded-2xl border border-white/10 bg-black/30 p-4"
            >
              <div
                class="flex items-start gap-3 text-xs text-white/80 sm:text-sm"
              >
                <CheckCircle2 class="mt-0.5 size-4 shrink-0 text-primary" />
                <span
                  >Không sử dụng cho giao dịch, thanh toán hay thu phí thực
                  tế.</span
                >
              </div>
              <div
                class="flex items-start gap-3 text-xs text-white/80 sm:text-sm"
              >
                <CheckCircle2 class="mt-0.5 size-4 shrink-0 text-primary" />
                <span
                  >Nội dung phim và dữ liệu phục vụ mục đích kiểm thử hệ
                  thống.</span
                >
              </div>
              <div
                class="flex items-start gap-3 text-xs text-white/80 sm:text-sm"
              >
                <ShieldAlert class="mt-0.5 size-4 shrink-0 text-amber-400" />
                <span
                  >Không nhập thông tin thanh toán hoặc mật khẩu nhạy cảm của
                  bạn.</span
                >
              </div>
            </div>

            <!-- Action Button -->
            <div class="mt-6 flex flex-col gap-2.5 sm:flex-row">
              <button
                type="button"
                class="inline-flex h-12 flex-1 items-center justify-center rounded-2xl bg-gradient-to-r from-primary to-primary/90 px-6 text-sm font-semibold text-primary-container-foreground shadow-lg shadow-primary/25 transition hover:opacity-95 hover:scale-[1.01] active:scale-[0.99]"
                @click="closeModal"
              >
                Tôi đã hiểu & Tiếp tục
              </button>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
