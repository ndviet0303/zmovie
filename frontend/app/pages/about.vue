<script setup lang="ts">
import {
  Clapperboard,
  Compass,
  ShieldCheck,
  Sparkles,
  UsersRound,
  Zap,
} from "@lucide/vue";

useHead({ title: "Giới thiệu — ZMovie" });
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar />

    <section class="border-b border-white/7 bg-surface-container-low">
      <div class="mx-auto max-w-360 px-5 py-16 lg:px-12 lg:py-24">
        <span
          class="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3.5 py-1 text-xs font-bold text-primary"
        >
          <Sparkles class="size-3.5" /> Về chúng tôi
        </span>
        <h1
          class="font-display mt-5 max-w-3xl text-4xl font-extrabold leading-tight tracking-tight sm:text-5xl lg:text-6xl"
        >
          Trải nghiệm điện ảnh đỉnh cao, tinh gọn và miễn phí
        </h1>
        <p
          class="mt-6 max-w-2xl text-base leading-relaxed text-muted-foreground sm:text-lg"
        >
          ZMovie được xây dựng với mục tiêu mang đến không gian thưởng thức phim
          trực tuyến hiện đại nhất: không quảng cáo chen ngang, tải nhanh vượt
          trội và hỗ trợ xem chung cùng bạn bè ở mọi nơi.
        </p>
      </div>
    </section>

    <section class="mx-auto max-w-360 px-5 py-16 lg:px-12 lg:py-20">
      <div class="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-3xl border border-white/8 bg-[#171717] p-7">
          <div
            class="grid size-12 place-items-center rounded-2xl bg-primary/10 text-primary"
          >
            <Zap class="size-6" />
          </div>
          <h3 class="mt-5 font-display text-lg font-bold">
            Tốc độ & Chất lượng
          </h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Phát mượt mà với HLS Adaptive Streaming và đường truyền CDN tối ưu
            hóa cho thị trường Việt Nam.
          </p>
        </div>

        <div class="rounded-3xl border border-white/8 bg-[#171717] p-7">
          <div
            class="grid size-12 place-items-center rounded-2xl bg-primary/10 text-primary"
          >
            <UsersRound class="size-6" />
          </div>
          <h3 class="mt-5 font-display text-lg font-bold">
            Xem chung tương tác
          </h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Phòng Watch Party thời gian thực với đồng bộ phát tua, tin nhắn trò
            chuyện và đạn mạc bay sinh động.
          </p>
        </div>

        <div class="rounded-3xl border border-white/8 bg-[#171717] p-7">
          <div
            class="grid size-12 place-items-center rounded-2xl bg-primary/10 text-primary"
          >
            <Compass class="size-6" />
          </div>
          <h3 class="mt-5 font-display text-lg font-bold">
            Khám phá thông minh
          </h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Bộ lọc phân loại đa chiều theo thể loại, quốc gia, năm phát hành
            cùng trợ lý AI gợi ý phim theo tâm trạng.
          </p>
        </div>

        <div class="rounded-3xl border border-white/8 bg-[#171717] p-7">
          <div
            class="grid size-12 place-items-center rounded-2xl bg-primary/10 text-primary"
          >
            <ShieldCheck class="size-6" />
          </div>
          <h3 class="mt-5 font-display text-lg font-bold">
            Minh bạch & An toàn
          </h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Tôn trọng bản quyền tác giả, bảo mật thông tin cá nhân và xử lý
            khiếu nại nội dung trong 24-48 giờ.
          </p>
        </div>
      </div>

      <div
        class="mt-16 rounded-3xl border border-white/10 bg-gradient-to-br from-[#1b1918] via-[#141312] to-background p-8 sm:p-12"
      >
        <div class="grid gap-8 lg:grid-cols-2 lg:items-center">
          <div>
            <span
              class="text-xs font-bold uppercase tracking-[.18em] text-primary"
              >Cam kết trải nghiệm</span
            >
            <h2 class="font-display mt-3 text-3xl font-bold tracking-tight">
              Được tạo ra bởi những người yêu điện ảnh
            </h2>
            <p class="mt-4 text-sm leading-relaxed text-muted-foreground">
              Chúng tôi hiểu sự ức chế khi xem phim bị quảng cáo độc hại che
              mắt, giật lag hay liên kết hỏng. ZMovie liên tục nâng cấp hệ thống
              nguồn phát dự phòng, tự động kiểm tra chất lượng tập phim và hỗ
              trợ báo lỗi nhanh để mỗi phút xem phim của bạn đều trọn vẹn.
            </p>
            <div class="mt-6 flex flex-wrap gap-4">
              <NuxtLink
                to="/browse"
                class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-3 text-xs font-bold text-primary-foreground transition hover:brightness-110"
              >
                <Clapperboard class="size-4" /> Khám phá kho phim
              </NuxtLink>
              <NuxtLink
                to="/party"
                class="inline-flex items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-5 py-3 text-xs font-bold transition hover:border-primary/50"
              >
                <UsersRound class="size-4" /> Thử phòng xem chung
              </NuxtLink>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4 text-center">
            <div class="rounded-2xl border border-white/8 bg-black/30 p-6">
              <p class="font-display text-4xl font-extrabold text-primary">
                100%
              </p>
              <p class="mt-1 text-xs text-muted-foreground">
                Tương thích mọi thiết bị
              </p>
            </div>
            <div class="rounded-2xl border border-white/8 bg-black/30 p-6">
              <p class="font-display text-4xl font-extrabold text-primary">
                4K / HD
              </p>
              <p class="mt-1 text-xs text-muted-foreground">
                Độ phân giải sắc nét
              </p>
            </div>
            <div class="rounded-2xl border border-white/8 bg-black/30 p-6">
              <p class="font-display text-4xl font-extrabold text-primary">
                24/7
              </p>
              <p class="mt-1 text-xs text-muted-foreground">
                Cập nhật liên tục
              </p>
            </div>
            <div class="rounded-2xl border border-white/8 bg-black/30 p-6">
              <p class="font-display text-4xl font-extrabold text-primary">
                &lt; 2MB
              </p>
              <p class="mt-1 text-xs text-muted-foreground">
                Cài đặt PWA siêu nhẹ
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <AppFooter />
  </main>
</template>
