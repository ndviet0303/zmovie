<script setup lang="ts">
import { CalendarDays, ChevronLeft, ChevronRight, Clock3 } from "@lucide/vue";

const {
  locale,
  days,
  weekLabel,
  pending,
  error,
  changeWeek,
  goToCurrentWeek,
  refresh,
} = await useSchedule();

const dayFormatter = new Intl.DateTimeFormat("vi-VN", { weekday: "long" });
const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
});

useZMovieSeo({
  title: "Lịch cập nhật phim",
  description: "Theo dõi các tập phim mới được cập nhật theo từng ngày.",
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section class="mx-auto max-w-360 px-5 pb-20 pt-10 lg:px-12">
      <div
        class="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between"
      >
        <div>
          <div
            class="mb-3 inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-bold text-primary"
          >
            <CalendarDays class="size-3.5" />
            Cập nhật từ dữ liệu thực
          </div>
          <h1
            class="font-display text-3xl font-semibold tracking-[-.03em] lg:text-4xl"
          >
            Lịch cập nhật phim
          </h1>
          <p class="mt-2 text-sm text-muted-foreground">{{ weekLabel }}</p>
        </div>
        <div class="flex items-center gap-2">
          <button
            class="rounded-xl border border-white/10 px-3 py-2 text-sm transition hover:border-primary/50"
            aria-label="Tuần trước"
            @click="changeWeek(-1)"
          >
            <ChevronLeft class="size-4" />
          </button>
          <button
            class="rounded-xl border border-white/10 px-4 py-2 text-xs font-semibold transition hover:border-primary/50"
            @click="goToCurrentWeek"
          >
            Tuần này
          </button>
          <button
            class="rounded-xl border border-white/10 px-3 py-2 text-sm transition hover:border-primary/50"
            aria-label="Tuần sau"
            @click="changeWeek(1)"
          >
            <ChevronRight class="size-4" />
          </button>
        </div>
      </div>

      <div v-if="pending" class="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <div
          v-for="index in 4"
          :key="index"
          class="h-64 animate-pulse rounded-2xl bg-white/5"
        />
      </div>
      <div
        v-else-if="error"
        class="mt-8 rounded-2xl border border-destructive/30 bg-destructive/10 p-8 text-center"
      >
        <p class="text-sm">Không thể tải lịch cập nhật.</p>
        <button
          class="mt-4 text-sm font-semibold text-primary"
          @click="refresh"
        >
          Thử lại
        </button>
      </div>
      <div v-else class="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <section
          v-for="day in days"
          :key="day.value"
          class="min-h-64 rounded-2xl border border-white/8 bg-[#171717] p-4"
        >
          <header
            class="mb-4 flex items-center justify-between border-b border-white/8 pb-3"
          >
            <h2 class="capitalize font-semibold">
              {{ dayFormatter.format(day.date) }}
            </h2>
            <span class="text-xs text-muted-foreground">{{
              dateFormatter.format(day.date)
            }}</span>
          </header>
          <div v-if="day.items.length" class="space-y-3">
            <NuxtLink
              v-for="item in day.items"
              :key="item.slug"
              :to="`/movies/${item.slug}`"
              class="group flex gap-3 rounded-xl bg-white/[.03] p-2 transition hover:bg-white/[.07]"
            >
              <img
                :src="item.posterUrl"
                :alt="item.title"
                class="h-20 w-14 shrink-0 rounded-lg object-cover"
                loading="lazy"
              />
              <span class="min-w-0 py-1">
                <strong
                  class="line-clamp-2 text-sm transition group-hover:text-primary"
                  >{{ item.title }}</strong
                >
                <span
                  v-if="item.episodeNumber"
                  class="mt-2 flex items-center gap-1 text-xs text-muted-foreground"
                >
                  <Clock3 class="size-3" /> Tập {{ item.episodeNumber }}
                </span>
              </span>
            </NuxtLink>
          </div>
          <p
            v-else
            class="grid min-h-40 place-items-center text-center text-xs text-muted-foreground"
          >
            Chưa có phim được cập nhật
          </p>
        </section>
      </div>
    </section>
  </main>
</template>
