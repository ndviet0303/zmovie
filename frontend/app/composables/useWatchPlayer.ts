import { computed, onBeforeUnmount, ref, watch } from "vue";
import type {
  PlaybackEpisode,
  PlaybackResponse,
  PlaybackSource,
  TitleDetail,
} from "~/types/catalog";
import type { QualityOption, SubtitleOption } from "~/types/watch";
import {
  fetchCatalogPlayback,
  fetchCatalogTitleBySlug,
  recordTitleView,
} from "~/services/catalog.service";
import {
  fetchUserLibrary,
  recordWatchHistory,
} from "~/services/library.service";
import { getPlaybackSourceKind, inferPlaybackFormat } from "~/utils/playback";

const LOCAL_PROGRESS_KEY = "zmovie.watch-progress.v1";
const LOCAL_PROGRESS_TTL_MS = 90 * 24 * 60 * 60 * 1000;
const LOCAL_PROGRESS_LIMIT = 100;

export function useWatchPlayer(slug: string) {
  const video = ref<HTMLVideoElement | null>(null);
  const playerFrame = ref<HTMLElement | null>(null);

  const title = ref<TitleDetail | null>(null);
  const playback = ref<PlaybackResponse | null>(null);
  const selectedEpisodeIndex = ref(0);

  const activeSourceIndex = ref(0);
  const playerError = ref("");
  const isLoading = ref(true);
  const isPlaying = ref(false);
  const currentTime = ref(0);
  const duration = ref(0);
  const volume = ref(0.85);
  const isMuted = ref(false);
  const playbackRate = ref(1);

  const qualityOptions = ref<QualityOption[]>([]);
  const selectedQuality = ref(-1);

  const subtitleOptions = ref<SubtitleOption[]>([]);
  const selectedSubtitle = ref(-1);

  const isTheaterMode = ref(false);
  const isAutoNext = ref(true);
  const isSkipIntro = ref(false);
  const isMiniPlayerActive = ref(false);
  const isScrolledPast = ref(false);

  const showSkipIntroPrompt = ref(false);
  const viewCount = ref(0);
  const hasRecordedView = ref(false);
  const authState = ref<"unknown" | "authenticated" | "anonymous">("unknown");

  let hlsInstance: unknown = null;
  let scrollObserver: IntersectionObserver | null = null;

  const currentEpisode = computed<PlaybackEpisode | null>(() => {
    return playback.value?.episodes[selectedEpisodeIndex.value] ?? null;
  });

  const availableSources = computed<PlaybackSource[]>(() => {
    const ep = currentEpisode.value;
    if (!ep) return [];
    if (ep.sources && ep.sources.length > 0) {
      return [...ep.sources].sort((a, b) => a.priority - b.priority);
    }
    const defaultFormat = inferPlaybackFormat(ep.hlsUrl);
    return [
      {
        provider: "Default",
        url: ep.hlsUrl,
        format: defaultFormat,
        priority: 1,
        subtitleUrl: ep.subtitleUrl,
      },
    ];
  });

  const currentSource = computed<PlaybackSource | null>(() => {
    const sources = availableSources.value;
    if (sources.length === 0) return null;
    return sources[activeSourceIndex.value] ?? sources[0];
  });

  const isEmbed = computed(() => {
    return getPlaybackSourceKind(currentSource.value) === "embed";
  });

  const milestones = computed(() => {
    return currentEpisode.value?.milestones ?? null;
  });

  // Local watch progress helpers
  function getLocalProgress(
    titleSlug: string,
    episodeNumber: number | null,
  ): number {
    if (!import.meta.client) return 0;
    try {
      const raw = localStorage.getItem(LOCAL_PROGRESS_KEY);
      if (!raw) return 0;
      const parsed = JSON.parse(raw);
      const key = `${titleSlug}#${episodeNumber ?? 0}`;
      const entry = parsed[key];
      if (entry && Date.now() - entry.updatedAt < LOCAL_PROGRESS_TTL_MS) {
        return entry.progressSeconds || 0;
      }
    } catch {
      // ignore
    }
    return 0;
  }

  function saveLocalProgress(
    titleSlug: string,
    episodeNumber: number | null,
    progressSeconds: number,
  ) {
    if (!import.meta.client) return;
    try {
      const raw = localStorage.getItem(LOCAL_PROGRESS_KEY);
      const data = raw ? JSON.parse(raw) : {};
      const key = `${titleSlug}#${episodeNumber ?? 0}`;
      data[key] = { episodeNumber, progressSeconds, updatedAt: Date.now() };

      const entries = Object.entries(data);
      if (entries.length > LOCAL_PROGRESS_LIMIT) {
        entries.sort(
          (a, b) =>
            (b[1] as { updatedAt: number }).updatedAt -
            (a[1] as { updatedAt: number }).updatedAt,
        );
        const trimmed = Object.fromEntries(
          entries.slice(0, LOCAL_PROGRESS_LIMIT),
        );
        localStorage.setItem(LOCAL_PROGRESS_KEY, JSON.stringify(trimmed));
      } else {
        localStorage.setItem(LOCAL_PROGRESS_KEY, JSON.stringify(data));
      }
    } catch {
      // ignore
    }
  }

  async function loadData() {
    isLoading.value = true;
    playerError.value = "";
    try {
      const [titleData, playbackData] = await Promise.all([
        fetchCatalogTitleBySlug(slug),
        fetchCatalogPlayback(slug),
      ]);
      title.value = titleData;
      playback.value = playbackData;
      if (titleData?.viewCount) viewCount.value = titleData.viewCount;

      checkAuthAndResume();
    } catch (err: unknown) {
      playerError.value =
        err instanceof Error ? err.message : "Failed to load movie playback";
    } finally {
      isLoading.value = false;
    }
  }

  async function checkAuthAndResume() {
    try {
      const lib = await fetchUserLibrary();
      authState.value = "authenticated";
      if (lib && currentEpisode.value) {
        const historyItem = lib.history?.find((h) => h.title.slug === slug);
        if (historyItem) {
          const matchedIndex =
            playback.value?.episodes.findIndex(
              (e) => e.number === historyItem.episodeNumber,
            ) ?? -1;
          if (matchedIndex >= 0) selectedEpisodeIndex.value = matchedIndex;
        }
      }
    } catch {
      authState.value = "anonymous";
    }
  }

  async function initPlayer() {
    const src = currentSource.value;
    const sourceKind = getPlaybackSourceKind(src);
    if (!src || sourceKind === "embed") return;

    if (!import.meta.client) return;

    destroyHls();

    const videoEl = video.value;
    if (!videoEl) return;

    playerError.value = "";
    qualityOptions.value = [];
    subtitleOptions.value = [];
    selectedQuality.value = -1;
    selectedSubtitle.value = -1;
    currentTime.value = 0;
    duration.value = 0;
    videoEl.pause();
    videoEl.removeAttribute("src");
    videoEl.load();
    videoEl.volume = volume.value;
    videoEl.muted = isMuted.value;
    videoEl.playbackRate = playbackRate.value;

    if (sourceKind === "video") {
      videoEl.src = src.url;
      videoEl.load();
      return;
    }

    try {
      const { default: Hls } = await import("hls.js");
      if (Hls.isSupported()) {
        const hls = new Hls({
          enableWorker: true,
          lowLatencyMode: true,
          backBufferLength: 90,
        });

        hls.attachMedia(videoEl);
        hls.on(Hls.Events.MEDIA_ATTACHED, () => {
          hls.loadSource(src.url);
        });

        hls.on(Hls.Events.MANIFEST_PARSED, (_e, data) => {
          qualityOptions.value = data.levels.map((level, idx) => ({
            level: idx,
            label: level.height ? `${level.height}p` : `Level ${idx + 1}`,
          }));
          selectedQuality.value = hls.currentLevel;

          resumeSavedPosition(videoEl);
        });

        hls.on(Hls.Events.SUBTITLE_TRACKS_UPDATED, () => {
          subtitleOptions.value = (hls.subtitleTracks || []).map(
            (track, idx) => ({
              index: idx,
              label: track.name || track.lang || `Track ${idx + 1}`,
            }),
          );
        });

        hls.on(Hls.Events.ERROR, (_event, data) => {
          if (data.fatal) {
            handleStreamError(data.type, data.details);
          }
        });

        hlsInstance = hls;
      } else if (videoEl.canPlayType("application/vnd.apple.mpegurl")) {
        videoEl.src = src.url;
      }
    } catch (e: unknown) {
      playerError.value =
        e instanceof Error ? e.message : "Error initializing player";
    }
  }

  function handleStreamError(_errorType: string, _details: string) {
    // Multi-source automatic failover!
    if (activeSourceIndex.value < availableSources.value.length - 1) {
      activeSourceIndex.value++;
      initPlayer();
    } else {
      playerError.value =
        "Tất cả nguồn phát đều không khả dụng. Vui lòng báo lỗi để đội ngũ hỗ trợ sửa chữa.";
    }
  }

  function destroyHls() {
    if (
      hlsInstance &&
      typeof (hlsInstance as { destroy: () => void }).destroy === "function"
    ) {
      (hlsInstance as { destroy: () => void }).destroy();
      hlsInstance = null;
    }
  }

  function selectEpisode(idx: number) {
    if (idx === selectedEpisodeIndex.value) return;
    selectedEpisodeIndex.value = idx;
    activeSourceIndex.value = 0;
    currentTime.value = 0;
    initPlayer();
  }

  function selectSource(idx: number) {
    if (idx === activeSourceIndex.value) return;
    activeSourceIndex.value = idx;
    initPlayer();
  }

  function resumeSavedPosition(videoEl: HTMLVideoElement) {
    const resumePos = getLocalProgress(
      slug,
      currentEpisode.value?.number ?? null,
    );
    if (
      resumePos > 5 &&
      Number.isFinite(videoEl.duration) &&
      resumePos < videoEl.duration - 30
    ) {
      videoEl.currentTime = resumePos;
    }
  }

  function onLoadedMetadata() {
    const el = video.value;
    if (!el) return;
    duration.value = Number.isFinite(el.duration) ? el.duration : 0;
    currentTime.value = el.currentTime;
    resumeSavedPosition(el);
  }

  function togglePlay() {
    const el = video.value;
    if (!el) return;
    if (el.paused) {
      el.play().catch(() => undefined);
    } else {
      el.pause();
    }
  }

  function seek(targetSeconds: number) {
    const el = video.value;
    if (!el) return;
    el.currentTime = Math.max(0, Math.min(targetSeconds, duration.value || 0));
  }

  function setVolume(v: number) {
    volume.value = Math.max(0, Math.min(1, v));
    if (video.value) {
      video.value.volume = volume.value;
      if (volume.value > 0) isMuted.value = false;
    }
  }

  function toggleMute() {
    isMuted.value = !isMuted.value;
    if (video.value) video.value.muted = isMuted.value;
  }

  function toggleTheater() {
    isTheaterMode.value = !isTheaterMode.value;
  }

  function skipIntro() {
    const ms = milestones.value;
    if (ms?.introEnd) {
      seek(ms.introEnd);
      showSkipIntroPrompt.value = false;
    }
  }

  function onTimeUpdate() {
    const el = video.value;
    if (!el) return;
    currentTime.value = el.currentTime;
    duration.value = el.duration || 0;

    // Record view after 30 seconds
    if (!hasRecordedView.value && currentTime.value > 30) {
      hasRecordedView.value = true;
      recordTitleView(slug)
        .then((res) => {
          if (res?.counted) viewCount.value = res.viewCount;
        })
        .catch(() => undefined);
    }

    // Save progress periodically
    if (Math.floor(currentTime.value) % 5 === 0) {
      const epNum = currentEpisode.value?.number ?? null;
      saveLocalProgress(slug, epNum, Math.floor(currentTime.value));
      if (authState.value === "authenticated") {
        recordWatchHistory(slug, epNum, Math.floor(currentTime.value)).catch(
          () => undefined,
        );
      }
    }

    // Handle skip intro
    const ms = milestones.value;
    if (ms?.introStart && ms?.introEnd) {
      if (
        currentTime.value >= ms.introStart &&
        currentTime.value <= ms.introEnd
      ) {
        if (isSkipIntro.value) {
          seek(ms.introEnd);
        } else {
          showSkipIntroPrompt.value = true;
        }
      } else {
        showSkipIntroPrompt.value = false;
      }
    }
  }

  function onEnded() {
    isPlaying.value = false;
    if (
      isAutoNext.value &&
      selectedEpisodeIndex.value < (playback.value?.episodes.length ?? 0) - 1
    ) {
      selectEpisode(selectedEpisodeIndex.value + 1);
    }
  }

  function setupIntersectionObserver() {
    if (!import.meta.client || !playerFrame.value) return;
    scrollObserver = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        isScrolledPast.value =
          !entry.isIntersecting && entry.boundingClientRect.top < 0;
        if (isScrolledPast.value && isPlaying.value) {
          isMiniPlayerActive.value = true;
        }
      },
      { threshold: 0.1 },
    );
    scrollObserver.observe(playerFrame.value);
  }

  watch([currentSource, video], () => {
    if (isEmbed.value) {
      destroyHls();
      const el = video.value;
      if (el) {
        el.pause();
        el.removeAttribute("src");
        el.load();
      }
      currentTime.value = 0;
      duration.value = 0;
      return;
    }
    initPlayer();
  });

  onBeforeUnmount(() => {
    destroyHls();
    scrollObserver?.disconnect();
  });

  return {
    video,
    playerFrame,
    title,
    playback,
    currentEpisode,
    selectedEpisodeIndex,
    availableSources,
    currentSource,
    activeSourceIndex,
    isEmbed,
    milestones,
    isLoading,
    isPlaying,
    currentTime,
    duration,
    volume,
    isMuted,
    playbackRate,
    qualityOptions,
    selectedQuality,
    subtitleOptions,
    selectedSubtitle,
    playerError,
    isTheaterMode,
    isAutoNext,
    isSkipIntro,
    isMiniPlayerActive,
    isScrolledPast,
    showSkipIntroPrompt,
    viewCount,
    loadData,
    initPlayer,
    selectEpisode,
    selectSource,
    togglePlay,
    seek,
    setVolume,
    toggleMute,
    toggleTheater,
    skipIntro,
    onTimeUpdate,
    onLoadedMetadata,
    onEnded,
    setupIntersectionObserver,
  };
}
