<script setup lang="ts">
import {
  BookmarkPlus,
  Check,
  ChevronLeft,
  CircleAlert,
  Download,
  Expand,
  LoaderCircle,
  Pause,
  Play,
  Settings,
  Share2,
  Star,
  Volume2,
  VolumeX,
} from "@lucide/vue";
import {
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogOverlay,
  AlertDialogPortal,
  AlertDialogRoot,
  AlertDialogTitle,
} from "reka-ui";

type Title = {
  slug: string;
  title: string;
  synopsis: string;
  genre: string;
  year: number;
  type: string;
  posterUrl: string;
  runtimeMinutes: number;
  viewCount: number;
};
type Episode = { number: number; name: string; hlsUrl: string };
type Playback = {
  slug: string;
  title: string;
  isSeries: boolean;
  episodes: Episode[];
};
type ViewRecordedResponse = { viewCount: number; counted: boolean };
type LibraryHistory = {
  title: { slug: string };
  episodeNumber: number | null;
  progressSeconds: number;
};
type LibraryResponse = { saved: { slug: string }[]; history: LibraryHistory[] };

const route = useRoute();
const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const { $api } = useNuxtApp();
const video = ref<HTMLVideoElement | null>(null);
const playerFrame = ref<HTMLElement | null>(null);
const selectedEpisode = ref(0);
const playerError = ref("");
const isLoading = ref(true);
const isPlaying = ref(false);
const currentTime = ref(0);
const duration = ref(0);
const volume = ref(0.85);
const isMuted = ref(false);
const isSettingsOpen = ref(false);
const playbackRate = ref(1);
const selectedQuality = ref(-1);
const qualityOptions = ref<{ level: number; label: string }[]>([]);
const isUnavailableDialogOpen = ref(false);
const isInMyList = ref(false);
const isSynopsisExpanded = ref(false);
const actionNotice = ref("");
const viewCount = ref(0);
const hasRecordedView = ref(false);
let hls: {
  destroy: () => void;
  currentLevel: number;
  levels: { height: number }[];
} | null = null;
let lastProgressSaved = 0;
let isSavingProgress = false;
let resumeSeconds = 0;

const { data: title, error: titleError } = await useAsyncData(
  `watch-title-${route.params.slug}`,
  () =>
    $api<Title>(`/v1/catalog/titles/${route.params.slug}`, {
      query: { locale: locale.value },
    }),
);
const { data: playback, error: playbackError } = await useAsyncData(
  `watch-playback-${route.params.slug}`,
  () =>
    $api<Playback>(`/v1/catalog/titles/${route.params.slug}/playback`, {
      query: { locale: locale.value },
    }),
);
const requestedEpisode = Number(route.query.episode);
if (
  Number.isInteger(requestedEpisode) &&
  requestedEpisode > 0 &&
  playback.value?.isSeries
) {
  const requestedIndex = playback.value.episodes.findIndex(
    (item) => item.number === requestedEpisode,
  );
  if (requestedIndex >= 0) selectedEpisode.value = requestedIndex;
}

const episode = computed(() => playback.value?.episodes[selectedEpisode.value]);
const copy = computed(() =>
  locale.value === "vi"
    ? {
        back: "Quay lại chi tiết",
        episodes: "Danh sách phát",
        now: "Đang xem",
        unavailable: "Không thể tải video. Hãy thử lại.",
        unavailableTitle: "Video hiện chưa khả dụng",
        unavailableDescription:
          "Phim này chưa có nguồn phát hợp lệ hoặc nguồn phát đang gặp sự cố. Vui lòng thử lại sau.",
        returnToTitle: "Quay lại trang phim",
      }
    : {
        back: "Back to details",
        episodes: "Playlist",
        now: "Now watching",
        unavailable: "Unable to load the video. Please try again.",
        unavailableTitle: "Video unavailable",
        unavailableDescription:
          "This title has no valid source yet, or its source is currently unavailable. Please try again later.",
        returnToTitle: "Back to title",
      },
);

function showUnavailableDialog() {
  playerError.value = copy.value.unavailable;
  isLoading.value = false;
  isUnavailableDialogOpen.value = true;
}

