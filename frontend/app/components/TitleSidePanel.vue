<script setup lang="ts">
import { Clock } from "@lucide/vue";

defineProps<{
  title: any;
  genreList: string[];
  episodeCount: number;
  actorList: Array<{ name: string; slug: string }>;
  catalogTitles?: any[];
}>();
</script>

<template>
  <aside class="flex flex-col gap-6">
    <!-- Floating Rounded Poster -->
    <div
      class="aspect-[2/3] w-48 sm:w-64 lg:w-full overflow-hidden rounded-3xl border-2 border-white/10 bg-[#191b24] shadow-[0_20px_50px_rgba(0,0,0,0.8)]"
    >
      <img
        :src="title.posterUrl"
        :alt="title.title"
        class="size-full object-cover"
      />
    </div>

    <!-- Title & Subtitle -->
    <div>
      <h1 class="text-2xl sm:text-3xl font-black text-white font-display">
        {{ title.title }}
      </h1>
      <p class="text-sm font-semibold text-primary/80 mt-1 font-display">
        {{
          title.slug
            ?.split("-")
            .map((s: string) => s.charAt(0).toUpperCase() + s.slice(1))
            .join(" ")
        }}
      </p>
    </div>

    <!-- Metadata Chips -->
    <div class="flex flex-wrap items-center gap-1.5 text-xs font-bold">
      <span
        class="rounded bg-[#ffd875] px-2 py-0.5 text-[11px] font-black text-black"
      >
        IMDb {{ title.rating ? title.rating.toFixed(1) : "10.0" }}
      </span>
      <span class="rounded bg-white/10 px-2 py-0.5 text-gray-200"> T16 </span>
      <span class="rounded bg-white/10 px-2 py-0.5 text-gray-200">
        {{ title.year || 2026 }}
      </span>
      <span class="rounded bg-white/10 px-2 py-0.5 text-gray-200">
        {{ title.type === "series" ? "Phần 1" : "Bản Đẹp" }}
      </span>
      <span class="rounded bg-white/10 px-2 py-0.5 text-gray-200">
        Tập {{ title.totalEpisodes || 10 }}
      </span>
    </div>

    <!-- Genre Pills -->
    <div class="flex flex-wrap gap-1.5 text-xs font-medium">
      <span
        v-for="g in genreList"
        :key="g"
        class="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-gray-300"
      >
        {{ g }}
      </span>
    </div>

    <!-- Broadcast Status Badge -->
    <div
      class="inline-flex items-center gap-2 rounded-full border border-amber-400/20 bg-amber-400/10 px-3.5 py-1.5 text-xs font-semibold text-amber-300"
    >
      <Clock class="size-3.5" />
      <span>
        Đã chiếu: Tập {{ episodeCount }} /
        {{ title.totalEpisodes || episodeCount }}
      </span>
    </div>

    <!-- Synopsis / Giới thiệu -->
    <div>
      <h3 class="text-sm font-bold text-white mb-1.5 font-display">
        Giới thiệu:
      </h3>
      <p class="text-xs sm:text-sm leading-relaxed text-gray-300/80">
        {{
          title.synopsis ||
          "Một câu chuyện kịch tính và hấp dẫn với những nút thắt bất ngờ. Trải nghiệm xem phim sắc nét đỉnh cao cùng phụ đề và thuyết minh chuẩn."
        }}
      </p>
    </div>

    <!-- Production Details -->
    <div
      class="space-y-1.5 text-xs text-gray-300/80 border-t border-white/10 pt-4"
    >
      <p>
        <span class="text-white/50">Thời lượng:</span>
        {{ title.type === "series" ? "45 phút / tập" : "120 phút" }}
      </p>
      <p>
        <span class="text-white/50">Quốc gia:</span>
        {{ title.country || "Hàn Quốc" }}
      </p>
      <p>
        <span class="text-white/50">Đạo diễn:</span>
        {{ title.director || "Đang cập nhật" }}
      </p>
    </div>

    <!-- Diễn viên (Cast) Circular Avatars -->
    <div v-if="actorList?.length" class="border-t border-white/10 pt-4">
      <h3 class="text-sm font-bold text-white mb-3 font-display">Diễn viên</h3>
      <div class="grid grid-cols-3 gap-3 text-center">
        <NuxtLink
          v-for="actor in actorList.slice(0, 6)"
          :key="actor.name"
          :to="`/actors/${actor.slug}`"
          class="flex flex-col items-center"
        >
          <span
            class="grid size-14 place-items-center rounded-full border border-white/15 bg-primary/10 text-sm font-bold text-primary shadow-sm transition hover:scale-105"
          >
            {{ actor.name.slice(0, 2).toUpperCase() }}
          </span>
          <span
            class="mt-1.5 line-clamp-1 text-[11px] font-medium text-gray-200"
          >
            {{ actor.name }}
          </span>
        </NuxtLink>
      </div>
    </div>

    <!-- Weekly Leaderboard Sidebar Component -->
    <div class="border-t border-white/10 pt-4">
      <WeeklyLeaderboard :titles="catalogTitles || []" :limit="10" />
    </div>
  </aside>
</template>
