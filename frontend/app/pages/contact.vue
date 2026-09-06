<script setup lang="ts">
import {
  CheckCircle2,
  Mail,
  MessageSquare,
  Send,
  ShieldAlert,
  Sparkles,
} from "@lucide/vue";

useHead({ title: "Liên hệ & Hỗ trợ — ZMovie" });

const name = ref("");
const email = ref("");
const category = ref("feedback");
const message = ref("");
const sending = ref(false);
const submitted = ref(false);

function submitContact() {
  if (
    !name.value.trim() ||
    !email.value.trim() ||
    !message.value.trim() ||
    sending.value
  )
    return;
  sending.value = true;
  setTimeout(() => {
    sending.value = false;
    submitted.value = true;
    name.value = "";
    email.value = "";
    message.value = "";
  }, 600);
}
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar />

    <section class="border-b border-white/7 bg-surface-container-low">
      <div class="mx-auto max-w-360 px-5 py-16 lg:px-12 lg:py-24">
        <span
          class="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3.5 py-1 text-xs font-bold text-primary"
        >
          <Sparkles class="size-3.5" /> Hỗ trợ 24/7
        </span>
        <h1
          class="font-display mt-5 max-w-3xl text-4xl font-extrabold leading-tight tracking-tight sm:text-5xl lg:text-6xl"
        >
          Liên hệ & Đóng góp ý kiến
        </h1>
        <p
          class="mt-6 max-w-2xl text-base leading-relaxed text-muted-foreground sm:text-lg"
        >
          Chúng tôi luôn sẵn sàng lắng nghe mọi phản hồi, báo lỗi sự cố nguồn
          phát và đề xuất tính năng để hoàn thiện ZMovie tốt hơn mỗi ngày.
        </p>
      </div>
    </section>

    <section class="mx-auto max-w-360 px-5 py-16 lg:px-12 lg:py-20">
      <div class="grid gap-12 lg:grid-cols-[1fr_28rem]">
        <div>
          <h2 class="font-display text-2xl font-bold tracking-tight">
            Gửi tin nhắn cho ban quản trị
          </h2>
          <p class="mt-2 text-sm text-muted-foreground">
            Điền thông tin vào mẫu bên dưới, chúng tôi sẽ phản hồi qua email sớm
            nhất.
          </p>

          <div
            v-if="submitted"
            class="mt-8 rounded-3xl border border-emerald-500/30 bg-emerald-500/10 p-8 text-center"
          >
            <CheckCircle2 class="mx-auto size-12 text-emerald-400" />
            <h3 class="mt-4 font-display text-xl font-bold text-emerald-200">
              Cảm ơn bạn đã liên hệ!
            </h3>
            <p class="mt-2 text-sm text-emerald-300/80">
              Tin nhắn của bạn đã được ghi nhận. Chúng tôi sẽ xử lý và phản hồi
              trong thời gian sớm nhất.
            </p>
            <button
              type="button"
              class="mt-6 rounded-xl bg-primary px-6 py-2.5 text-xs font-bold text-primary-foreground"
              @click="submitted = false"
            >
              Gửi tin nhắn khác
            </button>
          </div>

          <form v-else class="mt-8 space-y-5" @submit.prevent="submitContact">
            <div class="grid gap-5 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-xs font-semibold text-white/70"
                  >Họ và tên</span
                >
                <input
                  v-model="name"
                  type="text"
                  required
                  placeholder="Tên của bạn"
                  class="h-12 w-full rounded-xl border border-white/10 bg-white/5 px-4 text-sm outline-none transition focus:border-primary/60"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold text-white/70"
                  >Địa chỉ email</span
                >
                <input
                  v-model="email"
                  type="email"
                  required
                  placeholder="you@example.com"
                  class="h-12 w-full rounded-xl border border-white/10 bg-white/5 px-4 text-sm outline-none transition focus:border-primary/60"
                />
              </label>
            </div>

            <label class="block">
              <span class="mb-2 block text-xs font-semibold text-white/70"
                >Chủ đề liên hệ</span
              >
              <select
                v-model="category"
                class="h-12 w-full rounded-xl border border-white/10 bg-[#171717] px-4 text-sm outline-none transition focus:border-primary/60"
              >
                <option value="feedback">
                  Góp ý cải tiến giao diện / tính năng
                </option>
                <option value="broken_stream">
                  Báo lỗi phim không xem được
                </option>
                <option value="copyright">Vấn đề bản quyền / DMCA</option>
                <option value="cooperation">Hợp tác nội dung / quảng bá</option>
                <option value="other">Vấn đề khác</option>
              </select>
            </label>

            <label class="block">
              <span class="mb-2 block text-xs font-semibold text-white/70"
                >Nội dung chi tiết</span
              >
              <textarea
                v-model="message"
                required
                rows="5"
                placeholder="Mô tả cụ thể vấn đề hoặc ý kiến đóng góp của bạn..."
                class="w-full rounded-2xl border border-white/10 bg-white/5 p-4 text-sm outline-none transition focus:border-primary/60"
              />
            </label>

            <button
              type="submit"
              class="inline-flex items-center gap-2 rounded-xl bg-primary px-8 py-3.5 text-sm font-bold text-primary-foreground shadow-lg transition hover:brightness-110 disabled:opacity-50"
              :disabled="sending"
            >
              <Send class="size-4" />
              <span>{{ sending ? "Đang gửi..." : "Gửi tin nhắn" }}</span>
            </button>
          </form>
        </div>

        <!-- Contact Cards Side Column -->
        <aside class="space-y-4">
          <div class="rounded-3xl border border-white/8 bg-[#171717] p-6">
            <div
              class="grid size-11 place-items-center rounded-xl bg-primary/10 text-primary"
            >
              <Mail class="size-5" />
            </div>
            <h3 class="mt-4 font-bold">Hỗ trợ người dùng</h3>
            <p class="mt-1 text-xs text-muted-foreground">
              Giải đáp thắc mắc về tài khoản, tính năng và hướng dẫn xem phim.
            </p>
            <a
              href="mailto:support@zmovie.local"
              class="mt-3 inline-block font-mono text-xs font-semibold text-primary hover:underline"
            >
              support@zmovie.local
            </a>
          </div>

          <div class="rounded-3xl border border-white/8 bg-[#171717] p-6">
            <div
              class="grid size-11 place-items-center rounded-xl bg-rose-500/10 text-rose-400"
            >
              <ShieldAlert class="size-5" />
            </div>
            <h3 class="mt-4 font-bold">Khiếu nại bản quyền</h3>
            <p class="mt-1 text-xs text-muted-foreground">
              Đầu mối tiếp nhận văn bản yêu cầu gỡ bỏ liên kết vi phạm DMCA.
            </p>
            <a
              href="mailto:dmca@zmovie.local"
              class="mt-3 inline-block font-mono text-xs font-semibold text-rose-400 hover:underline"
            >
              dmca@zmovie.local
            </a>
          </div>

          <div class="rounded-3xl border border-white/8 bg-[#171717] p-6">
            <div
              class="grid size-11 place-items-center rounded-xl bg-amber-500/10 text-amber-400"
            >
              <MessageSquare class="size-5" />
            </div>
            <h3 class="mt-4 font-bold">Hợp tác & Đối tác</h3>
            <p class="mt-1 text-xs text-muted-foreground">
              Liên hệ hợp tác phân phối, CDN nguồn phát và phát triển cộng đồng.
            </p>
            <a
              href="mailto:partner@zmovie.local"
              class="mt-3 inline-block font-mono text-xs font-semibold text-amber-300 hover:underline"
            >
              partner@zmovie.local
            </a>
          </div>
        </aside>
      </div>
    </section>

    <AppFooter />
  </main>
</template>
