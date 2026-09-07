<script setup lang="ts">
import { Bookmark, Heart, Play, Share2, Star } from "@lucide/vue";
import { useMovieSeo } from "~/composables/useMovieSeo";

const {
  locale,
  title,
  error,
  reviews,
  isSaved,
  actionNotice,
  reviewRating,
  reviewComment,
  isSubmittingReview,
  related,
  formattedDate,
  catalog,
  setLocale,
  toggleSaved,
  shareTitle,
  submitReview,
} = await useMovieDetail();

const isTrailerModalOpen = ref(false);

useMovieSeo({
  title: computed(() => title.value?.title || ""),
  description: computed(() => title.value?.synopsis || ""),
  image: computed(() => title.value?.posterUrl),
  type: computed(() => title.value?.type || "movie"),
  year: computed(() => title.value?.year),
  director: computed(() => title.value?.directors),
  actors: computed(() =>
    Array.isArray(title.value?.actors)
      ? title.value?.actors.join(", ")
      : title.value?.actors,
  ),
  trailerUrl: computed(() => title.value?.trailerUrl),
  genre: computed(() => title.value?.genre),
});

// Active tab in right column
const activeTab = ref<"episodes" | "gallery" | "actors" | "recommendations">(
  "episodes",
);

function personSlug(name: string): string {
  return name
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, "-")
    .replace(/^-|-$/g, "");
}

const actorList = computed(() => {
  const rawActors = title.value?.actors;
  const names = Array.isArray(rawActors)
    ? rawActors
    : typeof rawActors === "string"
      ? rawActors.split(",")
      : [];
  return names
    .map((name) => name.trim())
    .filter(Boolean)
    .map((name) => ({ name, slug: personSlug(name) }));
});

// Episodes list
const episodeList = computed(() => {
  if (title.value?.episodes?.length) return title.value.episodes;
  const count =
    title.value?.totalEpisodes || (title.value?.type === "series" ? 12 : 1);
  return Array.from({ length: count }, (_, i) => ({
    episodeNumber: i + 1,
    title: `Tập ${i + 1}`,
  }));
});

// Genres array
const genreList = computed(() => {
  if (!title.value?.genre) return ["Hành Động", "Hình Sự", "Chính Kịch"];
  return title.value.genre
    .split(",")
    .map((g) => g.trim())
    .filter(Boolean);
});
</script>

