<script setup lang="ts">
import { ArrowLeft, Film, Mail } from "@lucide/vue";
import { requestPasswordReset } from "~/services/auth.service";

useHead({ title: "Quên mật khẩu — ZMovie" });

const email = ref("");
const loading = ref(false);
const submitted = ref(false);
const error = ref("");

async function submitForgot() {
  if (!email.value.trim() || loading.value) return;
  loading.value = true;
  error.value = "";
  try {
    await requestPasswordReset(email.value.trim());
    submitted.value = true;
  } catch {
    error.value = "Không thể gửi yêu cầu. Vui lòng thử lại sau.";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <main
    class="min-h-screen bg-[#131313] text-[#e5e2e1] lg:grid lg:grid-cols-[minmax(0,1.45fr)_minmax(26rem,1fr)]"
  >
    <section class="relative hidden overflow-hidden lg:block">
      <div
        class="absolute inset-0 bg-[radial-gradient(circle_at_18%_25%,rgba(255,181,157,.25),transparent_30%),radial-gradient(circle_at_75%_72%,rgba(217,131,103,.2),transparent_28%),linear-gradient(135deg,#151211_0%,#2f1d1b_48%,#121212_100%)]"
      />
      <div
        class="absolute inset-0 opacity-40 [background-image:linear-gradient(115deg,transparent_0%,rgba(255,255,255,.08)_38%,transparent_42%),repeating-linear-gradient(105deg,rgba(255,255,255,.025)_0_1px,transparent_1px_9px)]"
      />
      <div
        class="absolute inset-0 bg-gradient-to-r from-black/15 via-transparent to-[#131313]/55"
      />

      <NuxtLink
        to="/login"
        class="absolute left-10 top-10 inline-flex items-center gap-2 text-sm text-white/70 transition hover:text-[#ffb59d]"
      >
        <ArrowLeft class="size-4" /> Quay lại đăng nhập
      </NuxtLink>

      <div class="relative flex h-full flex-col justify-end p-16 xl:p-24">
        <div
          class="mb-5 inline-flex size-14 items-center justify-center rounded-2xl border border-[#ffb59d]/30 bg-[#ffb59d]/10 text-[#ffb59d] shadow-[0_0_48px_rgba(217,131,103,.22)]"
        >
          <Film class="size-7" />
        </div>
        <p
          class="font-display text-6xl font-semibold tracking-[-.05em] text-[#ffb59d] xl:text-7xl"
        >
          ZMovie
        </p>
        <p class="mt-5 max-w-md text-lg leading-relaxed text-white/65">
          Khôi phục quyền truy cập vào danh sách phim và thế giới điện ảnh của
          bạn.
        </p>
      </div>
    </section>

    <section
      class="relative flex min-h-screen items-center justify-center px-5 py-12 sm:px-8 lg:px-12"
    >
      <NuxtLink
        to="/login"
        class="absolute left-5 top-6 inline-flex items-center gap-2 text-sm text-white/60 transition hover:text-[#ffb59d] lg:hidden"
      >
        <ArrowLeft class="size-4" /> Quay lại đăng nhập
      </NuxtLink>

      <div class="w-full max-w-md">
        <div
          class="rounded-3xl border border-white/7 bg-[#201f1f] p-6 shadow-2xl shadow-black/25 sm:p-9"
        >
          <p class="text-sm font-medium tracking-[.18em] text-[#d98367]">
            KHÔI PHỤC TÀI KHOẢN
          </p>
          <h1
            class="font-display mt-3 text-3xl font-semibold tracking-[-.035em] text-[#e5e2e1]"
          >
            Quên mật khẩu?
          </h1>
          <p class="mt-3 leading-relaxed text-white/55">
            Nhập email đã đăng ký tài khoản. Chúng tôi sẽ gửi hướng dẫn đặt lại
            mật khẩu nếu tài khoản tồn tại.
          </p>

          <div
            v-if="submitted"
            class="mt-7 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-5 text-sm leading-relaxed text-emerald-200"
          >
            <p class="font-semibold">Đã gửi yêu cầu khôi phục!</p>
            <p class="mt-1 text-xs text-emerald-300/80">
              Nếu địa chỉ {{ email }} khớp với tài khoản, liên kết đặt lại mật
              khẩu đã được gửi và có hiệu lực trong 30 phút.
            </p>
            <NuxtLink
              to="/login"
              class="mt-4 inline-block font-semibold text-[#ffb59d] hover:text-[#d98367]"
            >
              Về trang đăng nhập
            </NuxtLink>
          </div>

          <form v-else class="mt-8 space-y-5" @submit.prevent="submitForgot">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-white/80"
                >Email</span
              >
              <span
                class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"
              >
                <Mail class="size-4 shrink-0 text-white/35" />
                <input
                  v-model="email"
                  type="email"
                  required
                  autocomplete="email"
                  placeholder="you@example.com"
                  class="h-12 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28"
                />
              </span>
            </label>

            <p
              v-if="error"
              class="rounded-xl bg-destructive/15 px-4 py-3 text-xs font-semibold text-destructive"
            >
              {{ error }}
            </p>

            <button
              type="submit"
              class="flex h-12 w-full items-center justify-center rounded-xl bg-[#ffb59d] text-sm font-semibold text-[#34201b] transition hover:bg-[#d98367] disabled:opacity-50"
              :disabled="loading"
            >
              {{ loading ? "Đang gửi..." : "Gửi liên kết khôi phục" }}
            </button>
          </form>

          <p class="mt-7 text-center text-sm text-white/50">
            Nhớ mật khẩu rồi?
            <NuxtLink
              to="/login"
              class="font-semibold text-[#ffb59d] transition hover:text-[#d98367]"
            >
              Đăng nhập
            </NuxtLink>
          </p>
        </div>
      </div>
    </section>
  </main>
</template>