async function loadEpisode() {
  const element = video.value;
  const source = episode.value?.hlsUrl;
  if (!element || !source) {
    showUnavailableDialog();
    return;
  }

  playerError.value = "";
  isLoading.value = true;
  hls?.destroy();
  hls = null;
  qualityOptions.value = [];
  selectedQuality.value = -1;
  element.pause();
  element.playbackRate = playbackRate.value;
  element.removeAttribute("src");
  element.load();

  if (element.canPlayType("application/vnd.apple.mpegurl")) {
    element.src = source;
  } else {
    const { default: Hls } = await import("hls.js");
    if (!Hls.isSupported()) {
      showUnavailableDialog();
      return;
    }
    const instance = new Hls();
    instance.on(Hls.Events.ERROR, (_event, data) => {
      if (data.fatal) showUnavailableDialog();
    });
    instance.on(Hls.Events.MANIFEST_PARSED, () => {
      qualityOptions.value = instance.levels
        .map((level, index) => ({
          level: index,
          label: level.height ? `${level.height}p` : `Quality ${index + 1}`,
        }))
        .filter(
          (option, index, options) =>
            options.findIndex((x) => x.label === option.label) === index,
        )
        .reverse();
    });
    instance.loadSource(source);
    instance.attachMedia(element);
    hls = instance;
  }
}

function applyResumePosition() {
  const element = video.value;
  if (
    !element ||
    !resumeSeconds ||
    !Number.isFinite(element.duration) ||
    element.duration <= 0
  )
    return;
  // Avoid resuming into the end credits/completed state.
  if (resumeSeconds < element.duration - 10) {
    element.currentTime = resumeSeconds;
    currentTime.value = resumeSeconds;
    lastProgressSaved = resumeSeconds;
  }
  resumeSeconds = 0;
}

async function loadResumePosition() {
  if (!title.value) return;
  try {
    const library = await $api<LibraryResponse>("/v1/me/library", {
      credentials: "include",
      query: { locale: locale.value },
    });
    isInMyList.value = library.saved.some(
      (item) => item.slug === title.value?.slug,
    );
    const currentEpisode = playback.value?.isSeries
      ? (episode.value?.number ?? null)
      : null;
    resumeSeconds =
      library.history.find(
        (item) =>
          item.title.slug === title.value?.slug &&
          item.episodeNumber === currentEpisode,
      )?.progressSeconds ?? 0;
    applyResumePosition();
  } catch {
    // Resume state is optional and may be unavailable for anonymous viewers.
  }
}

function selectEpisode(index: number) {
  selectedEpisode.value = index;
}

async function toggleMyList() {
  if (!title.value) return;
  try {
    await $api(`/v1/me/saved/${title.value.slug}`, {
      method: isInMyList.value ? "DELETE" : "PUT",
      credentials: "include",
    });
    isInMyList.value = !isInMyList.value;
  } catch {
    actionNotice.value = "Hãy đăng nhập để lưu phim vào danh sách.";
  }
}

async function recordWatchProgress() {
  if (
    isSavingProgress ||
    !title.value ||
    currentTime.value < 5 ||
    !Number.isFinite(currentTime.value)
  )
    return;
  isSavingProgress = true;
  const progressSeconds = currentTime.value;
  try {
    await $api(`/v1/me/history/${title.value.slug}`, {
      method: "POST",
      credentials: "include",
      body: {
        episodeNumber: playback.value?.isSeries ? episode.value?.number : null,
        progressSeconds,
      },
    });
    lastProgressSaved = progressSeconds;
  } catch {
    // Progress saving is best effort and must not interrupt playback.
  } finally {
    isSavingProgress = false;
  }
}

function onTimeUpdate() {
  currentTime.value = video.value?.currentTime || 0;
  if (
    currentTime.value >= 5 &&
    (lastProgressSaved === 0 || currentTime.value - lastProgressSaved >= 30)
  )
    void recordWatchProgress();
}

function onVideoPause() {
  isPlaying.value = false;
  void recordWatchProgress();
}

async function recordView() {
  if (hasRecordedView.value || !title.value) return;
  hasRecordedView.value = true;
  try {
    const result = await $api<ViewRecordedResponse>(
      `/v1/catalog/titles/${title.value.slug}/views`,
      {
        method: "POST",
        credentials: "include",
        body: {
          episodeNumber: playback.value?.isSeries
            ? episode.value?.number
            : null,
        },
      },
    );
    viewCount.value = result.viewCount;
  } catch {
    hasRecordedView.value = false;
  }
}

function onVideoPlay() {
  isPlaying.value = true;
  void recordView();
}

function formatViews(count: number) {
  if (count >= 1_000_000)
    return `${(count / 1_000_000).toFixed(count >= 10_000_000 ? 0 : 1)}M`;
  if (count >= 1_000)
    return `${(count / 1_000).toFixed(count >= 10_000 ? 0 : 1)}K`;
  return String(count);
}

