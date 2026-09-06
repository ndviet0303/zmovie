<script setup lang="ts">
import { ChevronRight, Heart, Info, Play } from "@lucide/vue";
import type { TitleSummary } from "~/types/catalog";

const {
  home,
  activeLocale,
  continueWatching,
  newReleaseTitles,
  titles2026,
  moviePicks,
  seriesPicks,
  changeLocale,
  progressPercent,
} = await useHomePage();

// Reactive spotlight title in Hero Banner
const selectedHero = ref<TitleSummary | null>(null);

const activeHero = computed<TitleSummary | null>(() => {
  return (
    selectedHero.value ||
    home.value?.hero ||
    newReleaseTitles.value?.[0] ||
    null
  );
});

// Spotlight thumbnails list for the bottom-right hero slider
const heroSliderTitles = computed<TitleSummary[]>(() => {
  const currentHero = home.value?.hero;
  const list = [
    ...(currentHero ? [currentHero] : []),
    ...(newReleaseTitles.value || []).slice(0, 6),
  ];
  const seen = new Set<string>();
  return list.filter((item) => {
    if (seen.has(item.slug)) return false;
    seen.add(item.slug);
    return true;
  });
});

let autoCycleTimer: ReturnType<typeof setInterval> | null = null;
const isHoveringHero = ref(false);

function selectHero(title: TitleSummary) {
  selectedHero.value = title;
}

onMounted(() => {
  autoCycleTimer = setInterval(() => {
    if (isHoveringHero.value || !heroSliderTitles.value.length) return;
    const currentIndex = heroSliderTitles.value.findIndex(
      (item) => item.slug === activeHero.value?.slug,
    );
    const nextIndex = (currentIndex + 1) % heroSliderTitles.value.length;
    selectedHero.value = heroSliderTitles.value[nextIndex];
  }, 7000);
});

onBeforeUnmount(() => {
  if (autoCycleTimer) clearInterval(autoCycleTimer);
});

// Fallback posters collection
const fallbackPosters = [
  "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=1200&q=80",
  "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80",
  "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1200&q=80",
  "https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=1200&q=80",
  "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?auto=format&fit=crop&w=1200&q=80",
  "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1200&q=80",
];

