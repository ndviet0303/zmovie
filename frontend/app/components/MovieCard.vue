<script setup lang="ts">
import type { TitleSummary } from "~/types/catalog";

const props = withDefaults(
  defineProps<{
    title: TitleSummary;
    variant?: "vertical" | "horizontal";
    showProgress?: boolean;
    progressPercent?: number;
    episodeNumber?: number | null;
  }>(),
  {
    variant: "vertical",
    showProgress: false,
    progressPercent: 0,
    episodeNumber: null,
  },
);

// Derive badge labels
const isSongNgu = computed(() => {
  const g = props.title.genre.toLowerCase();
  const t = props.title.title.toLowerCase();
  return (
    g.includes("song ngữ") || t.includes("song ngữ") || props.title.isR2Hosted
  );
});

const is4K = computed(() => {
  return (
    props.title.isR2Hosted || (props.title.year && props.title.year >= 2024)
  );
});

const subBadge = computed(() => {
  if (props.title.type === "series") {
    return `Tập ${props.title.totalEpisodes || 12}`;
  }
  return "P.Đề";
});

const dubBadge = computed(() => {
  return "T.Minh";
});

const fallbackPosters = [
  "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80",
];

const safePosterUrl = computed(() => {
  const url = props.title.posterUrl?.trim() || "";
  if (
    url === "https://phim.nguonc.com" ||
    url === "https://phim.nguonc.com/" ||
    !url
  ) {
    const idx =
      Math.abs(
        (props.title.slug || "")
          .split("")
          .reduce((acc, c) => acc + c.charCodeAt(0), 0),
      ) % fallbackPosters.length;
    return fallbackPosters[idx];
  }
  return url;
});

function onImgError(e: Event) {
  const target = e.target as HTMLImageElement;
  if (!target) return;
  const idx =
    Math.abs(
      (props.title.slug || "")
        .split("")
        .reduce((acc, c) => acc + c.charCodeAt(0), 0),
    ) % fallbackPosters.length;
  target.src = fallbackPosters[idx];
}
</script>

<template>
  <NuxtLink
    :to="`/movies/${title.slug}`"
    class="group flex flex-col transition duration-200"
  >
    <!-- Card Media Container -->
    <div
      class="relative overflow-hidden rounded-xl bg-[#191b24] shadow-md border border-white/5 transition duration-300 group-hover:border-primary/40 group-hover:shadow-[0_10px_30px_rgba(0,0,0,0.6)]"
      :class="variant === 'horizontal' ? 'aspect-video' : 'aspect-[2/3]'"
    >
      <!-- Poster / Backdrop Image -->
      <img
        :src="safePosterUrl"
        :alt="title.title"
        loading="lazy"
        class="size-full object-cover transition-transform duration-500 group-hover:scale-105"
        @error="onImgError"
      />

      <!-- Subtle Gradient Overlay -->
      <div
        class="absolute inset-0 bg-gradient-to-t from-black/85 via-black/20 to-transparent pointer-events-none"
      />

      <!-- Top Badges -->
      <div
        class="absolute left-2.5 top-2.5 flex items-center gap-1.5 pointer-events-none"
      >
        <span
          v-if="isSongNgu"
          class="rounded-md bg-[#1667cf] px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-wide text-white shadow-sm"
        >
          Song ngữ
        </span>
      </div>

      <div
        class="absolute right-2.5 top-2.5 flex items-center gap-1.5 pointer-events-none"
      >
        <span
          v-if="is4K"
          class="rounded-md bg-black/60 backdrop-blur-sm border border-amber-400/50 px-1.5 py-0.5 text-[10px] font-black text-amber-300 shadow-sm"
        >
          4K
        </span>
      </div>

      <!-- Bottom Audio / Episode Badges -->
      <div
        class="absolute inset-x-2.5 bottom-2.5 flex items-center justify-between gap-1 pointer-events-none"
      >
        <div class="flex items-center gap-1">
          <span
            class="rounded bg-black/70 backdrop-blur-sm px-1.5 py-0.5 text-[10px] font-bold text-gray-200"
          >
            {{ subBadge }}
          </span>
          <span
            class="rounded bg-[#059669]/90 px-1.5 py-0.5 text-[10px] font-bold text-emerald-100"
          >
            {{ dubBadge }}
          </span>
        </div>

        <span
          v-if="title.rating && title.rating > 0"
          class="text-[11px] font-bold text-amber-400 flex items-center gap-0.5 drop-shadow"
        >
          ★ {{ title.rating.toFixed(1) }}
        </span>
      </div>

      <!-- Progress bar if applicable -->
      <div
        v-if="showProgress"
        class="absolute inset-x-0 bottom-0 h-1 bg-white/20"
      >
        <div
          class="h-full bg-primary"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>
    </div>

    <!-- Title & Subtitle below card -->
    <div class="mt-2.5 px-0.5">
      <h3
        class="truncate text-sm font-bold text-white transition group-hover:text-primary"
      >
        {{ title.title }}
      </h3>
      <p class="truncate text-xs text-muted-foreground mt-0.5 font-medium">
        {{ title.genre || title.type === "series" ? "Phim bộ" : "Phim lẻ" }} ·
        {{ title.year }}
      </p>
    </div>
  </NuxtLink>
</template>
