<script setup lang="ts">
import { ChevronRight } from "@lucide/vue";
import { onBeforeUnmount, onMounted, ref } from "vue";
import { fetchCatalogTitles } from "~/services/catalog.service";
import type { TitleSummary } from "~/types/catalog";

const props = withDefaults(
  defineProps<{
    genre: string;
    title?: string;
    color?: string;
    pageSize?: number;
    locale?: string;
  }>(),
  {
    title: "",
    color: "text-primary",
    pageSize: 6,
    locale: "vi",
  },
);

const sectionRef = ref<HTMLElement | null>(null);
const titles = ref<TitleSummary[]>([]);
const hasLoaded = ref(false);
const isLoading = ref(false);
const hasError = ref(false);

let observer: IntersectionObserver | null = null;

async function loadTitles() {
  if (hasLoaded.value || isLoading.value) return;
  isLoading.value = true;
  hasError.value = false;
  try {
    const res = await fetchCatalogTitles({
      genre: props.genre,
      pageSize: props.pageSize,
      locale: props.locale,
    });
    titles.value = res.items || [];
    hasLoaded.value = true;
  } catch {
    hasError.value = true;
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  if (!import.meta.client) return;

  if (!("IntersectionObserver" in window)) {
    void loadTitles();
    return;
  }

  observer = new IntersectionObserver(
    (entries) => {
      const entry = entries[0];
      if (entry?.isIntersecting) {
        void loadTitles();
        if (observer && sectionRef.value) {
          observer.unobserve(sectionRef.value);
        }
      }
    },
    { rootMargin: "400px" },
  );

  if (sectionRef.value) {
    observer.observe(sectionRef.value);
  }
});

onBeforeUnmount(() => {
  if (observer) {
    observer.disconnect();
    observer = null;
  }
});
</script>

<template>
  <section ref="sectionRef" class="transition-opacity duration-500">
    <!-- Show title if already loaded and has items, or if currently loading -->
    <div
      v-if="isLoading || titles.length > 0"
      class="mb-4 flex items-center justify-between"
    >
      <h2
        class="font-display text-xl font-bold tracking-tight text-white sm:text-2xl"
      >
        Phim <span :class="color">{{ title || genre }}</span>
      </h2>
      <NuxtLink
        :to="`/browse?genre=${encodeURIComponent(genre)}`"
        class="inline-flex items-center gap-1 text-xs font-semibold text-white/70 transition hover:text-primary sm:text-sm"
      >
        Xem toàn bộ <ChevronRight class="size-4" />
      </NuxtLink>
    </div>

    <!-- Skeletons while loading -->
    <div
      v-if="isLoading"
      class="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 md:grid-cols-4 lg:grid-cols-6"
    >
      <div
        v-for="i in pageSize"
        :key="i"
        class="aspect-[2/3] animate-pulse rounded-2xl bg-[#191b24]"
      />
    </div>

    <!-- Loaded Cards Grid -->
    <div
      v-else-if="titles.length > 0"
      class="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 md:grid-cols-4 lg:grid-cols-6"
    >
      <MovieCard
        v-for="item in titles"
        :key="item.slug"
        :title="item"
        variant="vertical"
      />
    </div>

    <!-- Minimal placeholder if empty and not yet triggered, ensures sectionRef has height for intersection -->
    <div v-else-if="!hasLoaded" class="h-20" />
  </section>
</template>