function getSafePoster(title?: TitleSummary | null) {
  if (!title) return fallbackPosters[0];
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

function onImgError(e: Event, slug?: string) {
  const target = e.target as HTMLImageElement;
  if (!target) return;
  const idx =
    Math.abs(
      (slug || "").split("").reduce((acc, c) => acc + c.charCodeAt(0), 0),
    ) % fallbackPosters.length;
  target.src = fallbackPosters[idx];
}

// Split genres into pills
const heroGenres = computed(() => {
  if (!activeHero.value?.genre) return ["Hành Động", "Viễn Tưởng"];
  return activeHero.value.genre
    .split(",")
    .map((g) => g.trim())
    .filter(Boolean);
});

const lazyGenres = [
  { genre: "Hành Động", title: "Hành Động", color: "text-amber-400" },
  { genre: "Cổ Trang", title: "Cổ Trang", color: "text-rose-400" },
  { genre: "Hoạt Hình", title: "Hoạt Hình & Anime", color: "text-cyan-400" },
  { genre: "Tình Cảm", title: "Tình Cảm & Lãng Mạn", color: "text-pink-400" },
  { genre: "Kinh Dị", title: "Kinh Dị", color: "text-red-500" },
  { genre: "Hài Hước", title: "Hài Hước", color: "text-yellow-300" },
  {
    genre: "Khoa Học Viễn Tưởng",
    title: "Khoa Học Viễn Tưởng",
    color: "text-blue-400",
  },
  { genre: "Tâm Lý", title: "Tâm Lý", color: "text-purple-400" },
  { genre: "Võ Thuật", title: "Võ Thuật", color: "text-orange-400" },
  { genre: "Hình Sự", title: "Hình Sự & Tội Phạm", color: "text-emerald-400" },
  {
    genre: "Học Đường",
    title: "Học Đường & Thanh Xuân",
    color: "text-teal-400",
  },
  { genre: "Gia Đình", title: "Gia Đình", color: "text-indigo-400" },
  { genre: "Chiếu Rạp", title: "Điện Ảnh Chiếu Rạp", color: "text-amber-300" },
  {
    genre: "Chiến Tranh",
    title: "Chiến Tranh & Lịch Sử",
    color: "text-stone-300",
  },
];
</script>

<template>
  <div class="min-h-screen bg-[#0f111a] text-white flex flex-col">
    <!-- Minimalist Sticky Header with Drawer -->
    <AppNavbar :locale="activeLocale" @locale-change="changeLocale" />

    <!-- 1. HERO SPOTLIGHT BANNER -->
    <section
      v-if="activeHero"
      class="relative -mt-22 h-[80vh] min-h-[560px] max-h-[700px] w-full flex items-end overflow-hidden pb-5 pt-26"
      @mouseenter="isHoveringHero = true"
      @mouseleave="isHoveringHero = false"
    >
      <!-- Backdrop Image with Transition -->
      <transition name="fade" mode="out-in">
        <img
          :key="activeHero.slug"
          :src="getSafePoster(activeHero)"
          :alt="activeHero.title"
          class="absolute inset-0 size-full object-cover object-center opacity-70 animate-cover-fade"
          @error="(e) => onImgError(e, activeHero?.slug)"
        />
      </transition>

      <!-- Gradient Masks (Cinematic Vignette) -->
      <!-- Top header fade -->
      <div
        class="absolute inset-x-0 top-0 h-32 bg-gradient-to-b from-black/85 via-black/40 to-transparent pointer-events-none"
      />
      <!-- Left text backdrop veil -->
      <div
        class="absolute inset-y-0 left-0 w-full lg:w-3/5 bg-gradient-to-r from-[#0f111a] via-[#0f111a]/85 to-transparent pointer-events-none"
      />
      <!-- Bottom fade into page background -->
      <div
        class="absolute inset-x-0 bottom-0 h-32 bg-gradient-to-t from-[#0f111a] via-[#0f111a]/70 to-transparent pointer-events-none"
      />

      <!-- Hero Content Container -->
      <div
        class="relative mx-auto flex w-full max-w-360 flex-col px-4 sm:px-6 lg:px-10 z-10"
      >
        <!-- Details Column -->
        <div class="max-w-2xl">
          <!-- Title & Subtitle -->
          <h1
            class="text-3xl sm:text-4xl lg:text-[46px] font-black tracking-tight text-white drop-shadow-[0_4px_16px_rgba(0,0,0,0.9)] font-display leading-[1.1]"
          >
            {{ activeHero.title }}
          </h1>
          <p
            class="mt-1.5 text-xs sm:text-sm font-semibold text-[#ffd875] font-display tracking-wider drop-shadow"
          >
            {{
              activeHero.slug
                .split("-")
                .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
                .join(" ")
            }}
          </p>

          <!-- Metadata Chips Row -->
          <div
            class="mt-3.5 flex flex-wrap items-center gap-1.5 text-xs font-semibold"
          >
            <!-- IMDb Badge with gold border -->
            <span
              class="rounded border border-[#ffd875] bg-black/40 px-2 py-0.5 text-xs font-bold text-[#ffd875] backdrop-blur-md"
            >
              IMDb
              {{ activeHero.rating ? activeHero.rating.toFixed(1) : "8.5" }}
            </span>
            <!-- Rating Tag -->
            <span
              class="rounded border border-white/20 bg-black/40 px-2 py-0.5 text-xs text-gray-200 backdrop-blur-md"
            >
              T16
            </span>
            <!-- Year -->
            <span
              class="rounded border border-white/20 bg-black/40 px-2 py-0.5 text-xs text-gray-200 backdrop-blur-md"
            >
              {{ activeHero.year || 2026 }}
            </span>
            <!-- Type / Season -->
            <span
              class="rounded border border-white/20 bg-black/40 px-2 py-0.5 text-xs text-gray-200 backdrop-blur-md"
            >
              {{ activeHero.type === "series" ? "Phần 1" : "Bản Đẹp" }}
            </span>
            <!-- Episodes -->
            <span
              class="rounded border border-white/20 bg-black/40 px-2 py-0.5 text-xs text-gray-200 backdrop-blur-md"
            >
              {{
                activeHero.type === "series"
                  ? `Tập ${activeHero.totalEpisodes || 12}`
                  : "Tập Hoàn Tất"
              }}
            </span>
          </div>

          <!-- Genre Pills -->
          <div
            class="mt-2.5 flex flex-wrap items-center gap-1.5 text-xs font-medium"
          >
            <span
              v-for="g in heroGenres"
              :key="g"
              class="rounded border border-white/10 bg-black/30 px-2.5 py-0.5 text-gray-300 backdrop-blur-md text-[11px]"
            >
              {{ g }}
            </span>
          </div>

          <!-- Description (Truncated) -->
          <p
            class="mt-3 line-clamp-2 sm:line-clamp-3 text-xs sm:text-sm leading-relaxed text-gray-300/80 max-w-lg font-normal"
          >
            {{
              activeHero.description ||
              `${activeHero.title} (${activeHero.year || 2026}) - Trọn bộ HD vietsub thuyết minh mới nhất trên ZMovie.`
            }}
          </p>
        </div>

        <!-- Bottom Controls Row: Action Buttons on Left, Thumbnails on Right -->
        <div class="mt-5 flex items-center justify-between gap-4">
          <!-- Left Action Buttons -->
          <div class="flex items-center gap-3">
            <!-- Play Button (Yellow Circle) -->
            <NuxtLink
              :to="`/watch/${activeHero.slug}`"
              class="grid size-13 place-items-center rounded-full bg-[#ffd875] text-black shadow-[0_0_22px_rgba(255,216,117,0.4)] transition duration-200 hover:scale-105 hover:bg-[#ffde8a] active:scale-95"
              title="Xem ngay"
            >
              <Play class="size-5.5 fill-current translate-x-0.5" />
            </NuxtLink>

            <!-- Favorite Button (Round Outline) -->
            <button
              type="button"
              class="grid size-10.5 place-items-center rounded-full border border-white/20 bg-black/40 text-white backdrop-blur-md transition duration-200 hover:border-white/50 hover:bg-white/10 active:scale-95"
              title="Yêu thích"
            >
              <Heart class="size-4.5" />
            </button>

            <!-- Details Button (Round Outline) -->
            <NuxtLink
              :to="`/movies/${activeHero.slug}`"
              class="grid size-10.5 place-items-center rounded-full border border-white/20 bg-black/40 text-white backdrop-blur-md transition duration-200 hover:border-white/50 hover:bg-white/10 active:scale-95"
              title="Chi tiết phim"
            >
              <Info class="size-4.5" />
            </NuxtLink>
          </div>

          <!-- Right Thumbnail Strip (Hero Select) -->
          <div
            class="flex items-center gap-1.5 sm:gap-2 overflow-x-auto pb-1 no-scrollbar shrink-0"
          >
            <button
              v-for="item in heroSliderTitles"
              :key="item.slug"
              type="button"
              class="relative aspect-[16/10] w-14 sm:w-16 shrink-0 overflow-hidden rounded-[7px] transition-all duration-200 bg-[#191b24]"
              :class="
                activeHero.slug === item.slug
                  ? 'border-[1.5px] border-white opacity-100'
                  : 'border border-white/10 opacity-45 hover:opacity-85'
              "
              :title="item.title"
              @click="selectHero(item)"
            >
              <img
                :src="getSafePoster(item)"
                :alt="item.title"
                class="size-full object-cover"
                @error="(e) => onImgError(e, item.slug)"
              />
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- 2. TOPIC CARDS: "Bạn đang quan tâm gì?" -->
    <TopicCards />

    <!-- 3. MAIN CONTENT SECTIONS -->
    <div
      class="mx-auto w-full max-w-360 px-4 sm:px-6 lg:px-10 pb-16 space-y-12"
    >
      <!-- Section: Xem tiếp (Continue Watching) -->
      <section v-if="continueWatching.length">
        <div class="mb-4 flex items-center justify-between">
          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white font-display"
          >
            Xem tiếp
          </h2>
          <NuxtLink
            to="/my-list"
            class="inline-flex items-center gap-1 text-xs sm:text-sm font-semibold text-primary hover:underline"
          >
            Xem toàn bộ <ChevronRight class="size-4" />
          </NuxtLink>
        </div>
        <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
          <MovieCard
            v-for="item in continueWatching"
            :key="item.title.slug"
            :title="item.title"
            variant="horizontal"
            :show-progress="true"
            :progress-percent="progressPercent(item)"
            :episode-number="item.episodeNumber"
          />
        </div>
      </section>

      <!-- Section: Phim Song Ngữ (16:9 Horizontal Cards) -->
      <section>
        <div class="mb-4 flex items-center justify-between">
          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white font-display"
          >
            Phim <span class="text-primary">Song Ngữ</span>
          </h2>
          <NuxtLink
            to="/browse?genre=Song%20Ngữ"
            class="inline-flex items-center gap-1 text-xs sm:text-sm font-semibold text-white/70 hover:text-primary transition"
          >
            Xem toàn bộ <ChevronRight class="size-4" />
          </NuxtLink>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          <MovieCard
            v-for="item in (seriesPicks || []).slice(0, 3)"
            :key="item.slug"
            :title="item"
            variant="horizontal"
          />
        </div>
      </section>

      <!-- Section: Phim Trung Quốc mới (2:3 Vertical Cards) -->
      <section>
        <div class="mb-4 flex items-center justify-between">
          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white font-display"
          >
            Phim <span class="text-amber-400">Trung Quốc</span> mới
          </h2>
          <NuxtLink
            to="/browse?country=Trung%20Quốc"
            class="inline-flex items-center gap-1 text-xs sm:text-sm font-semibold text-white/70 hover:text-primary transition"
          >
            Xem toàn bộ <ChevronRight class="size-4" />
          </NuxtLink>
        </div>
        <div
          class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5"
        >
          <MovieCard
            v-for="item in moviePicks.slice(0, 6)"
            :key="item.slug"
            :title="item"
            variant="vertical"
          />
        </div>
      </section>

      <!-- Section: Phim US-UK mới (2:3 Vertical Cards) -->
      <section>
        <div class="mb-4 flex items-center justify-between">
          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white font-display"
          >
            Phim <span class="text-rose-400">US-UK</span> mới
          </h2>
          <NuxtLink
            to="/browse?country=Âu%20Mỹ"
            class="inline-flex items-center gap-1 text-xs sm:text-sm font-semibold text-white/70 hover:text-primary transition"
          >
            Xem toàn bộ <ChevronRight class="size-4" />
          </NuxtLink>
        </div>
        <div
          class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5"
        >
          <MovieCard
            v-for="item in newReleaseTitles.slice(0, 6)"
            :key="item.slug"
            :title="item"
            variant="vertical"
          />
        </div>
      </section>

      <!-- Section: Phim 2026 Mới Chiếu Rạp -->
      <section v-if="titles2026.length">
        <div class="mb-4 flex items-center justify-between">
          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white font-display"
          >
            Phim Chiếu Rạp <span class="text-emerald-400">2026</span>
          </h2>
          <NuxtLink
            to="/browse?year=2026"
            class="inline-flex items-center gap-1 text-xs sm:text-sm font-semibold text-white/70 hover:text-primary transition"
          >
            Xem toàn bộ <ChevronRight class="size-4" />
          </NuxtLink>
        </div>
        <div
          class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5"
        >
          <MovieCard
            v-for="item in titles2026.slice(0, 6)"
            :key="item.slug"
            :title="item"
            variant="vertical"
          />
        </div>
      </section>

      <!-- 5. Lazy-loaded Genre Sections -->
      <GenreShelf
        v-for="item in lazyGenres"
        :key="item.genre"
        :genre="item.genre"
        :title="item.title"
        :color="item.color"
        :locale="activeLocale"
      />
    </div>

    <!-- 4. FOOTER -->
    <AppFooter />
  </div>
</template>
