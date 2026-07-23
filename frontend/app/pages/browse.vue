<script setup lang="ts">
import { ChevronDown, Search, SlidersHorizontal, X } from "@lucide/vue";

type Title = {
  slug: string;
  title: string;
  genre: string;
  year: number;
  type: string;
  posterUrl: string;
};
type TitleListResponse = { items: Title[]; total: number };

const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const route = useRoute();
const query = ref("");
const selectedGenre = ref(
  typeof route.query.genre === "string" ? route.query.genre : "all",
);
const selectedType = computed(() =>
  route.query.type === "series" ? "series" : "all",
);
const { $api } = useNuxtApp();
const { data } = await useAsyncData("catalog-browse", () =>
  $api<TitleListResponse>("/v1/catalog/titles", {
    query: { locale: locale.value },
  }),
);
let searchTimer: ReturnType<typeof setTimeout> | undefined;
watch(query, (value) => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(async () => {
    data.value = value.trim()
      ? await $api<TitleListResponse>("/v1/search", {
          query: { q: value.trim(), locale: locale.value },
        })
      : await $api<TitleListResponse>("/v1/catalog/titles", {
          query: { locale: locale.value },
        });
  }, 180);
});

const copy = computed(() =>
  locale.value === "vi"
    ? {
        title: selectedType.value === "series" ? "Phim bộ" : "Khám phá",
        placeholder: "Tìm kiếm phim, diễn viên...",
        filters: "Lọc kết quả",
        all: "Tất cả",
        movies: "Phim lẻ",
        series: "Phim bộ",
        latest: "Mới nhất",
        showMore: "Tải thêm",
        empty: "Không tìm thấy phim phù hợp.",
      }
    : {
        title: selectedType.value === "series" ? "Series" : "Discover",
        placeholder: "Search films, actors...",
        filters: "Filter results",
        all: "All",
        movies: "Movies",
        series: "Series",
        latest: "Latest",
        showMore: "Load more",
        empty: "No titles match your search.",
      },
);

const genres = computed(() => [
  "all",
  ...new Set(data.value?.items.map((title) => title.genre) ?? []),
]);
const visibleTitles = computed(() =>
  (data.value?.items ?? []).filter((title) => {
    const matchesGenre =
      selectedGenre.value === "all" || title.genre === selectedGenre.value;
    const matchesType =
      selectedType.value === "all" || title.type === selectedType.value;
    return matchesGenre && matchesType;
  }),
);

function genreLabel(genre: string) {
  return genre === "all" ? copy.value.all : genre;
}

async function setLocale(nextLocale: "vi" | "en") {
  if (nextLocale === locale.value) return;
  const nextCatalog = await $api<TitleListResponse>("/v1/catalog/titles", {
    query: { locale: nextLocale },
  });
  data.value = nextCatalog;
  locale.value = nextLocale;
}
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" @locale-change="setLocale" />

    <section class="mx-auto max-w-360 px-5 pb-24 pt-12 lg:px-12 lg:pt-16">
      <h1
        class="font-display text-4xl font-semibold tracking-tight sm:text-5xl"
      >
        {{ copy.title }}
      </h1>
      <label class="relative mt-7 block max-w-2xl">
        <Search
          class="pointer-events-none absolute left-5 top-1/2 size-5 -translate-y-1/2 text-muted-foreground"
        />
        <input
          v-model="query"
          :placeholder="copy.placeholder"
          class="h-14 w-full rounded-2xl border border-white/10 bg-surface-container pl-13 pr-12 text-sm text-foreground outline-none transition placeholder:text-muted-foreground focus:border-primary/60 focus:ring-2 focus:ring-primary/15"
        />
        <button
          v-if="query"
          class="absolute right-4 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-primary"
          aria-label="Clear search"
          @click="query = ''"
        >
          <X class="size-5" />
        </button>
      </label>

      <div id="filters" class="mt-8 flex flex-wrap items-center gap-3">
        <span
          class="mr-1 inline-flex items-center gap-2 text-xs font-semibold text-muted-foreground"
          ><SlidersHorizontal class="size-4" />{{ copy.filters }}</span
        >
        <button
          v-for="genre in genres"
          :key="genre"
          class="rounded-full border px-4 py-2 text-xs font-medium transition"
          :class="
            selectedGenre === genre
              ? 'border-primary-container bg-primary-container text-primary-container-foreground'
              : 'border-white/10 bg-surface-container text-muted-foreground hover:border-primary/50 hover:text-foreground'
          "
          @click="selectedGenre = genre"
        >
          {{ genreLabel(genre) }}
        </button>
        <button
          class="ml-auto hidden items-center gap-2 rounded-xl border border-white/10 bg-surface-container px-4 py-2 text-xs text-muted-foreground sm:flex"
        >
          {{ copy.latest }} <ChevronDown class="size-4" />
        </button>
      </div>

      <div
        v-if="visibleTitles.length"
        class="mt-10 grid grid-cols-2 gap-5 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5"
      >
        <NuxtLink
          v-for="(title, index) in visibleTitles"
          :key="title.slug"
          :to="`/movies/${title.slug}`"
          class="group relative aspect-[2/3] overflow-hidden rounded-3xl bg-surface-container shadow-[inset_0_1px_0_rgba(235,225,214,.1)] transition duration-300 hover:scale-[1.02] hover:shadow-[0_16px_40px_rgba(217,131,103,.16)]"
        >
          <img
            :src="title.posterUrl"
            :alt="title.title"
            class="absolute inset-0 size-full object-cover opacity-80 transition duration-500 group-hover:opacity-100"
            loading="lazy"
          />
          <div
            class="absolute inset-0 bg-gradient-to-t from-black/95 via-black/20 to-transparent"
          />
          <span
            class="absolute left-4 top-4 rounded-md px-2 py-1 text-[10px] font-bold tracking-wider"
            :class="
              index % 2 === 0
                ? 'bg-primary text-primary-container-foreground'
                : 'border border-white/20 bg-background/60 text-foreground backdrop-blur-sm'
            "
            >{{ index % 2 === 0 ? "4K" : "HD" }}</span
          >
          <div class="absolute inset-x-0 bottom-0 p-5">
            <h2 class="font-display truncate text-xl font-medium">
              {{ title.title }}
            </h2>
            <p class="mt-1 text-xs text-tertiary">
              {{ title.year }} · {{ title.genre }}
            </p>
          </div>
        </NuxtLink>
      </div>
      <p
        v-else
        class="mt-10 rounded-3xl border border-white/10 bg-surface-container p-10 text-center text-muted-foreground"
      >
        {{ copy.empty }}
      </p>
      <button
        v-if="visibleTitles.length"
        class="mx-auto mt-12 block rounded-full border border-white/10 bg-surface-container px-6 py-3 text-xs font-medium text-foreground transition hover:border-primary/60 hover:text-primary"
      >
        {{ copy.showMore }}
      </button>
    </section>

    <footer
      class="border-t border-white/5 bg-surface-container-lowest px-5 py-10 text-center text-xs text-tertiary"
    >
      <NuxtLink to="/" class="font-display text-xl font-semibold text-primary"
        >ZMovie</NuxtLink
      >
      <p class="mt-5">© 2026 ZMovie Premium. All rights reserved.</p>
    </footer>
  </main>
</template>
