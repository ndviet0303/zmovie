<script setup lang="ts">
import {
  BookmarkPlus,
  Check,
  ChevronLeft,
  CircleAlert,
  Download,
  Expand,
  Flag,
  Heart,
  LoaderCircle,
  Pause,
  Play,
  Settings,
  Share2,
  Star,
  Subtitles,
  Users,
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

import type { TitleDetail } from "~/types/catalog";
import type { LocalWatchProgress } from "~/types/watch";
import {
  fetchCatalogPlayback,
  fetchCatalogTitleBySlug,
  recordTitleView,
  reportTitleIssue,
} from "~/services/catalog.service";
import {
  fetchUserLibrary,
  recordWatchHistory,
  removeTitleFromLibrary,
  saveTitleToLibrary,
} from "~/services/library.service";

type Title = TitleDetail & { viewCount?: number };
type LibraryHistory = {
  title: { slug: string };
  episodeNumber: number | null;
  progressSeconds: number;
};
type LibraryResponse = { saved: { slug: string }[]; history: LibraryHistory[] };

const LOCAL_PROGRESS_KEY = "zmovie.watch-progress.v1";
const LOCAL_PROGRESS_TTL_MS = 90 * 24 * 60 * 60 * 1000;
const LOCAL_PROGRESS_LIMIT = 100;

const route = useRoute();
const { locale } = useLocale();
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
const authState = ref<"unknown" | "authenticated" | "anonymous">("unknown");
const selectedSubtitle = ref(-1);
const subtitleOptions = ref<{ index: number; label: string }[]>([]);
const isReportModalOpen = ref(false);
const reportReason = ref("video");
const reportDescription = ref("");
const isSubmittingReport = ref(false);

const isAutoNext = ref(true);
const isSkipIntro = ref(false);
const isTheaterMode = ref(false);
const watchAudio = ref<"sub" | "dual" | "dub">("sub");
const isCompactEpisodes = ref(false);

const isDanmakuEnabled = ref(true);
const danmakuInput = ref("");
const danmakus = ref([
  {
    timeSeconds: 2,
    content: "Chào mừng mọi người đến với ZMovie! 🔥",
    color: "#f59e0b",
  },
  {
    timeSeconds: 6,
    content: "Âm thanh sống động nghe sướng tai thật sự 🎧",
    color: "#38bdf8",
  },
  {
    timeSeconds: 12,
    content: "Hình ảnh cực nét không một chút giật lag! ❤️",
    color: "#f43f5e",
  },
  {
    timeSeconds: 18,
    content: "Trải nghiệm xem phim quá mượt mà 👍",
    color: "#4ade80",
  },
]);

function submitDanmaku() {
  if (!danmakuInput.value.trim()) return;
  danmakus.value.push({
    timeSeconds: currentTime.value,
    content: danmakuInput.value.trim(),
    color: "#ffffff",
  });
  danmakuInput.value = "";
}

const isEmbedMode = computed(() => {
  const url = episode.value?.hlsUrl || "";
  return (
    url.includes("embed") ||
    url.includes("streamc.xyz") ||
    (!url.includes(".m3u8") && !url.includes(".mp4"))
  );
});

const { hudNotice } = usePlayerHotkeys({
  togglePlay: () => togglePlayback(),
  toggleFullscreen: () => void toggleFullscreen(),
  toggleMute: () => toggleMute(),
  seekDelta: (delta) => {
    if (!video.value) return;
    video.value.currentTime = Math.max(
      0,
      Math.min(duration.value, video.value.currentTime + delta),
    );
  },
  adjustVolume: (delta) => {
    if (!video.value) return;
    const next = Math.max(0, Math.min(1, volume.value + delta));
    volume.value = next;
    isMuted.value = next === 0;
    video.value.volume = next;
  },
  toggleCaptions: () => toggleSubtitles(),
  enabled: computed(() => !isEmbedMode.value),
});

let hls: any = null;
let lastProgressSaved = 0;
let isSavingProgress = false;
let resumeSeconds = 0;
let libraryRequest: Promise<LibraryResponse> | null = null;

const { data: title, error: titleError } = await useAsyncData(
  `watch-title-${route.params.slug}`,
  () =>
    fetchCatalogTitleBySlug(
      String(route.params.slug),
      locale.value,
    ) as Promise<Title>,
);
const { data: playback, error: playbackError } = await useAsyncData(
  `watch-playback-${route.params.slug}`,
  () => fetchCatalogPlayback(String(route.params.slug), locale.value),
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

useZMovieSeo({
  title: computed(() =>
    title.value?.title
      ? `Đang xem ${title.value.title}`
      : "Xem phim trực tuyến",
  ),
  description: computed(
    () =>
      title.value?.synopsis ??
      "Xem phim trực tuyến với phụ đề tiếng Việt trên ZMovie.",
  ),
  image: computed(() => title.value?.posterUrl),
  type: "video.movie",
});

function showUnavailableDialog() {
  playerError.value = copy.value.unavailable;
  isLoading.value = false;
  isUnavailableDialogOpen.value = true;
}

async function loadEpisode() {
  const source = episode.value?.hlsUrl;
  if (!source) {
    showUnavailableDialog();
    return;
  }

  if (isEmbedMode.value) {
    isLoading.value = false;
    playerError.value = "";
    hls?.destroy();
    hls = null;
    return;
  }

  const element = video.value;
  if (!element) return;

  playerError.value = "";
  isLoading.value = true;
  hls?.destroy();
  hls = null;
  qualityOptions.value = [];
  selectedQuality.value = -1;
  selectedSubtitle.value = -1;
  subtitleOptions.value = [];
  element.pause();
  element.playbackRate = playbackRate.value;
  element.removeAttribute("src");
  element.load();

  if (episode.value?.subtitleUrl) {
    subtitleOptions.value = [{ index: 999, label: "Tiếng Việt (R2 WebVTT)" }];
  }

  const isDirectMp4 = source.endsWith(".mp4") || source.includes(".mp4");
  if (isDirectMp4) {
    element.src = source;
    element.play().catch(() => {});
    return;
  }

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
    instance.on(Hls.Events.SUBTITLE_TRACKS_UPDATED, () => {
      const tracks = instance.subtitleTracks.map(
        (track: any, index: number) => ({
          index,
          label: track.name || track.lang || `Phụ đề ${index + 1}`,
        }),
      );
      if (
        episode.value?.subtitleUrl &&
        !tracks.some((t: any) => t.index === 999)
      ) {
        tracks.unshift({ index: 999, label: "Tiếng Việt (R2 WebVTT)" });
      }
      subtitleOptions.value = tracks;
    });
    instance.loadSource(source);
    instance.attachMedia(element);
    hls = instance;
  }
}

function selectSubtitle(index: number) {
  selectedSubtitle.value = index;
  const element = video.value;
  if (!element) return;

  const tracks = element.textTracks;
  for (let i = 0; i < tracks.length; i++) {
    tracks[i].mode = index === 999 ? "showing" : "disabled";
  }

  if (hls) {
    if (index === -1 || index === 999) {
      hls.subtitleTrack = -1;
    } else {
      hls.subtitleTrack = index;
      hls.subtitleDisplay = true;
    }
  }
}

function toggleSubtitles() {
  if (selectedSubtitle.value === -1) {
    if (subtitleOptions.value.length > 0) {
      selectSubtitle(subtitleOptions.value[0].index);
    } else if (episode.value?.subtitleUrl) {
      selectSubtitle(999);
    }
  } else {
    selectSubtitle(-1);
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

function localProgressId(slug: string, episodeNumber: number | null) {
  return `${slug}:${episodeNumber ?? "movie"}`;
}

function readLocalProgress(): Record<string, LocalWatchProgress> {
  if (!import.meta.client) return {};
  try {
    const parsed = JSON.parse(
      localStorage.getItem(LOCAL_PROGRESS_KEY) ?? "{}",
    ) as Record<string, LocalWatchProgress>;
    const cutoff = Date.now() - LOCAL_PROGRESS_TTL_MS;
    return Object.fromEntries(
      Object.entries(parsed).filter(
        ([, item]) =>
          item &&
          Number.isFinite(item.progressSeconds) &&
          item.progressSeconds >= 0 &&
          Number.isFinite(item.updatedAt) &&
          item.updatedAt >= cutoff,
      ),
    );
  } catch {
    return {};
  }
}

function writeLocalProgress(progressSeconds: number) {
  if (!import.meta.client || !title.value) return;
  const episodeNumber = playback.value?.isSeries
    ? (episode.value?.number ?? null)
    : null;
  const progress = readLocalProgress();
  progress[localProgressId(title.value.slug, episodeNumber)] = {
    episodeNumber,
    progressSeconds,
    updatedAt: Date.now(),
  };
  const entries = Object.entries(progress)
    .sort(([, first], [, second]) => second.updatedAt - first.updatedAt)
    .slice(0, LOCAL_PROGRESS_LIMIT);
  try {
    localStorage.setItem(
      LOCAL_PROGRESS_KEY,
      JSON.stringify(Object.fromEntries(entries)),
    );
  } catch {
    // Browsers may disable or fill localStorage; playback must continue normally.
  }
}

function loadLocalResumePosition() {
  if (!title.value) return;
  const episodeNumber = playback.value?.isSeries
    ? (episode.value?.number ?? null)
    : null;
  const saved =
    readLocalProgress()[localProgressId(title.value.slug, episodeNumber)];
  resumeSeconds = saved?.progressSeconds ?? 0;
  applyResumePosition();
}

async function loadResumePosition() {
  if (!title.value) return;
  if (authState.value === "anonymous") {
    loadLocalResumePosition();
    return;
  }
  if (libraryRequest) return libraryRequest;

  const request = fetchUserLibrary(locale.value) as Promise<LibraryResponse>;
  libraryRequest = request;

  try {
    const library = await request;
    authState.value = "authenticated";
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
    authState.value = "anonymous";
    loadLocalResumePosition();
    // An anonymous viewer must not keep retrying an authenticated endpoint.
  } finally {
    libraryRequest = null;
  }
}

function selectEpisode(index: number) {
  selectedEpisode.value = index;
}

async function toggleMyList() {
  if (!title.value) return;
  try {
    if (isInMyList.value) {
      await removeTitleFromLibrary(title.value.slug);
    } else {
      await saveTitleToLibrary(title.value.slug);
    }
    isInMyList.value = !isInMyList.value;
  } catch {
    actionNotice.value = "Hãy đăng nhập để lưu phim vào danh sách.";
  }
}

async function recordWatchProgress(keepalive = false) {
  if (
    isSavingProgress ||
    !title.value ||
    currentTime.value < 5 ||
    !Number.isFinite(currentTime.value) ||
    Math.abs(currentTime.value - lastProgressSaved) < 1
  )
    return;
  if (authState.value === "anonymous") {
    writeLocalProgress(currentTime.value);
    lastProgressSaved = currentTime.value;
    return;
  }
  if (authState.value !== "authenticated") return;
  isSavingProgress = true;
  const progressSeconds = currentTime.value;
  try {
    await recordWatchHistory(
      title.value.slug,
      {
        episodeNumber: playback.value?.isSeries ? episode.value?.number : null,
        progressSeconds,
      },
      keepalive,
    );
    lastProgressSaved = progressSeconds;
  } catch {
    // Progress saving is best effort and must not interrupt playback.
  } finally {
    isSavingProgress = false;
  }
}

function onTimeUpdate() {
  currentTime.value = video.value?.currentTime || 0;
}

function onVideoPause() {
  isPlaying.value = false;
  void recordWatchProgress();
}

function onPageExit() {
  void recordWatchProgress(true);
}

async function recordView() {
  if (hasRecordedView.value || !title.value) return;
  hasRecordedView.value = true;
  try {
    const result = await recordTitleView(
      title.value.slug,
      playback.value?.isSeries ? episode.value?.number : null,
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
    background: `linear-gradient(to right, var(--primary) 0%, var(--primary) ${percent}%, rgba(255,255,255,.28) ${percent}%, rgba(255,255,255,.28) 100%)`,
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

async function submitReport() {
  if (!title.value) return;
  isSubmittingReport.value = true;
  try {
    await reportTitleIssue(title.value.slug, {
      category: reportReason.value,
      description: reportDescription.value.trim(),
      timestampSeconds: Math.round(currentTime.value),
    });
    actionNotice.value = "Cảm ơn bạn! Báo cáo sự cố đã được ghi nhận.";
    isReportModalOpen.value = false;
    reportDescription.value = "";
  } catch {
    actionNotice.value = "Không thể gửi báo cáo lúc này. Vui lòng thử lại sau.";
  } finally {
    isSubmittingReport.value = false;
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
  window.addEventListener("pagehide", onPageExit);
  document.addEventListener("visibilitychange", onPageExit);
  if (video.value) video.value.volume = volume.value;
  viewCount.value = title.value?.viewCount ?? 0;
  void loadResumePosition();
  void loadEpisode();
});
onBeforeUnmount(() => {
  window.removeEventListener("pagehide", onPageExit);
  document.removeEventListener("visibilitychange", onPageExit);
  void recordWatchProgress(true);
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
      <section class="mx-auto max-w-360 px-4 sm:px-6 lg:px-10 py-5">
        <div class="mb-4 flex items-center gap-3">
          <NuxtLink
            :to="`/movies/${title.slug}`"
            class="inline-flex size-9 items-center justify-center rounded-full bg-white/5 border border-white/10 text-white hover:bg-white/10 hover:text-primary transition"
          >
            <ChevronLeft class="size-5" />
          </NuxtLink>
          <h1
            class="text-sm sm:text-base font-bold text-white font-display truncate"
          >
            Xem phim {{ title.title }} -
            {{ playback.isSeries ? `Tập ${episode?.number || 1}` : "Bản Đẹp" }}
          </h1>
        </div>
        <div
          class="mt-5 overflow-hidden rounded-3xl border border-white/10 bg-black shadow-2xl"
        >
          <!-- Embed Stream Mode (Iframe) -->
          <div
            v-if="isEmbedMode"
            class="relative aspect-video w-full overflow-hidden bg-black"
          >
            <iframe
              :src="episode?.hlsUrl"
              class="size-full border-0"
              allow="
                accelerometer;
                autoplay;
                clipboard-write;
                encrypted-media;
                gyroscope;
                picture-in-picture;
                fullscreen;
              "
              allowfullscreen
            />
          </div>

          <!-- Native HLS Video Mode -->
          <div v-else ref="playerFrame" class="relative aspect-video group">
            <!-- CôBéPhim Meme Error Fallback State -->
            <div
              v-if="playerError"
              class="absolute inset-0 z-30 flex flex-col sm:flex-row items-center justify-center gap-6 bg-[#0f111a]/95 p-6 backdrop-blur-md text-center sm:text-left"
            >
              <img
                src="/default-meme-avatar.png"
                alt="Meme"
                class="size-28 sm:size-36 rounded-2xl object-cover shadow-2xl border-2 border-white/10"
              />
              <div>
                <h3
                  class="text-2xl sm:text-3xl font-black text-white font-display"
                >
                  CÓ BIẾN RỒI
                </h3>
                <p class="text-sm sm:text-base text-gray-300 mt-1">
                  Hãy thử refresh lại!
                </p>
                <button
                  type="button"
                  class="mt-4 inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-xs font-bold text-black hover:bg-[#ffde8a] transition shadow-lg active:scale-95"
                  @click="loadEpisode"
                >
                  Thử lại ngay
                </button>
              </div>
            </div>

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
            >
              <track
                v-if="episode?.subtitleUrl"
                kind="subtitles"
                :src="episode.subtitleUrl"
                srclang="vi"
                label="Tiếng Việt (WebVTT)"
                :default="selectedSubtitle === 999"
              />
            </video>

            <!-- Danmaku Canvas Layer -->
            <DanmakuCanvas
              :current-time="currentTime"
              :is-playing="isPlaying"
              :danmakus="danmakus"
              :enabled="isDanmakuEnabled"
            />

            <!-- HUD Visual Feedback overlay -->
            <div
              v-if="hudNotice"
              class="pointer-events-none absolute left-1/2 top-1/2 z-30 flex -translate-x-1/2 -translate-y-1/2 items-center gap-2 rounded-2xl bg-black/85 px-6 py-3.5 text-base font-bold text-white shadow-2xl backdrop-blur-md"
            >
              <span>{{ hudNotice.text }}</span>
            </div>

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
              class="absolute left-1/2 top-1/2 grid size-18 -translate-x-1/2 -translate-y-1/2 cursor-pointer place-items-center rounded-full bg-primary-container text-primary-container-foreground shadow-[0_0_50px_rgba(248,147,0,.5)] transition hover:scale-105"
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
                    v-if="subtitleOptions.length > 0"
                    class="player-control hidden size-9 place-items-center sm:grid transition"
                    :class="
                      selectedSubtitle !== -1 ? 'text-primary' : 'text-white/80'
                    "
                    aria-label="Subtitles"
                    @click="toggleSubtitles"
                  >
                    <Subtitles class="size-5" />
                  </button>
                  <button
                    class="player-control hidden size-9 place-items-center sm:grid"
                    aria-label="Playback settings"
                    @click="isSettingsOpen = !isSettingsOpen"
                  >
                    <Settings class="size-5 text-white/80" />
                  </button>
                  <div
                    v-if="isSettingsOpen"
                    class="absolute bottom-12 right-0 w-48 overflow-hidden rounded-xl border border-white/10 bg-[#202020] p-1 shadow-2xl"
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

                    <template v-if="subtitleOptions.length > 0">
                      <div class="mx-2 my-1 border-t border-white/10" />
                      <p class="px-3 py-2 text-xs font-semibold text-white/55">
                        Phụ đề
                      </p>
                      <button
                        class="player-control flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm hover:bg-white/10"
                        :class="selectedSubtitle === -1 ? 'text-primary' : ''"
                        @click="selectSubtitle(-1)"
                      >
                        <span>Tắt phụ đề</span>
                        <span v-if="selectedSubtitle === -1">✓</span>
                      </button>
                      <button
                        v-for="sub in subtitleOptions"
                        :key="sub.index"
                        class="player-control flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm hover:bg-white/10"
                        :class="
                          selectedSubtitle === sub.index ? 'text-primary' : ''
                        "
                        @click="selectSubtitle(sub.index)"
                      >
                        <span class="truncate">{{ sub.label }}</span>
                        <span v-if="selectedSubtitle === sub.index">✓</span>
                      </button>
                    </template>
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

          <!-- UNDER-PLAYER TOOLBAR (CôBéPhim Style) -->
          <div
            class="flex flex-wrap items-center justify-between gap-y-3 bg-[#141622] px-4 py-3.5 border-t border-white/5 text-xs font-semibold text-white/80"
          >
            <!-- Left actions -->
            <div class="flex items-center gap-4">
              <button
                type="button"
                class="inline-flex items-center gap-1.5 hover:text-primary transition"
                @click="toggleMyList"
              >
                <Heart
                  class="size-4"
                  :class="{ 'fill-red-500 text-red-500': isInMyList }"
                />
                <span>{{ isInMyList ? "Đã thích" : "Yêu thích" }}</span>
              </button>
              <button
                type="button"
                class="inline-flex items-center gap-1.5 hover:text-primary transition"
                @click="toggleMyList"
              >
                <BookmarkPlus
                  class="size-4"
                  :class="{ 'fill-primary text-primary': isInMyList }"
                />
                <span>Thêm vào</span>
              </button>
            </div>

            <!-- Center controls (Chuyển tập, Bỏ qua giới thiệu, Rạp phim) -->
            <div class="flex flex-wrap items-center gap-4 text-xs">
              <div
                class="flex items-center gap-1.5 cursor-pointer select-none"
                @click="isAutoNext = !isAutoNext"
              >
                <span>Chuyển tập</span>
                <span
                  class="rounded px-1.5 py-0.5 text-[10px] font-bold"
                  :class="
                    isAutoNext
                      ? 'bg-primary text-black'
                      : 'bg-white/10 text-white/60'
                  "
                >
                  {{ isAutoNext ? "ON" : "OFF" }}
                </span>
              </div>

              <div
                class="flex items-center gap-1.5 cursor-pointer select-none"
                @click="isSkipIntro = !isSkipIntro"
              >
                <span>Bỏ qua giới thiệu</span>
                <span
                  class="rounded px-1.5 py-0.5 text-[10px] font-bold"
                  :class="
                    isSkipIntro
                      ? 'bg-primary text-black'
                      : 'bg-white/10 text-white/60'
                  "
                >
                  {{ isSkipIntro ? "ON" : "OFF" }}
                </span>
              </div>

              <div
                class="flex items-center gap-1.5 cursor-pointer select-none"
                @click="isTheaterMode = !isTheaterMode"
              >
                <span>Rạp phim</span>
                <span
                  class="rounded px-1.5 py-0.5 text-[10px] font-bold"
                  :class="
                    isTheaterMode
                      ? 'bg-primary text-black'
                      : 'bg-white/10 text-white/60'
                  "
                >
                  {{ isTheaterMode ? "ON" : "OFF" }}
                </span>
              </div>
            </div>

            <!-- Right actions -->
            <div class="flex items-center gap-4">
              <button
                type="button"
                class="inline-flex items-center gap-1.5 hover:text-primary transition"
                @click="shareTitle"
              >
                <Share2 class="size-4" />
                <span>Chia sẻ</span>
              </button>
              <NuxtLink
                :to="`/party/${title.slug}-party?movie=${title.slug}&episode=${episode?.number || 1}`"
                class="inline-flex items-center gap-1.5 text-rose-400 hover:text-rose-300 transition"
              >
                <Users class="size-4" />
                <span>Xem chung</span>
              </NuxtLink>
              <button
                type="button"
                class="inline-flex items-center gap-1.5 hover:text-amber-400 transition"
                @click="isReportModalOpen = true"
              >
                <Flag class="size-4" />
                <span>Báo lỗi</span>
              </button>
            </div>
          </div>

          <!-- Chapter Thumbnails Strip -->
          <div
            class="grid grid-cols-1 sm:grid-cols-3 gap-2 bg-[#0d0f16] px-4 py-2.5 border-t border-white/5"
          >
            <div
              class="flex items-center gap-2.5 overflow-hidden rounded-xl bg-white/5 p-2 hover:bg-white/10 cursor-pointer transition"
            >
              <img
                src="https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=100&auto=format&fit=crop&q=80"
                class="size-9 rounded-lg object-cover"
              />
              <span class="text-xs font-semibold text-white/80 truncate"
                >Secrets Await</span
              >
            </div>
            <div
              class="flex items-center gap-2.5 overflow-hidden rounded-xl bg-white/5 p-2 hover:bg-white/10 cursor-pointer transition"
            >
              <img
                src="https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=100&auto=format&fit=crop&q=80"
                class="size-9 rounded-lg object-cover"
              />
              <span class="text-xs font-semibold text-white/80 truncate"
                >Find Your Path</span
              >
            </div>
            <div
              class="flex items-center gap-2.5 overflow-hidden rounded-xl bg-white/5 p-2 hover:bg-white/10 cursor-pointer transition"
            >
              <img
                src="https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=100&auto=format&fit=crop&q=80"
                class="size-9 rounded-lg object-cover"
              />
              <span class="text-xs font-semibold text-white/80 truncate"
                >Discover Hidden Gems</span
              >
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

          <!-- Danmaku Bar -->
          <div
            v-if="!isEmbedMode"
            class="mt-4 flex items-center gap-3 rounded-2xl border border-white/8 bg-surface-container px-4 py-2.5"
          >
            <button
              class="px-2.5 py-1 rounded-lg text-xs font-semibold border transition"
              :class="
                isDanmakuEnabled
                  ? 'bg-primary/15 text-primary border-primary/40'
                  : 'bg-surface-container-lowest text-muted-foreground border-white/10'
              "
              @click="isDanmakuEnabled = !isDanmakuEnabled"
            >
              Đạn mạc (Danmaku)
            </button>
            <form
              class="flex-1 flex items-center gap-2"
              @submit.prevent="submitDanmaku"
            >
              <input
                v-model="danmakuInput"
                type="text"
                placeholder="Bắn bình luận bay ngang màn hình..."
                class="flex-1 rounded-xl border border-white/10 bg-surface-container-lowest px-3 py-1.5 text-xs text-foreground placeholder:text-muted-foreground/60 outline-none focus:border-primary/60 transition"
              />
              <button
                type="submit"
                :disabled="!danmakuInput.trim()"
                class="rounded-xl bg-primary px-3 py-1.5 text-xs font-semibold text-primary-container-foreground transition hover:opacity-90 disabled:opacity-40"
              >
                Bắn
              </button>
            </form>
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
              <NuxtLink
                :to="`/party/${title.slug}-party?movie=${title.slug}&episode=${episode?.number || 1}`"
                class="inline-flex h-10 items-center gap-2 rounded-full border border-rose-500/40 bg-rose-500/15 px-4 text-xs font-semibold text-rose-400 transition hover:bg-rose-500/25"
                title="Tạo phòng xem chung"
              >
                <Users class="size-4" />
                <span>Xem chung</span>
              </NuxtLink>
              <button
                class="inline-flex h-10 items-center gap-2 rounded-full border border-primary/45 bg-primary/10 px-5 text-xs font-semibold text-primary transition hover:bg-primary-container hover:text-primary-container-foreground"
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
              <button
                class="inline-flex size-10 items-center justify-center rounded-full bg-surface-container-high text-muted-foreground transition hover:bg-surface-container-highest hover:text-amber-400"
                aria-label="Báo lỗi"
                title="Báo lỗi phim"
                @click="isReportModalOpen = true"
              >
                <Flag class="size-4" />
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

        <!-- Episode Grid Section (CôBéPhim Style) -->
        <section v-if="playback.episodes.length" class="mt-8">
          <div
            class="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 pb-4"
          >
            <div class="flex items-center gap-3">
              <!-- Season Dropdown -->
              <div
                class="rounded-xl border border-white/10 bg-[#191b24] px-3.5 py-2 text-xs font-bold text-white"
              >
                Phần 1 ▾
              </div>

              <!-- Audio track pills -->
              <div class="flex items-center gap-1.5">
                <button
                  type="button"
                  class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    watchAudio === 'sub'
                      ? 'border-primary bg-primary/10 text-primary font-bold'
                      : 'border-white/10 bg-white/5 text-white/80'
                  "
                  @click="watchAudio = 'sub'"
                >
                  Phụ đề #1
                </button>
                <button
                  type="button"
                  class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    watchAudio === 'dual'
                      ? 'border-primary bg-primary/10 text-primary font-bold'
                      : 'border-white/10 bg-white/5 text-white/80'
                  "
                  @click="watchAudio = 'dual'"
                >
                  Song ngữ
                </button>
                <button
                  type="button"
                  class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
                  :class="
                    watchAudio === 'dub'
                      ? 'border-primary bg-primary/10 text-primary font-bold'
                      : 'border-white/10 bg-white/5 text-white/80'
                  "
                  @click="watchAudio = 'dub'"
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
                @click="isCompactEpisodes = !isCompactEpisodes"
              >
                <div
                  class="size-4 rounded-full bg-black transition-transform"
                  :class="{ 'translate-x-4': isCompactEpisodes }"
                />
              </div>
            </div>
          </div>

          <!-- Episode Buttons Grid -->
          <div
            class="mt-4 grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-2.5"
          >
            <button
              v-for="(item, index) in playback.episodes"
              :key="item.number"
              type="button"
              class="flex items-center justify-center gap-2 rounded-2xl border py-3 text-xs font-bold transition active:scale-95"
              :class="
                index === selectedEpisode
                  ? 'border-primary bg-primary text-black shadow-md'
                  : 'border-white/10 bg-[#191b24] text-white hover:border-primary/50 hover:text-primary'
              "
              @click="selectEpisode(index)"
            >
              <Play class="size-3.5 fill-current" />
              <span>Tập {{ item.number }}</span>
            </button>
          </div>
        </section>

        <!-- Discord Community Banner (CôBéPhim Style) -->
        <div
          class="mt-8 flex flex-col sm:flex-row items-center justify-between gap-4 rounded-2xl bg-gradient-to-r from-blue-700 via-indigo-600 to-purple-700 p-4 sm:p-5 shadow-xl text-white"
        >
          <div class="flex items-center gap-3.5 text-center sm:text-left">
            <div
              class="grid size-12 shrink-0 place-items-center rounded-2xl bg-white/20 shadow-inner"
            >
              <svg class="size-6 fill-current" viewBox="0 0 24 24">
                <path
                  d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994.021-.041.001-.09-.041-.106a13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.929 1.793 8.18 1.793 12.061 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.894.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.028zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z"
                />
              </svg>
            </div>
            <div>
              <h4 class="text-sm font-black tracking-tight font-display">
                ĐẢO RÒ XANH - NHÓM DISCORD
              </h4>
              <p class="text-xs text-white/80 mt-0.5">
                Tham gia cộng đồng bàn luận phim sôi nổi nhất
              </p>
            </div>
          </div>
          <a
            href="#"
            class="rounded-xl bg-white px-5 py-2.5 text-xs font-bold text-indigo-900 shadow hover:bg-gray-100 transition whitespace-nowrap active:scale-95"
          >
            Tham gia ngay
          </a>
        </div>
      </section>

      <!-- AppFooter Component -->
      <AppFooter />
    </template>

    <AlertDialogRoot v-model:open="isReportModalOpen">
      <AlertDialogPortal>
        <AlertDialogOverlay
          class="fixed inset-0 z-[100] bg-black/70 backdrop-blur-sm"
        />
        <AlertDialogContent
          class="fixed left-1/2 top-1/2 z-[101] w-[calc(100%-2.5rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-3xl border border-white/10 bg-surface-container p-6 shadow-2xl outline-none"
        >
          <div class="flex items-center gap-3">
            <div
              class="flex size-10 items-center justify-center rounded-xl bg-amber-500/15 text-amber-400"
            >
              <Flag class="size-5" />
            </div>
            <div>
              <AlertDialogTitle class="font-display text-xl font-semibold"
                >Báo lỗi phim</AlertDialogTitle
              >
              <p class="text-xs text-muted-foreground">{{ title?.title }}</p>
            </div>
          </div>

          <div class="mt-5 space-y-4 text-sm">
            <div>
              <label
                class="mb-1.5 block text-xs font-semibold text-muted-foreground"
                >Loại sự cố</label
              >
              <select
                v-model="reportReason"
                class="w-full rounded-xl border border-white/10 bg-surface-container-high px-3 py-2 text-sm text-foreground focus:border-primary focus:outline-none"
              >
                <option value="video">Video không phát được / giật lag</option>
                <option value="audio">Âm thanh bị mất hoặc lệch tiếng</option>
                <option value="subtitle">Phụ đề sai hoặc không hiển thị</option>
                <option value="content">
                  Nội dung tập bị sai hoặc nhầm phim
                </option>
              </select>
            </div>

            <div>
              <label
                class="mb-1.5 block text-xs font-semibold text-muted-foreground"
                >Chi tiết sự cố</label
              >
              <textarea
                v-model="reportDescription"
                rows="3"
                placeholder="Mô tả cụ thể sự cố để đội ngũ kỹ thuật xử lý nhanh chóng..."
                class="w-full rounded-xl border border-white/10 bg-surface-container-high px-3 py-2 text-sm text-foreground placeholder:text-white/30 focus:border-primary focus:outline-none"
              />
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button
              class="rounded-xl border border-white/15 px-4 py-2 text-xs font-semibold text-foreground hover:bg-white/5"
              @click="isReportModalOpen = false"
            >
              Hủy
            </button>
            <button
              class="rounded-xl bg-primary px-5 py-2 text-xs font-semibold text-primary-container-foreground transition hover:opacity-90 disabled:opacity-50"
              :disabled="isSubmittingReport"
              @click="submitReport"
            >
              {{ isSubmittingReport ? "Đang gửi..." : "Gửi báo lỗi" }}
            </button>
          </div>
        </AlertDialogContent>
      </AlertDialogPortal>
    </AlertDialogRoot>
  </main>
</template>

<style scoped>
.player-range {
  appearance: none;
  height: 5px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.28);
  accent-color: var(--primary);
  cursor: pointer;
}
.player-range::-webkit-slider-thumb {
  appearance: none;
  width: 13px;
  height: 13px;
  border-radius: 999px;
  background: var(--primary);
  box-shadow: 0 0 0 3px rgba(248, 147, 0, 0.2);
}
.player-range::-moz-range-thumb {
  width: 13px;
  height: 13px;
  border: 0;
  border-radius: 999px;
  background: var(--primary);
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