function togglePlayback() {
  if (!video.value) return;
  if (video.value.paused) video.value.play().catch(showUnavailableDialog);
  else video.value.pause();
}

function seek(event: Event) {
  if (!video.value) return;
  video.value.currentTime = Number((event.target as HTMLInputElement).value);
}

function changeVolume(event: Event) {
  const value = Number((event.target as HTMLInputElement).value);
  volume.value = value;
  isMuted.value = value === 0;
  if (video.value) video.value.volume = value;
}

function toggleMute() {
  if (!video.value) return;
  isMuted.value = !isMuted.value;
  video.value.muted = isMuted.value;
}

function setPlaybackRate(rate: number) {
  playbackRate.value = rate;
  if (video.value) video.value.playbackRate = rate;
  isSettingsOpen.value = false;
}

function setQuality(level: number) {
  selectedQuality.value = level;
  if (hls) hls.currentLevel = level;
}

async function toggleFullscreen() {
  if (!playerFrame.value) return;
  if (document.fullscreenElement) await document.exitFullscreen();
  else await playerFrame.value.requestFullscreen();
}

function formatTime(seconds: number) {
  if (!Number.isFinite(seconds)) return "0:00";
  const minutes = Math.floor(seconds / 60);
  return `${minutes}:${Math.floor(seconds % 60)
    .toString()
    .padStart(2, "0")}`;
}

function rangeStyle(value: number, max: number) {
  const percent = max > 0 ? Math.min(100, Math.max(0, (value / max) * 100)) : 0;
  return {
    background: `linear-gradient(to right, #d98367 0%, #d98367 ${percent}%, rgba(255,255,255,.28) ${percent}%, rgba(255,255,255,.28) 100%)`,
  };
}

async function shareTitle() {
  try {
    if (navigator.share)
      await navigator.share({
        title: title.value?.title,
        url: window.location.href,
      });
    else {
      await navigator.clipboard?.writeText(window.location.href);
      actionNotice.value = "Đã sao chép liên kết.";
    }
  } catch {
    // Sharing is best effort when browser permissions or APIs are unavailable.
  }
}

