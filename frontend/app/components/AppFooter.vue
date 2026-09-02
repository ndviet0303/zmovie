<script setup lang="ts">
import { Send } from "@lucide/vue";

const socialLinks = [
  { name: "Telegram", icon: "telegram", href: "https://t.me/motchilltv" },
  { name: "Discord", icon: "discord", href: "#" },
  { name: "X", icon: "x", href: "#" },
  { name: "Facebook", icon: "facebook", href: "#" },
  { name: "TikTok", icon: "tiktok", href: "#" },
  { name: "YouTube", icon: "youtube", href: "#" },
  { name: "Instagram", icon: "instagram", href: "#" },
];

const quickLinks = [
  { label: "Hồi-Đáp", to: "/privacy" },
  { label: "Chính sách bảo mật", to: "/privacy" },
  { label: "Điều khoản sử dụng", to: "/terms" },
  { label: "Giới thiệu", to: "/privacy" },
  { label: "Liên hệ", to: "https://t.me/motchilltv", external: true },
];
</script>

<template>
  <footer
    class="border-t border-white/5 bg-[#0a0c12] px-4 py-12 text-white/80 sm:px-6 lg:px-12"
  >
    <div class="mx-auto flex max-w-360 flex-col items-center text-center">
      <!-- Sovereignty Red Banner -->
      <div
        class="inline-flex items-center gap-2 rounded-full border border-red-500/30 bg-red-600/20 px-5 py-2 text-xs font-bold text-red-400 shadow-sm transition hover:bg-red-600/25"
      >
        <span class="text-amber-400">★</span>
        <span>Hoàng Sa & Trường Sa là của Việt Nam!</span>
      </div>

      <!-- Brand Logo & Tagline -->
      <div class="mt-8 flex items-center justify-center">
        <ZMovieLogo />
      </div>

      <!-- Social Media Icons Row -->
      <div class="mt-6 flex flex-wrap items-center justify-center gap-3">
        <!-- Telegram -->
        <a
          href="https://t.me/motchilltv"
          target="_blank"
          rel="noopener noreferrer"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="Telegram"
        >
          <Send class="size-4" />
        </a>
        <!-- Discord -->
        <a
          href="#"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="Discord"
        >
          <svg class="size-4 fill-current" viewBox="0 0 24 24">
            <path
              d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994.021-.041.001-.09-.041-.106a13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.929 1.793 8.18 1.793 12.061 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.894.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.028zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z"
            />
          </svg>
        </a>
        <!-- X (Twitter) -->
        <a
          href="#"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="X"
        >
          <svg class="size-3.5 fill-current" viewBox="0 0 24 24">
            <path
              d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"
            />
          </svg>
        </a>
        <!-- Facebook -->
        <a
          href="#"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="Facebook"
        >
          <svg class="size-4 fill-current" viewBox="0 0 24 24">
            <path
              d="M9 8H6v4h3v12h5V12h3.642L18 8h-4V6.333C14 5.374 14.5 5 15.5 5H18V0h-3.808C10.595 0 9 1.582 9 4.615V8z"
            />
          </svg>
        </a>
        <!-- TikTok -->
        <a
          href="#"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="TikTok"
        >
          <svg class="size-4 fill-current" viewBox="0 0 24 24">
            <path
              d="M12.525.02c1.31-.02 2.61-.01 3.91-.02.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-.93-.01 2.92.01 5.84-.02 8.75-.08 1.4-.54 2.79-1.35 3.94-1.31 1.92-3.58 3.17-5.91 3.21-1.43.08-2.86-.31-4.08-1.03-2.02-1.19-3.44-3.37-3.65-5.71-.02-.5-.03-1-.01-1.49.18-1.9 1.12-3.72 2.58-4.96 1.66-1.44 3.98-2.13 6.15-1.72.02 1.48-.04 2.96-.04 4.44-.99-.32-2.15-.23-3.02.37-.63.41-1.11 1.04-1.27 1.76-.23 1.05.14 2.2.93 2.9.72.67 1.76.95 2.73.74.88-.17 1.62-.76 2.01-1.55.22-.51.29-1.08.28-1.64V.02h.01z"
            />
          </svg>
        </a>
        <!-- YouTube -->
        <a
          href="#"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-white/5 text-white/80 transition hover:border-primary hover:bg-primary/15 hover:text-primary"
          title="YouTube"
        >
          <svg class="size-4 fill-current" viewBox="0 0 24 24">
            <path
              d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"
            />
          </svg>
        </a>
      </div>

      <!-- Quick Links -->
      <nav
        class="mt-6 flex flex-wrap items-center justify-center gap-4 text-xs font-semibold text-white/70"
      >
        <template v-for="(link, i) in quickLinks" :key="link.label">
          <NuxtLink
            v-if="!link.external"
            :to="link.to"
            class="transition hover:text-primary"
          >
            {{ link.label }}
          </NuxtLink>
          <a
            v-else
            :href="link.to"
            target="_blank"
            rel="noopener noreferrer"
            class="transition hover:text-primary"
          >
            {{ link.label }}
          </a>
          <span v-if="i < quickLinks.length - 1" class="text-white/20">·</span>
        </template>
      </nav>

      <!-- Description / Disclaimer -->
      <p
        class="mt-6 max-w-2xl text-xs leading-relaxed text-muted-foreground/80"
      >
        ZMovie - Trang xem phim online chất lượng cao miễn phí Vietsub, thuyết
        minh, lồng tiếng full HD. Kho phim mới khổng lồ, phim chiếu rạp, phim
        bộ, phim lẻ từ nhiều quốc gia như Việt Nam, Hàn Quốc, Trung Quốc, Thái
        Lan, Nhật Bản, Âu Mỹ... đa dạng thể loại. Khám phá nền tảng phim trực
        tuyến hay nhất 2026 chất lượng 4K!
      </p>

      <!-- Telegram Contact & Copyright -->
      <div
        class="mt-5 flex flex-col items-center gap-1.5 text-xs text-white/60"
      >
        <p class="flex items-center gap-1.5 font-medium">
          <Send class="size-3.5 text-primary" />
          <span>Liên hệ Telegram:</span>
          <a
            href="https://t.me/motchilltv"
            target="_blank"
            rel="noopener noreferrer"
            class="font-bold text-primary hover:underline"
          >
            motchilltv
          </a>
        </p>
        <p class="text-[11px] text-muted-foreground/60">
          © 2025 - 2026 ZMovie. All rights reserved.
        </p>
      </div>
    </div>
  </footer>
</template>
