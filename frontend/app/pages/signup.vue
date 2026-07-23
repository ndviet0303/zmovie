<script setup lang="ts">
import { ArrowLeft, Eye, EyeOff, Film, LockKeyhole, Mail, UserRound } from '@lucide/vue'

useHead({ title: 'Đăng ký — ZMovie' })

const name = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const acceptedTerms = ref(false)
const showPassword = ref(false)
const showConfirmation = ref(false)
const notice = ref('')

function submitSignup() {
  if (password.value !== confirmPassword.value) {
    notice.value = 'Mật khẩu xác nhận chưa khớp.'
    return
  }
  notice.value = 'Chức năng tạo tài khoản đang được kết nối. Bạn có thể tiếp tục khám phá ZMovie ngay bây giờ.'
}

</script>

<template>
  <main class="min-h-screen bg-[#131313] text-[#e5e2e1] lg:grid lg:grid-cols-[minmax(0,1.45fr)_minmax(26rem,1fr)]">
    <section class="relative hidden overflow-hidden lg:block">
      <div class="absolute inset-0 bg-[radial-gradient(circle_at_18%_25%,rgba(255,181,157,.25),transparent_30%),radial-gradient(circle_at_75%_72%,rgba(217,131,103,.2),transparent_28%),linear-gradient(135deg,#151211_0%,#2f1d1b_48%,#121212_100%)]" />
      <div class="absolute inset-0 opacity-40 [background-image:linear-gradient(115deg,transparent_0%,rgba(255,255,255,.08)_38%,transparent_42%),repeating-linear-gradient(105deg,rgba(255,255,255,.025)_0_1px,transparent_1px_9px)]" />
      <div class="absolute inset-0 bg-gradient-to-r from-black/15 via-transparent to-[#131313]/55" />
      <NuxtLink to="/" class="absolute left-10 top-10 inline-flex items-center gap-2 text-sm text-white/70 transition hover:text-[#ffb59d]"><ArrowLeft class="size-4" /> Về trang chủ</NuxtLink>
      <div class="relative flex h-full flex-col justify-end p-16 xl:p-24">
        <div class="mb-5 inline-flex size-14 items-center justify-center rounded-2xl border border-[#ffb59d]/30 bg-[#ffb59d]/10 text-[#ffb59d] shadow-[0_0_48px_rgba(217,131,103,.22)]"><Film class="size-7" /></div>
        <p class="font-display text-6xl font-semibold tracking-[-.05em] text-[#ffb59d] xl:text-7xl">ZMovie</p>
        <p class="mt-5 max-w-md text-lg leading-relaxed text-white/65">Lưu những thế giới điện ảnh bạn yêu thích, ở một nơi.</p>
      </div>
    </section>

    <section class="relative flex min-h-screen items-center justify-center px-5 py-12 sm:px-8 lg:px-12">
      <NuxtLink to="/" class="absolute left-5 top-6 inline-flex items-center gap-2 text-sm text-white/60 transition hover:text-[#ffb59d] lg:hidden"><ArrowLeft class="size-4" /> Về trang chủ</NuxtLink>
      <div class="w-full max-w-md">
        <NuxtLink to="/" class="mb-10 flex items-center gap-3 lg:hidden"><span class="inline-flex size-10 items-center justify-center rounded-xl bg-[#ffb59d]/15 text-[#ffb59d]"><Film class="size-5" /></span><span class="font-display text-2xl font-semibold text-[#ffb59d]">ZMovie</span></NuxtLink>
        <div class="rounded-3xl border border-white/7 bg-[#201f1f] p-6 shadow-2xl shadow-black/25 sm:p-9">
          <p class="text-sm font-medium tracking-[.18em] text-[#d98367]">ZMOVIE PREMIUM</p>
          <h1 class="font-display mt-3 text-4xl font-semibold tracking-[-.035em] text-[#e5e2e1]">Tạo tài khoản</h1>
          <p class="mt-3 leading-relaxed text-white/55">Bắt đầu hành trình điện ảnh được tuyển chọn riêng cho bạn.</p>

          <form class="mt-7 space-y-4" @submit.prevent="submitSignup">
            <label class="block"><span class="mb-2 block text-sm font-medium text-white/80">Họ và tên</span><span class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"><UserRound class="size-4 shrink-0 text-white/35" /><input v-model="name" required autocomplete="name" placeholder="Tên của bạn" class="h-11 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28" /></span></label>
            <label class="block"><span class="mb-2 block text-sm font-medium text-white/80">Email</span><span class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"><Mail class="size-4 shrink-0 text-white/35" /><input v-model="email" type="email" required autocomplete="email" placeholder="you@example.com" class="h-11 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28" /></span></label>
            <label class="block"><span class="mb-2 block text-sm font-medium text-white/80">Mật khẩu</span><span class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"><LockKeyhole class="size-4 shrink-0 text-white/35" /><input v-model="password" :type="showPassword ? 'text' : 'password'" required minlength="8" autocomplete="new-password" placeholder="Ít nhất 8 ký tự" class="h-11 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28" /><button type="button" class="text-white/40 transition hover:text-[#ffb59d]" :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" @click="showPassword = !showPassword"><EyeOff v-if="showPassword" class="size-4" /><Eye v-else class="size-4" /></button></span></label>
            <label class="block"><span class="mb-2 block text-sm font-medium text-white/80">Xác nhận mật khẩu</span><span class="flex items-center gap-3 rounded-xl border border-white/10 bg-black/15 px-4 transition focus-within:border-[#d98367] focus-within:ring-2 focus-within:ring-[#d98367]/20"><LockKeyhole class="size-4 shrink-0 text-white/35" /><input v-model="confirmPassword" :type="showConfirmation ? 'text' : 'password'" required autocomplete="new-password" placeholder="Nhập lại mật khẩu" class="h-11 w-full bg-transparent text-sm text-white outline-none placeholder:text-white/28" /><button type="button" class="text-white/40 transition hover:text-[#ffb59d]" :aria-label="showConfirmation ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" @click="showConfirmation = !showConfirmation"><EyeOff v-if="showConfirmation" class="size-4" /><Eye v-else class="size-4" /></button></span></label>
            <label class="flex cursor-pointer items-start gap-2.5 pt-1 text-sm leading-5 text-white/55"><input v-model="acceptedTerms" required type="checkbox" class="peer sr-only" /><span class="mt-0.5 grid size-4 shrink-0 place-items-center rounded-[5px] border border-white/25 bg-black/20 text-[11px] font-black leading-none text-[#34201b] transition after:content-[''] peer-checked:border-[#ffb59d] peer-checked:bg-[#ffb59d] peer-checked:after:content-['✓'] peer-focus-visible:ring-2 peer-focus-visible:ring-[#ffb59d]/60 peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-[#201f1f]" /><span>Tôi đồng ý với <NuxtLink to="/terms" class="font-medium text-[#ffb59d] hover:text-[#d98367]">Điều khoản</NuxtLink> và <NuxtLink to="/privacy" class="font-medium text-[#ffb59d] hover:text-[#d98367]">Chính sách bảo mật</NuxtLink>.</span></label>
            <button type="submit" class="flex h-12 w-full items-center justify-center rounded-xl bg-[#ffb59d] text-sm font-semibold text-[#34201b] transition hover:bg-[#d98367] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#ffb59d]">Tạo tài khoản</button>
          </form>

          <div class="my-6 flex items-center gap-3 text-xs text-white/35"><span class="h-px flex-1 bg-white/10" />hoặc<span class="h-px flex-1 bg-white/10" /></div>
          <GoogleSignInButton text="signup_with" />
          <p v-if="notice" class="mt-5 rounded-xl bg-[#ffb59d]/10 px-4 py-3 text-sm leading-relaxed text-[#ffcfbf]" role="status">{{ notice }}</p>
          <p class="mt-7 text-center text-sm text-white/50">Đã có tài khoản? <NuxtLink to="/login" class="font-semibold text-[#ffb59d] transition hover:text-[#d98367]">Đăng nhập</NuxtLink></p>
        </div>
      </div>
    </section>
  </main>
</template>
