<script setup lang="ts">
import {
  Bookmark,
  Clock,
  Heart,
  MessageSquare,
  Play,
  Send,
  Share2,
  Smile,
  Star,
  ThumbsDown,
  ThumbsUp,
  X,
} from "@lucide/vue";
import { useMovieSeo } from "~/composables/useMovieSeo";

const {
  locale,
  title,
  error,
  reviews,
  isSaved,
  reviewComment,
  isSubmittingReview,
  related,
  catalog,
  setLocale,
  toggleSaved,
  shareTitle,
  submitReview,
} = await useMovieDetail();

const isTrailerModalOpen = ref(false);

function getEmbedTrailerUrl(url: string): string {
  if (!url) return "";
  if (url.includes("youtube.com/watch?v=")) {
    return url.replace("watch?v=", "embed/");
  }
  if (url.includes("youtu.be/")) {
    return url.replace("youtu.be/", "www.youtube.com/embed/");
  }
  return url;
}

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
// Comment vs Rating tab
const commentTab = ref<"comment" | "rating">("comment");
// Audio filter tab
const activeAudio = ref<"sub" | "dual" | "dub">("sub");
// Is spoiler checkbox
const isSpoiler = ref(false);

// Format actors with fallback avatars
const actorList = computed(() => {
  const rawActors = title.value?.actors;
  let names: string[] = [];
  if (Array.isArray(rawActors)) {
    names = rawActors;
  } else if (typeof rawActors === "string" && rawActors.trim()) {
    names = rawActors
      .split(",")
      .map((s) => s.trim())
      .filter(Boolean);
  }

  if (!names.length) {
    return [
      {
        name: "Gong Hyo Jin",
        avatar:
          "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
      },
      {
        name: "Choi Woo-sung",
        avatar:
          "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
      },
      {
        name: "Sung Dong-il",
        avatar:
          "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80",
      },
      {
        name: "Lee Eun-saem",
        avatar:
          "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80",
      },
      {
        name: "Moo Jin-sung",
        avatar:
          "https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150&auto=format&fit=crop&q=80",
      },
      {
        name: "Lee Sang-yi",
        avatar:
          "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=150&auto=format&fit=crop&q=80",
      },
    ];
  }
  return names.map((name, i) => ({
    name,
    avatar: `https://images.unsplash.com/photo-${1500000000000 + ((i * 12345678) % 50000000)}?w=150&auto=format&fit=crop&q=80`,
  }));
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
              <h1
                class="text-2xl sm:text-3xl font-black text-white font-display"
              >
                {{ title.title }}
              </h1>
              <p
                class="text-sm font-semibold text-primary/80 mt-1 font-display"
              >
                {{
                  title.slug
                    .split("-")
                    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
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
              <span class="rounded bg-white/10 px-2 py-0.5 text-gray-200">
                T16
              </span>
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
              <span
                >Đã chiếu: Tập {{ episodeList.length }} /
                {{ title.totalEpisodes || episodeList.length }}</span
              >
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
            <div class="border-t border-white/10 pt-4">
              <h3 class="text-sm font-bold text-white mb-3 font-display">
                Diễn viên
              </h3>
              <div class="grid grid-cols-3 gap-3 text-center">
                <div
                  v-for="actor in actorList.slice(0, 6)"
                  :key="actor.name"
                  class="flex flex-col items-center"
                >
                  <img
                    :src="actor.avatar"
                    :alt="actor.name"
                    class="size-14 rounded-full object-cover border border-white/15 shadow-sm transition hover:scale-105"
                  />
                  <span
                    class="mt-1.5 text-[11px] font-medium text-gray-200 line-clamp-1"
                  >
                    {{ actor.name }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Weekly Leaderboard Sidebar Component -->
            <div class="border-t border-white/10 pt-4">
              <WeeklyLeaderboard :titles="catalog?.items || []" :limit="10" />
            </div>
          </aside>

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
            <div v-if="activeTab === 'episodes'" class="flex flex-col gap-6">
              <!-- Audio & Server Toggle Row -->
              <div class="flex flex-wrap items-center justify-between gap-4">
                <div class="flex items-center gap-3">
                  <!-- Season Dropdown -->
                  <div
                    class="rounded-xl border border-white/10 bg-[#191b24] px-3.5 py-2 text-xs font-bold text-white"
                  >
                    Phần 1 ▾
                  </div>

                  <!-- Audio format buttons -->
                  <div class="flex items-center gap-1.5">
                    <button
                      type="button"
                      class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                      :class="
                        activeAudio === 'sub'
                          ? 'border-primary bg-primary/10 text-primary font-bold'
                          : 'border-white/10 bg-white/5 text-white/80'
                      "
                      @click="activeAudio = 'sub'"
                    >
                      Phụ đề #1
                    </button>
                    <button
                      type="button"
                      class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                      :class="
                        activeAudio === 'dual'
                          ? 'border-primary bg-primary/10 text-primary font-bold'
                          : 'border-white/10 bg-white/5 text-white/80'
                      "
                      @click="activeAudio = 'dual'"
                    >
                      Song ngữ
                    </button>
                    <button
                      type="button"
                      class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                      :class="
                        activeAudio === 'dub'
                          ? 'border-primary bg-primary/10 text-primary font-bold'
                          : 'border-white/10 bg-white/5 text-white/80'
                      "
                      @click="activeAudio = 'dub'"
                    >
                      Thuyết Minh #1
                    </button>
                  </div>
                </div>

                <!-- Rút gọn toggle -->
                <div class="flex items-center gap-2 text-xs text-white/70">
                  <span>Rút gọn</span>
                  <div
                    class="h-5 w-9 rounded-full bg-primary p-0.5 cursor-pointer"
                  >
                    <div
                      class="size-4 rounded-full bg-black translate-x-4 transition"
                    />
                  </div>
                </div>
              </div>

              <!-- Episode Grid Buttons -->
              <div
                class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2.5"
              >
                <NuxtLink
                  v-for="ep in episodeList"
                  :key="ep.episodeNumber"
                  :to="{
                    path: `/watch/${title.slug}`,
                    query: { episode: ep.episodeNumber },
                  }"
                  class="flex items-center justify-center gap-2 rounded-2xl border border-white/10 bg-[#191b24] py-3 text-xs font-bold text-white transition hover:border-primary hover:bg-primary/15 hover:text-primary active:scale-95"
                >
                  <Play class="size-3.5 fill-current" />
                  <span>Tập {{ ep.episodeNumber }}</span>
                </NuxtLink>
              </div>
            </div>

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
              <div
                v-for="actor in actorList"
                :key="actor.name"
                class="flex flex-col items-center p-4 rounded-2xl bg-[#191b24] border border-white/10 text-center"
              >
                <img
                  :src="actor.avatar"
                  :alt="actor.name"
                  class="size-16 rounded-full object-cover border border-primary/50 shadow"
                />
                <h4 class="mt-2 text-xs font-bold text-white">
                  {{ actor.name }}
                </h4>
                <p class="text-[11px] text-muted-foreground mt-0.5">
                  Diễn viên chính
                </p>
              </div>
            </div>

            <!-- Tab 4: Đề xuất -->
            <div
              v-else-if="activeTab === 'recommendations'"
              class="grid grid-cols-2 sm:grid-cols-4 gap-4"
            >
              <MovieCard
                v-for="item in related.slice(0, 8)"
                :key="item.slug"
                :title="item"
                variant="vertical"
              />
            </div>

            <!-- COMMENTS & REVIEWS SECTION -->
            <section class="mt-8 pt-8 border-t border-white/10">
              <!-- Comment Header with Tabs -->
              <div class="flex items-center justify-between mb-5">
                <h3
                  class="text-base sm:text-lg font-bold text-white flex items-center gap-2 font-display"
                >
                  <MessageSquare class="size-5 text-primary" />
                  <span>Bình luận ({{ reviews?.length || 2 }})</span>
                </h3>

                <div
                  class="flex items-center rounded-xl border border-white/10 bg-[#191b24] p-1 text-xs font-bold"
                >
                  <button
                    type="button"
                    class="rounded-lg px-3 py-1.5 transition"
                    :class="
                      commentTab === 'comment'
                        ? 'bg-white text-black shadow-sm'
                        : 'text-white/70 hover:text-white'
                    "
                    @click="commentTab = 'comment'"
                  >
                    Bình luận
                  </button>
                  <button
                    type="button"
                    class="rounded-lg px-3 py-1.5 transition"
                    :class="
                      commentTab === 'rating'
                        ? 'bg-white text-black shadow-sm'
                        : 'text-white/70 hover:text-white'
                    "
                    @click="commentTab = 'rating'"
                  >
                    Đánh giá
                  </button>
                </div>
              </div>

              <!-- Comment Form Box -->
              <div
                class="rounded-3xl border border-white/10 bg-[#141622] p-4 sm:p-5 shadow-lg"
              >
                <textarea
                  v-model="reviewComment"
                  rows="3"
                  placeholder="Viết bình luận..."
                  maxlength="1000"
                  class="w-full resize-none bg-transparent text-sm text-white placeholder-white/40 outline-none"
                />

                <div
                  class="mt-3 flex flex-wrap items-center justify-between gap-3 border-t border-white/5 pt-3 text-xs text-white/60"
                >
                  <!-- Left options -->
                  <div class="flex items-center gap-4">
                    <label
                      class="flex items-center gap-2 cursor-pointer select-none"
                    >
                      <input
                        v-model="isSpoiler"
                        type="checkbox"
                        class="size-4 rounded border-white/20 bg-white/10 text-primary focus:ring-0"
                      />
                      <span>Tiết lộ?</span>
                    </label>
                    <button
                      type="button"
                      class="flex items-center gap-1.5 hover:text-white transition"
                    >
                      <Smile class="size-4 text-amber-400" />
                      <span>Popo</span>
                    </button>
                  </div>

                  <!-- Right actions -->
                  <div class="flex items-center gap-3">
                    <span class="text-[11px] text-muted-foreground"
                      >{{ reviewComment.length }} / 1000</span
                    >
                    <button
                      type="button"
                      :disabled="isSubmittingReview || !reviewComment.trim()"
                      class="inline-flex items-center gap-1.5 rounded-xl bg-primary px-4 py-2 font-bold text-black shadow transition hover:bg-[#ffde8a] active:scale-95 disabled:opacity-40"
                      @click="submitReview"
                    >
                      <span>Gửi</span>
                      <Send class="size-3.5" />
                    </button>
                  </div>
                </div>
              </div>

              <!-- Comments List -->
              <div class="mt-6 flex flex-col gap-4">
                <!-- Fallback Mock Comments if empty -->
                <div
                  class="flex items-start gap-3.5 rounded-2xl bg-[#191b24]/60 p-4 border border-white/5"
                >
                  <img
                    src="https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100&auto=format&fit=crop&q=80"
                    alt="Tuymh"
                    class="size-10 rounded-full object-cover border border-primary/40"
                  />
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 text-xs">
                      <span class="font-bold text-white">Tuymh</span>
                      <span class="text-primary font-bold">∞</span>
                      <span class="text-white/40">5 ngày trước</span>
                      <span
                        class="rounded bg-white/10 px-1.5 py-0.5 text-[10px] text-white/70"
                        >P.1 - Tập 1</span
                      >
                    </div>
                    <p class="mt-1 text-xs sm:text-sm text-gray-200">
                      ra hết chưa mn? Phim xem cuốn quá trời luôn
                    </p>
                    <div
                      class="mt-2.5 flex items-center gap-4 text-xs text-white/60 font-semibold"
                    >
                      <button
                        type="button"
                        class="flex items-center gap-1 hover:text-primary"
                      >
                        <ThumbsUp class="size-3.5" />
                      </button>
                      <button
                        type="button"
                        class="flex items-center gap-1 hover:text-red-400"
                      >
                        <ThumbsDown class="size-3.5" />
                      </button>
                      <button type="button" class="hover:text-white">
                        Trả lời
                      </button>
                      <button type="button" class="hover:text-white">
                        ··· Thêm
                      </button>
                    </div>
                  </div>
                </div>

                <div
                  class="flex items-start gap-3.5 rounded-2xl bg-[#191b24]/60 p-4 border border-white/5"
                >
                  <img
                    src="https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=100&auto=format&fit=crop&q=80"
                    alt="Nhuu qnhuy"
                    class="size-10 rounded-full object-cover border border-primary/40"
                  />
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 text-xs">
                      <span class="font-bold text-white">Nhuu qnhuy</span>
                      <span class="text-primary font-bold">∞</span>
                      <span class="text-white/40">10:58 18/8/2026</span>
                    </div>
                    <p class="mt-1 text-xs sm:text-sm text-gray-200">
                      Phimm hayy quá điii, nữ chính diễn xuất đỉnh nóc kịch trần
                    </p>
                    <div
                      class="mt-2.5 flex items-center gap-4 text-xs text-white/60 font-semibold"
                    >
                      <button
                        type="button"
                        class="flex items-center gap-1 hover:text-primary"
                      >
                        <ThumbsUp class="size-3.5" />
                      </button>
                      <button
                        type="button"
                        class="flex items-center gap-1 hover:text-red-400"
                      >
                        <ThumbsDown class="size-3.5" />
                      </button>
                      <button type="button" class="hover:text-white">
                        Trả lời
                      </button>
                      <button type="button" class="hover:text-white">
                        ··· Thêm
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          </div>
        </div>
      </main>
    </template>

    <div v-else-if="error" class="mx-auto max-w-lg py-32 text-center">
      <h2 class="text-xl font-bold text-red-400">
        Không tìm thấy thông tin phim
      </h2>
      <NuxtLink to="/" class="mt-4 inline-block text-sm text-primary underline"
        >Quay lại trang chủ</NuxtLink
      >
    </div>

    <!-- YouTube Trailer Modal -->
    <div
      v-if="isTrailerModalOpen && title?.trailerUrl"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md"
    >
      <div
        class="relative w-full max-w-4xl overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-950 shadow-2xl"
      >
        <div
          class="flex items-center justify-between border-b border-zinc-800 px-4 py-3"
        >
          <h3 class="text-sm font-semibold text-white">
            Trailer: {{ title.title }}
          </h3>
          <button
            type="button"
            class="rounded p-1 text-zinc-400 hover:text-white"
            @click="isTrailerModalOpen = false"
          >
            <X class="size-5" />
          </button>
        </div>
        <div class="aspect-video w-full bg-black">
          <iframe
            :src="getEmbedTrailerUrl(title.trailerUrl)"
            class="h-full w-full border-0"
            allow="
              accelerometer;
              autoplay;
              clipboard-write;
              encrypted-media;
              gyroscope;
              picture-in-picture;
            "
            allowfullscreen
          />
        </div>
      </div>
    </div>

    <AppFooter />
  </div>
</template>