<template>
  <div class="min-h-screen bg-[#0f111a] text-white flex flex-col">
    <AppNavbar :locale="locale" @locale-change="setLocale" />

    <template v-if="title">
      <!-- HERO BACKDROP BANNER -->
      <div class="relative h-72 sm:h-96 lg:h-[420px] w-full overflow-hidden">
        <img
          :src="title.posterUrl"
          :alt="title.title"
          class="size-full object-cover object-center opacity-40 blur-sm scale-105"
        />
        <div
          class="absolute inset-0 bg-gradient-to-t from-[#0f111a] via-[#0f111a]/70 to-transparent"
        />
      </div>

      <!-- MAIN 2-COLUMN CONTENT CONTAINER -->
      <main
        class="relative z-10 mx-auto w-full max-w-360 px-4 sm:px-6 lg:px-10 -mt-36 sm:-mt-48 pb-20"
      >
        <div
          class="grid grid-cols-1 lg:grid-cols-[340px_1fr] gap-8 sm:gap-12 items-start"
        >
          <!-- LEFT COLUMN: Poster, Title, Meta, Cast & Leaderboard -->
          <TitleSidePanel
            :title="title"
            :genre-list="genreList"
            :episode-count="episodeList.length"
            :actor-list="actorList"
            :catalog-titles="catalog?.items || []"
          />

          <!-- RIGHT COLUMN: Actions, Tabs, Episode Grid, Comments -->
          <div class="flex flex-col gap-8">
            <!-- Action Buttons Bar -->
            <div
              class="flex flex-wrap items-center gap-3.5 pb-6 border-b border-white/10"
            >
              <!-- Xem Ngay Yellow Pill Button -->
              <NuxtLink
                :to="`/watch/${title.slug}`"
                class="inline-flex items-center gap-2 rounded-full bg-primary px-8 py-3.5 text-sm font-extrabold text-black shadow-[0_0_25px_rgba(255,216,117,0.4)] transition hover:scale-105 hover:bg-[#ffde8a] active:scale-95"
              >
                <Play class="size-4.5 fill-current" />
                <span>Xem Ngay</span>
              </NuxtLink>

              <!-- Xem Trailer Button -->
              <button
                v-if="title.trailerUrl"
                type="button"
                class="inline-flex items-center gap-2 rounded-full border border-amber-500/30 bg-amber-500/10 px-6 py-3.5 text-xs sm:text-sm font-bold text-amber-400 backdrop-blur-sm transition hover:bg-amber-500/20 active:scale-95"
                @click="isTrailerModalOpen = true"
              >
                <Play class="size-4" />
                <span>Xem Trailer</span>
              </button>

              <!-- Yêu thích -->
              <button
                type="button"
                class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-4 py-3 text-xs font-bold text-white transition hover:bg-white/10"
                @click="toggleSaved"
              >
                <Heart
                  class="size-4"
                  :class="{ 'fill-red-500 text-red-500': isSaved }"
                />
                <span>{{ isSaved ? "Đã thích" : "Yêu thích" }}</span>
              </button>

              <!-- Thêm vào -->
              <button
                type="button"
                class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-4 py-3 text-xs font-bold text-white transition hover:bg-white/10"
                @click="toggleSaved"
              >
                <Bookmark
                  class="size-4"
                  :class="{ 'fill-primary text-primary': isSaved }"
                />
                <span>Thêm vào</span>
              </button>

              <!-- Chia sẻ -->
              <button
                type="button"
                class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-4 py-3 text-xs font-bold text-white transition hover:bg-white/10"
                @click="shareTitle"
              >
                <Share2 class="size-4" />
                <span>Chia sẻ</span>
              </button>

              <!-- Rating Badge -->
              <div
                class="ml-auto inline-flex items-center gap-1.5 rounded-full bg-[#1667cf] px-4 py-2 text-xs font-black text-white shadow-sm"
              >
                <Star class="size-3.5 fill-current" />
                <span>{{ reviews?.length || 0 }} Đánh giá</span>
              </div>
            </div>

            <!-- Detail Tabs Bar -->
            <div
              class="flex items-center gap-8 border-b border-white/10 text-sm font-bold"
            >
              <button
                type="button"
                class="pb-3 transition relative"
                :class="
                  activeTab === 'episodes'
                    ? 'text-primary font-black'
                    : 'text-white/60 hover:text-white'
                "
                @click="activeTab = 'episodes'"
              >
                <span>Tập phim</span>
                <span
                  v-if="activeTab === 'episodes'"
                  class="absolute inset-x-0 bottom-0 h-0.5 bg-primary"
                />
              </button>

              <button
                type="button"
                class="pb-3 transition relative"
                :class="
                  activeTab === 'gallery'
                    ? 'text-primary font-black'
                    : 'text-white/60 hover:text-white'
                "
                @click="activeTab = 'gallery'"
              >
                <span>Gallery</span>
                <span
                  v-if="activeTab === 'gallery'"
                  class="absolute inset-x-0 bottom-0 h-0.5 bg-primary"
                />
              </button>

              <button
                type="button"
                class="pb-3 transition relative"
                :class="
                  activeTab === 'actors'
                    ? 'text-primary font-black'
                    : 'text-white/60 hover:text-white'
                "
                @click="activeTab = 'actors'"
              >
                <span>Diễn viên</span>
                <span
                  v-if="activeTab === 'actors'"
                  class="absolute inset-x-0 bottom-0 h-0.5 bg-primary"
                />
              </button>

              <button
                type="button"
                class="pb-3 transition relative"
                :class="
                  activeTab === 'recommendations'
                    ? 'text-primary font-black'
                    : 'text-white/60 hover:text-white'
                "
                @click="activeTab = 'recommendations'"
              >
                <span>Đề xuất</span>
                <span
                  v-if="activeTab === 'recommendations'"
                  class="absolute inset-x-0 bottom-0 h-0.5 bg-primary"
                />
              </button>
            </div>

            <!-- Tab 1: Tập phim Content -->
            <TitleEpisodeGrid
              v-if="activeTab === 'episodes'"
              :slug="title.slug"
              :episode-list="episodeList"
            />

            <!-- Tab 2: Gallery -->
            <div
              v-else-if="activeTab === 'gallery'"
              class="grid grid-cols-2 sm:grid-cols-3 gap-4"
            >
              <div
                v-for="i in 6"
                :key="i"
                class="aspect-video rounded-2xl overflow-hidden bg-[#191b24] border border-white/10 shadow"
              >
                <img
                  :src="title.posterUrl"
                  :alt="title.title"
                  class="size-full object-cover hover:scale-105 transition"
                />
              </div>
            </div>

            <!-- Tab 3: Diễn viên -->
            <div
              v-else-if="activeTab === 'actors'"
              class="grid grid-cols-2 sm:grid-cols-4 gap-4"
            >
              <NuxtLink
                v-for="actor in actorList"
                :key="actor.name"
                :to="`/actors/${actor.slug}`"
                class="flex flex-col items-center rounded-2xl border border-white/10 bg-[#191b24] p-4 text-center transition hover:border-primary/50"
              >
                <span
                  class="grid size-16 place-items-center rounded-full border border-primary/50 bg-primary/10 text-lg font-bold text-primary shadow"
                >
                  {{ actor.name.slice(0, 2).toUpperCase() }}
                </span>
                <h4 class="mt-2 text-xs font-bold text-white">
                  {{ actor.name }}
                </h4>
                <p class="mt-0.5 text-[11px] text-muted-foreground">
                  Diễn viên
                </p>
              </NuxtLink>
            </div>

            <!-- Tab 4: Đề xuất -->
            <div
              v-else-if="activeTab === 'recommendations'"
              class="grid grid-cols-2 sm:grid-cols-4 gap-4"
            >
              <MovieCard
                v-for="item in (related || []).slice(0, 8)"
                :key="item.slug"
                :title="item"
                variant="vertical"
              />
            </div>

            <!-- COMMENTS & REVIEWS SECTION -->
            <TitleReviewSection
              :reviews="reviews"
              :action-notice="actionNotice"
              :is-submitting-review="isSubmittingReview"
              :formatted-date="formattedDate"
              v-model:rating="reviewRating"
              v-model:comment="reviewComment"
              @submit="submitReview"
            />
          </div>
        </div>
      </main>
    </template>

    <div v-else-if="error" class="mx-auto max-w-lg py-32 text-center">
      <h2 class="text-xl font-bold text-red-400">
        Không tìm thấy thông tin phim
      </h2>
      <NuxtLink to="/" class="mt-4 inline-block text-sm text-primary underline">
        Quay lại trang chủ
      </NuxtLink>
    </div>

    <!-- Reusable Trailer Modal Component -->
    <TrailerModal
      :open="isTrailerModalOpen"
      :title="title?.title || ''"
      :trailer-url="title?.trailerUrl"
      @close="isTrailerModalOpen = false"
    />

    <AppFooter />
  </div>
</template>