watch(selectedEpisode, () => {
  hasRecordedView.value = false;
  lastProgressSaved = 0;
  isSavingProgress = false;
  void loadEpisode();
  void loadResumePosition();
});
watchEffect(() => {
  if (
    titleError.value ||
    playbackError.value ||
    (playback.value && !playback.value.episodes.length)
  )
    showUnavailableDialog();
});
onMounted(() => {
  if (video.value) video.value.volume = volume.value;
  viewCount.value = title.value?.viewCount ?? 0;
  void loadResumePosition();
  void loadEpisode();
});
onBeforeUnmount(() => {
  void recordWatchProgress();
  hls?.destroy();
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <AlertDialogRoot v-model:open="isUnavailableDialogOpen">
      <AlertDialogPortal>
        <AlertDialogOverlay
          class="fixed inset-0 z-[100] bg-black/70 backdrop-blur-sm"
        />
        <AlertDialogContent
          class="fixed left-1/2 top-1/2 z-[101] w-[calc(100%-2.5rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-3xl border border-white/10 bg-surface-container p-7 shadow-2xl outline-none"
        >
          <div
            class="flex size-11 items-center justify-center rounded-2xl bg-destructive/15 text-destructive"
          >
            <CircleAlert class="size-6" />
          </div>
          <AlertDialogTitle class="mt-5 font-display text-3xl font-semibold">{{
            copy.unavailableTitle
          }}</AlertDialogTitle>
          <AlertDialogDescription
            class="mt-3 leading-relaxed text-muted-foreground"
            >{{ copy.unavailableDescription }}</AlertDialogDescription
          >
          <NuxtLink
            :to="title ? `/movies/${title.slug}` : '/browse'"
            class="mt-7 inline-flex cursor-pointer items-center justify-center rounded-2xl bg-primary-container px-5 py-3 text-sm font-semibold text-primary-container-foreground transition hover:bg-primary"
            @click="isUnavailableDialogOpen = false"
          >
            {{ copy.returnToTitle }}
          </NuxtLink>
        </AlertDialogContent>
      </AlertDialogPortal>
    </AlertDialogRoot>
    <template v-if="title && playback">
      <section class="mx-auto max-w-360 px-5 py-7 lg:px-12">
        <NuxtLink
          :to="`/movies/${title.slug}`"
          class="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-primary"
        >
          <ChevronLeft class="size-4" />{{ copy.back }}
        </NuxtLink>
        <div
          class="mt-5 overflow-hidden rounded-3xl border border-white/10 bg-black"
        >
          <div ref="playerFrame" class="relative aspect-video group">
            <video
              ref="video"
              class="size-full bg-black object-contain"
              playsinline
              :poster="title.posterUrl"
              @click="togglePlayback"
              @canplay="isLoading = false"
              @loadedmetadata="
                duration = video?.duration || 0;
                applyResumePosition();
              "
              @timeupdate="onTimeUpdate"
              @play="onVideoPlay"
              @pause="onVideoPause"
              @ended="onVideoPause"
              @error="showUnavailableDialog"
            />
            <div
              v-if="isLoading"
              class="pointer-events-none absolute inset-0 grid place-items-center bg-black/40"
            >
              <LoaderCircle class="size-9 animate-spin text-primary" />
            </div>
            <p
              v-if="playerError"
              class="absolute inset-x-4 top-4 rounded-xl bg-black/80 px-4 py-3 text-sm text-white"
            >
              {{ playerError }}
            </p>
            <button
              v-if="!isPlaying && !isLoading"
              class="absolute left-1/2 top-1/2 grid size-18 -translate-x-1/2 -translate-y-1/2 cursor-pointer place-items-center rounded-full bg-primary-container text-primary-container-foreground shadow-[0_0_50px_rgba(217,131,103,.5)] transition hover:scale-105"
              aria-label="Play"
              @click="togglePlayback"
            >
              <Play class="size-8 fill-current" />
            </button>
            <div
              class="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/90 via-black/50 to-transparent px-5 pb-5 pt-16"
            >
              <input
                class="player-range mb-4 w-full"
                type="range"
                min="0"
                :max="duration || 0"
                step="0.1"
                :value="currentTime"
                :style="rangeStyle(currentTime, duration)"
                aria-label="Video progress"
                @input="seek"
              />
              <div class="flex items-center justify-between gap-4 text-white">
                <div class="flex items-center gap-3 sm:gap-4">
                  <button
                    class="player-control grid size-10 place-items-center rounded-full bg-white/10 transition hover:bg-primary-container hover:text-primary-container-foreground"
                    :aria-label="isPlaying ? 'Pause' : 'Play'"
                    @click="togglePlayback"
                  >
                    <Pause v-if="isPlaying" class="size-5 fill-current" /><Play
                      v-else
                      class="size-5 fill-current"
                    />
                  </button>
                  <button
                    class="player-control grid size-9 place-items-center"
                    aria-label="Mute"
                    @click="toggleMute"
                  >
                    <VolumeX v-if="isMuted" class="size-5" /><Volume2
                      v-else
                      class="size-5"
                    />
                  </button>
                  <input
                    class="player-range hidden w-20 sm:block"
                    type="range"
                    min="0"
                    max="1"
                    step="0.05"
                    :value="isMuted ? 0 : volume"
                    :style="rangeStyle(isMuted ? 0 : volume, 1)"
                    aria-label="Volume"
                    @input="changeVolume"
                  />
                  <span
                    class="whitespace-nowrap text-xs tabular-nums text-white/80"
                    >{{ formatTime(currentTime) }} /
                    {{ formatTime(duration) }}</span
                  >
                </div>
                <div class="relative flex items-center gap-2">
                  <button
                    class="player-control hidden size-9 place-items-center sm:grid"
                    aria-label="Playback settings"
                    @click="isSettingsOpen = !isSettingsOpen"
                  >
                    <Settings class="size-5 text-white/80" />
                  </button>
                  <div
                    v-if="isSettingsOpen"
                    class="absolute bottom-12 right-0 w-40 overflow-hidden rounded-xl border border-white/10 bg-[#202020] p-1 shadow-2xl"
                  >
                    <p class="px-3 py-2 text-xs font-semibold text-white/55">
                      Chất lượng
                    </p>
                    <button
                      class="player-control flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm hover:bg-white/10"
                      :class="selectedQuality === -1 ? 'text-primary' : ''"
                      @click="setQuality(-1)"
                    >
                      <span>Tự động</span
                      ><span v-if="selectedQuality === -1">✓</span>
                    </button>
                    <button
                      v-for="option in qualityOptions"
                      :key="option.level"
                      class="player-control flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm hover:bg-white/10"
                      :class="
                        selectedQuality === option.level ? 'text-primary' : ''
                      "
                      @click="setQuality(option.level)"
                    >
                      <span>{{ option.label }}</span
                      ><span v-if="selectedQuality === option.level">✓</span>
                    </button>
                    <p
                      v-if="!qualityOptions.length"
                      class="px-3 pb-2 text-xs text-white/45"
                    >
                      Stream hiện có một chất lượng.
                    </p>
                    <div class="mx-2 my-1 border-t border-white/10" />
                    <p class="px-3 py-2 text-xs font-semibold text-white/55">
                      Tốc độ phát
                    </p>
                    <button
                      v-for="rate in [0.75, 1, 1.25, 1.5, 2]"
                      :key="rate"
                      class="player-control flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm hover:bg-white/10"
                      :class="playbackRate === rate ? 'text-primary' : ''"
                      @click="setPlaybackRate(rate)"
                    >
                      <span>{{ rate }}x</span
                      ><span v-if="playbackRate === rate">✓</span>
                    </button>
                  </div>
                  <button
                    class="player-control grid size-9 place-items-center"
                    aria-label="Fullscreen"
                    @click="toggleFullscreen"
                  >
                    <Expand class="size-5" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="mx-auto max-w-360 px-5 pb-20 pt-2 lg:px-12 lg:pt-5">
        <article>
          <div
            class="rounded-b-3xl border-x border-b border-white/7 bg-surface-container-low px-5 py-5 lg:px-7 lg:py-5"
          >
            <div
              class="flex flex-wrap items-center justify-between gap-x-8 gap-y-2 text-sm"
            >
              <p class="font-semibold text-foreground">
                {{
                  playback.isSeries
                    ? `Tập ${episode?.number}: ${episode?.name}`
                    : title.title
                }}
              </p>
              <p class="text-xs text-muted-foreground lg:text-sm">
                {{ formatViews(viewCount) }} lượt xem
                <span class="mx-2 text-white/20">·</span> Thời lượng:
                {{ title.runtimeMinutes }} phút
              </p>
            </div>
            <div
              class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-muted-foreground"
            >
              <span
                class="inline-flex items-center gap-1 font-semibold text-primary"
                ><Star class="size-3.5 fill-current" /> 4.8</span
              >
              <button
                class="inline-flex items-center gap-1 transition hover:text-primary"
                @click="shareTitle"
              >
                <Share2 class="size-3" /> Chia sẻ
              </button>
              <button
                class="inline-flex items-center gap-1 transition hover:text-primary"
                @click="toggleMyList"
              >
                <BookmarkPlus class="size-3" />
                {{ isInMyList ? "Đã thêm" : "Thêm vào DS" }}
              </button>
              <button
                class="inline-flex items-center gap-1 transition hover:text-primary"
              >
                <Download class="size-3" /> Tải xuống
              </button>
            </div>
            <p
              class="mt-3 max-w-4xl text-sm leading-6 text-muted-foreground line-clamp-2"
            >
              {{ title.synopsis }}
            </p>
          </div>
          <div
            class="mt-6 flex flex-wrap items-center justify-between gap-x-6 gap-y-4 border-b border-white/7 pb-5"
          >
            <div
              class="flex flex-wrap items-center gap-2.5 text-xs font-semibold text-foreground/80"
            >
              <span class="rounded-sm bg-surface-container-high px-2 py-1"
                >HD</span
              >
              <span class="rounded-sm bg-surface-container-high px-2 py-1"
                >16+</span
              >
              <span class="inline-flex items-center gap-1 text-primary"
                ><Star class="size-3.5 fill-current" /> 9.2</span
              ><span>{{ title.year }}</span>
            </div>
            <div class="flex items-center gap-2">
              <button
                class="inline-flex h-10 items-center gap-2 rounded-full border border-[#d98367]/45 bg-[#d98367]/10 px-5 text-xs font-semibold text-primary transition hover:bg-primary-container hover:text-primary-container-foreground"
                @click="toggleMyList"
              >
                <Check v-if="isInMyList" class="size-4" /><BookmarkPlus
                  v-else
                  class="size-4"
                />{{ isInMyList ? "Đã thêm" : "Thêm vào DS" }}
              </button>
              <button
                class="inline-flex size-10 items-center justify-center rounded-full bg-surface-container-high text-muted-foreground transition hover:bg-surface-container-highest hover:text-foreground"
                aria-label="Chia sẻ"
                @click="shareTitle"
              >
                <Share2 class="size-4" />
              </button>
            </div>
          </div>
          <p
            class="mt-4 max-w-3xl text-[15px] leading-7 text-foreground/90"
            :class="isSynopsisExpanded ? '' : 'line-clamp-3'"
          >
            {{ title.synopsis }}
          </p>
          <button
            v-if="title.synopsis.length > 150"
            class="mt-1 text-xs font-semibold text-primary transition hover:text-primary-container"
            @click="isSynopsisExpanded = !isSynopsisExpanded"
          >
            {{ isSynopsisExpanded ? "Thu gọn" : "Xem thêm" }}
          </button>
          <p
            v-if="actionNotice"
            class="mt-2 text-xs text-primary"
            role="status"
          >
            {{ actionNotice }}
          </p>
        </article>

        <section v-if="playback.episodes.length" class="mt-11">
          <div
            class="flex items-center justify-between border-b border-white/7 pb-4"
          >
            <h2
              class="font-display text-2xl font-semibold tracking-[-.02em] lg:text-[1.8rem]"
            >
              Tập tiếp theo
            </h2>
            <button
              class="text-xs font-semibold text-primary transition hover:text-primary-container"
            >
              Xem tất cả <span class="ml-1">→</span>
            </button>
          </div>
          <div
            class="mt-5 grid gap-5 sm:grid-cols-2 lg:max-w-250 lg:grid-cols-3"
          >
            <button
              v-for="(item, index) in playback.episodes.slice(0, 3)"
              :key="item.number"
              class="group text-left"
              @click="selectEpisode(index)"
            >
              <span
                class="relative block aspect-video overflow-hidden rounded-lg border border-white/8 bg-surface-container-high"
              >
                <img
                  :src="title.posterUrl"
                  :alt="item.name"
                  class="size-full object-cover opacity-70 transition duration-300 group-hover:scale-105 group-hover:opacity-90"
                />
                <span
                  class="absolute inset-0 bg-gradient-to-t from-black/65 via-transparent to-transparent"
                />
                <span
                  v-if="index === selectedEpisode"
                  class="absolute bottom-2 right-2 grid size-7 place-items-center rounded-full bg-primary-container text-primary-container-foreground"
                  ><Play class="size-3 fill-current"
                /></span>
                <span
                  v-else
                  class="absolute bottom-2 right-2 rounded bg-black/60 px-1.5 py-0.5 text-[9px] font-semibold text-white/75"
                  >{{ playback.isSeries ? `Tập ${item.number}` : "HD" }}</span
                >
              </span>
              <b
                class="mt-2.5 block truncate text-sm font-semibold text-foreground transition group-hover:text-primary"
                >{{ playback.isSeries ? item.name : title.title }}</b
              >
              <span class="mt-1 block text-xs text-muted-foreground">{{
                playback.isSeries
                  ? `Tập ${item.number}`
                  : `${title.runtimeMinutes} phút`
              }}</span>
            </button>
          </div>
        </section>
      </section>
      <footer
        class="border-t border-white/5 bg-surface-container-lowest px-5 py-10 text-center lg:px-12"
      >
        <NuxtLink to="/" class="font-display text-sm font-semibold text-primary"
          >ZMovie</NuxtLink
        >
        <nav
          class="mt-4 flex flex-wrap justify-center gap-x-6 gap-y-2 text-[10px] text-muted-foreground"
        >
          <a href="#">Privacy Policy</a><a href="#">Terms of Service</a
          ><a href="#">Help Center</a><a href="#">Contact Us</a>
        </nav>
        <p class="mt-5 text-[10px] text-tertiary">
          © 2026 ZMovie Premium. All rights reserved.
        </p>
      </footer>
    </template>
  </main>
</template>

<style scoped>
.player-range {
  appearance: none;
  height: 5px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.28);
  accent-color: #d98367;
  cursor: pointer;
}
.player-range::-webkit-slider-thumb {
  appearance: none;
  width: 13px;
  height: 13px;
  border-radius: 999px;
  background: #d98367;
  box-shadow: 0 0 0 3px rgba(217, 131, 103, 0.2);
}
.player-range::-moz-range-thumb {
  width: 13px;
  height: 13px;
  border: 0;
  border-radius: 999px;
  background: #d98367;
}
.player-control {
  cursor: pointer;
}
.player-control:hover {
  cursor: pointer;
}
button {
  cursor: pointer;
}
</style>
