<script setup lang="ts">
import { ArrowLeft, CheckCircle2, Film, LockKeyhole } from "@lucide/vue";
import { resetPassword } from "~/services/auth.service";

useHead({ title: "Đặt lại mật khẩu — ZMovie" });

const route = useRoute();
const router = useRouter();

const login = ref(String(route.query.login || ""));
const token = ref(String(route.query.token || ""));
const password = ref("");
const confirmPassword = ref("");
const loading = ref(false);
const success = ref(false);
const error = ref("");

async function submitReset() {
  if (password.value !== confirmPassword.value) {
    error.value = "Mật khẩu xác nhận không khớp.";
    return;
  }
  if (!login.value || !token.value) {
    error.value = "Liên kết đặt lại mật khẩu không hợp lệ.";
    return;
  }
  loading.value = true;
  error.value = "";
  try {
    await resetPassword(login.value, token.value, password.value);
    success.value = true;
    setTimeout(() => {
      void router.push("/login");
    }, 2500);
  } catch {
    error.value =
      "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu lại.";
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
        <ArrowLeft class="size-4" /> Về đăng nhập
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
          Bảo mật tài khoản với mật khẩu mới.
        </p>
      </div>
    </section>

    <section
      class="relative flex min-h-screen items-center justify-center px-5 py-12 sm:px-8 lg:px-12"
    >
      <div class="w-full max-w-md">
        <div
          class="rounded-3xl border border-white/7 bg-[#201f1f] p-6 shadow-2xl shadow-black/25 sm:p-9"
        >
          <p class="text-sm font-medium tracking-[.18em] text-[#d98367]">
            BẢO MẬT TÀI KHOẢN
          </p>
          <h1
            class="font-display mt-3 text-3xl font-semibold tracking-[-.035em] text-[#e5e2e1]"
          >
            Đặt lại mật khẩu
          </h1>
          <p class="mt-3 leading-relaxed text-white/55">
            Nhập mật khẩu mới ít nhất 8 ký tự cho tài khoản {{ login }}.
          </p>

          <div
            v-if="success"
            class="mt-7 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-5 text-sm leading-relaxed text-emerald-200"
          >
            <div class="flex items-center gap-2 font-semibold">
              <CheckCircle2 class="size-5 text-emerald-400" />
              Mật khẩu đã được cập nhật thành công!
            </div>
            <p class="mt-2 text-xs text-emerald-300/80">
              Đang chuyển hướng về trang đăng nhập...
            </p>
          </div>

          <form v-else class="mt-8 space-y-5" @submit.prevent="submitReset">
            <label class="block">
              <span class="mb-2 block text-sm font-medium text-white/80"
                >Mật khẩu mới</span
              >
              <span
                class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"
              >
                <LockKeyhole class="size-4 shrink-0 text-white/35" />
                <input
                  v-model="password"
                  type="password"
                  required
                  minlength="8"
                  autocomplete="new-password"
                  placeholder="Ít nhất 8 ký tự"
                  class="h-12 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28"
                />
              </span>
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-medium text-white/80"
                >Xác nhận mật khẩu</span
              >
              <span
                class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"
              >
                <LockKeyhole class="size-4 shrink-0 text-white/35" />
                <input
                  v-model="confirmPassword"
                  type="password"
                  required
                  minlength="8"
                  autocomplete="new-password"
                  placeholder="Nhập lại mật khẩu mới"
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
              {{ loading ? "Đang lưu..." : "Đổi mật khẩu" }}
            </button>
          </form>
        </div>
      </div>
    </section>
  </main>
</template>
