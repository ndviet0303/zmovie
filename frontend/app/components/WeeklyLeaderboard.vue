<script setup lang="ts">
import { Trophy } from "@lucide/vue";
import type { TitleSummary } from "~/types/catalog";

const props = defineProps<{
  titles: TitleSummary[];
  limit?: number;
}>();

const displayTitles = computed(() => {
  return props.titles.slice(0, props.limit || 10);
});

const fallbackPosters = [
  "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=400&q=80",
  "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=400&q=80",
  "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=400&q=80",
  "https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=400&q=80",
];

function getSafePoster(title: TitleSummary) {
  const url = title.posterUrl?.trim() || "";
  if (
    url === "https://phim.nguonc.com" ||
    url === "https://phim.nguonc.com/" ||
    !url
  ) {
    const idx =
      Math.abs(
        (title.slug || "")
          .split("")
          .reduce((acc, c) => acc + c.charCodeAt(0), 0),
      ) % fallbackPosters.length;
    return fallbackPosters[idx];
  }
  return url;
}

function onImgError(e: Event, slug: string) {
  const target = e.target as HTMLImageElement;
  if (!target) return;
  const idx =
    Math.abs(slug.split("").reduce((acc, c) => acc + c.charCodeAt(0), 0)) %
    fallbackPosters.length;
  target.src = fallbackPosters[idx];
}
</script>

<template>
  <div class="rounded-3xl border border-white/5 bg-[#141620] p-5">
    <div class="mb-4 flex items-center gap-2.5">
      <Trophy class="size-5 text-primary" />
      <h3 class="text-base font-bold text-white font-display">
        Top phim tuần này
      </h3>
    </div>

    <div class="flex flex-col gap-3">
      <NuxtLink
        v-for="(item, index) in displayTitles"
        :key="item.slug"
        :to="`/movies/${item.slug}`"
        class="group flex items-center gap-3.5 rounded-2xl p-2 transition hover:bg-white/5"
      >
        <!-- Outlined Rank Number -->
        <div
          class="w-7 text-center font-display text-2xl font-black italic text-white/30 transition group-hover:text-primary"
        >
          {{ index + 1 }}
        </div>

        <!-- Thumbnail -->
        <div
          class="relative size-14 shrink-0 overflow-hidden rounded-xl bg-[#1e2230] shadow"
        >
          <img
            :src="getSafePoster(item)"
            :alt="item.title"
            loading="lazy"
            class="size-full object-cover transition group-hover:scale-105"
            @error="(e) => onImgError(e, item.slug)"
          />
        </div>

        <!-- Meta -->
        <div class="min-w-0 flex-1">
          <h4
            class="truncate text-xs font-bold text-white transition group-hover:text-primary"
          >
            {{ item.title }}
          </h4>
          <p class="truncate text-[11px] text-muted-foreground mt-0.5">
            {{ item.genre }}
          </p>
          <div class="mt-1 flex items-center gap-1.5 text-[10px] text-white/60">
            <span class="rounded bg-white/10 px-1 py-0.2">T16</span>
            <span>·</span>
            <span>{{ item.year }}</span>
            <span v-if="item.type === 'series'">· Phần 1</span>
          </div>
        </div>
      </NuxtLink>
    </div>
  </div>
</template>
