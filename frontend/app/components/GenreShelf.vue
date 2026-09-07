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

watch(
  () => props.locale,
  (next, prev) => {
    if (next && next !== prev && hasLoaded.value) {
      hasLoaded.value = false;
      void loadTitles();
    }
  },
);

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
    { rootMargin: "200px 0px" },
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
  <section
    v-if="!hasLoaded || titles.length > 0"
    ref="sectionRef"
    class="min-h-[340px] transition-opacity duration-500"
  >
    <!-- Section Heading -->
    <div class="mb-4 flex items-center justify-between">
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

    <!-- Skeletons while not loaded yet or loading -->
    <div
      v-if="!hasLoaded"
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
  </section>
</template>

<style scoped>
/* Ensure section height reservation during SSR/prerender to prevent cascade */
section {
  content-visibility: auto;
  contain-intrinsic-size: 1px 340px;
}
</style>
