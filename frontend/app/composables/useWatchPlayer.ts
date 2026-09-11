import { computed, onBeforeUnmount, ref, watch } from "vue";
import Hls from "hls.js";
import type {
  PlaybackEpisode,
  PlaybackResponse,
  PlaybackSource,
  TitleDetail,
} from "../types/catalog";
import type { HistoryItem, Library } from "../types/library";
import type { QualityOption, SubtitleOption } from "../types/watch";
import {
  fetchCatalogPlayback,
  fetchCatalogTitleBySlug,
  recordTitleView,
} from "../services/catalog.service";
import {
  fetchUserLibrary,
  recordWatchHistory,
} from "../services/library.service";
import { getPlaybackSourceKind, inferPlaybackFormat } from "../utils/playback";
import { playbackMetrics } from "../utils/playbackMetrics";

const LOCAL_PROGRESS_KEY = "zmovie.watch-progress.v1";
const LOCAL_PROGRESS_TTL_MS = 90 * 24 * 60 * 60 * 1000;
const LOCAL_PROGRESS_LIMIT = 100;
const HISTORY_TIMEOUT_MS = 500;
const MAX_HLS_RETRIES = 2;

function parseEpisodeNumber(
  num: number | string | null | undefined,
): number | null {
  if (num === null || num === undefined) return null;
  const parsed = typeof num === "number" ? num : parseInt(num, 10);
  return Number.isNaN(parsed) ? null : parsed;
}

interface LocalProgressEntry {
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: number;
}

function isClientEnvironment(): boolean {
  return typeof window !== "undefined" || Boolean(import.meta.client);
}

export interface WatchPlayerDependencies {
  fetchLibrary?: () => Promise<Library | null>;
  recordHistory?: (
    slug: string,
    data: { episodeNumber?: number | null; progressSeconds: number },
  ) => Promise<unknown>;
  recordView?: (
    slug: string,
  ) => Promise<{ counted: boolean; viewCount: number }>;
}

export function useWatchPlayer(slug: string, deps?: WatchPlayerDependencies) {
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

  let hlsInstance: Hls | null = null;
  let scrollObserver: IntersectionObserver | null = null;

  // Generation & playback intent tracking
  let currentGeneration = 0;
  let hasAppliedResumeForGeneration = false;
  let userHasExplicitlySelectedEpisode = false;
  let resumeDecisionFrozen = false;
  let pendingResumePosition = 0;
  let pendingSeekTarget: number | null = null;
  let playbackIntent: "play" | "pause" = "pause";
  let hlsRetryCount = 0;

  // Resolved service dependencies
  const doFetchPlayback = deps?.fetchPlayback ?? fetchCatalogPlayback;
  const doFetchTitle = deps?.fetchTitle ?? fetchCatalogTitleBySlug;
  const doFetchLibrary = deps?.fetchLibrary ?? fetchUserLibrary;
  const doRecordHistory = deps?.recordHistory ?? recordWatchHistory;
  const doRecordView = deps?.recordView ?? recordTitleView;

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
    return sources[activeSourceIndex.value] ?? sources[0] ?? null;
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
    if (!isClientEnvironment()) return 0;
    try {
      const raw = localStorage.getItem(LOCAL_PROGRESS_KEY);
      if (!raw) return 0;
      const parsed: Record<string, LocalProgressEntry> = JSON.parse(raw);
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
    if (!isClientEnvironment()) return;
    try {
      const raw = localStorage.getItem(LOCAL_PROGRESS_KEY);
      const data: Record<string, LocalProgressEntry> = raw
        ? JSON.parse(raw)
        : {};
      const key = `${titleSlug}#${episodeNumber ?? 0}`;
      data[key] = { episodeNumber, progressSeconds, updatedAt: Date.now() };

      const entries = Object.entries(data);
      if (entries.length > LOCAL_PROGRESS_LIMIT) {
        entries.sort((a, b) => b[1].updatedAt - a[1].updatedAt);
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
    playbackMetrics.startSession(slug);

    try {
      // Title decoration does not gate playable data
      doFetchTitle(slug)
        .then((titleData) => {
          title.value = titleData;
          if (titleData?.viewCount) viewCount.value = titleData.viewCount;
        })
        .catch(() => undefined);

      // Concurrent playback resolution and bounded remote history (500ms budget)
      const historyPromise = Promise.race([
        doFetchLibrary().catch(() => null),
        new Promise<null>((resolve) =>
          setTimeout(() => resolve(null), HISTORY_TIMEOUT_MS),
        ),
      ]);

      const [playbackData, userLib] = await Promise.all([
        doFetchPlayback(slug),
        historyPromise,
      ]);

      playback.value = playbackData;

      // Select initial episode if not explicitly selected by user
      if (!userHasExplicitlySelectedEpisode && userLib?.history) {
        const historyItem = userLib.history.find(
          (h: HistoryItem) => h.title.slug === slug,
        );
        if (historyItem) {
          const matchedIndex = playbackData.episodes.findIndex(
            (e: PlaybackEpisode) =>
              parseEpisodeNumber(e.number) ===
              parseEpisodeNumber(historyItem.episodeNumber),
          );
          if (matchedIndex >= 0) {
            selectedEpisodeIndex.value = matchedIndex;
          }
        }
      }

      // Determine initial resume position with local > remote precedence
      const targetEpisode = playbackData.episodes[selectedEpisodeIndex.value];
      const epNum = parseEpisodeNumber(targetEpisode?.number);
      const localPos = getLocalProgress(slug, epNum);
      let chosenPos = 0;

      if (localPos > 5) {
        chosenPos = localPos;
      } else if (userLib?.history) {
        const remoteHist = userLib.history.find(
          (h: HistoryItem) => h.title.slug === slug,
        );
        if (
          remoteHist &&
          parseEpisodeNumber(remoteHist.episodeNumber) === (epNum ?? 1) &&
          remoteHist.progressSeconds > 5
        ) {
          chosenPos = remoteHist.progressSeconds;
        }
      }

      pendingResumePosition = chosenPos;
      resumeDecisionFrozen = true;

      // Asynchronous background library check if bounded history timed out
      if (!userLib) {
        doFetchLibrary()
          .then((fullLib) => {
            authState.value = fullLib ? "authenticated" : "anonymous";
          })
          .catch(() => {
            authState.value = "anonymous";
          });
      } else {
        authState.value = "authenticated";
      }
    } catch (err: unknown) {
      playerError.value =
        err instanceof Error ? err.message : "Failed to load movie playback";
      playbackMetrics.recordFailure("startup", "", "", playerError.value);
    } finally {
      if (!currentSource.value || playerError.value) isLoading.value = false;
    }
  }

  function initPlayer() {
    const src = currentSource.value;
    const sourceKind = getPlaybackSourceKind(src);
    if (!src) {
      isLoading.value = false;
      return;
    }

    const generation = ++currentGeneration;
    hasAppliedResumeForGeneration = false;
    hlsRetryCount = 0;

    isLoading.value = true;
    playerError.value = "";

    if (sourceKind === "embed" || !isClientEnvironment()) {
      destroyHls();
      return;
    }

    destroyHls();

    const videoEl = video.value;
    if (!videoEl) return;

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

    const initialPosition = pendingSeekTarget ?? pendingResumePosition;

    if (sourceKind === "video") {
      videoEl.src = src.url;
      videoEl.load();
      return;
    }

    try {
      if (Hls.isSupported()) {
        const hls = new Hls({
          enableWorker: true,
          lowLatencyMode: true,
          backBufferLength: 90,
          startPosition: initialPosition > 5 ? initialPosition : -1,
        });

        hls.attachMedia(videoEl);
        hls.on(Hls.Events.MEDIA_ATTACHED, () => {
          if (generation !== currentGeneration) {
            hls.destroy();
            return;
          }
          hls.loadSource(src.url);
        });

        hls.on(Hls.Events.MANIFEST_PARSED, (_e, data) => {
          if (generation !== currentGeneration) return;
          qualityOptions.value = data.levels.map((level, idx) => ({
            level: idx,
            label: level.height ? `${level.height}p` : `Level ${idx + 1}`,
          }));
          selectedQuality.value = hls.currentLevel;

          applyResumeOrSeek(videoEl, initialPosition);
        });

        hls.on(Hls.Events.SUBTITLE_TRACKS_UPDATED, () => {
          if (generation !== currentGeneration) return;
          subtitleOptions.value = (hls.subtitleTracks || []).map(
            (track, idx) => ({
              index: idx,
              label: track.name || track.lang || `Track ${idx + 1}`,
            }),
          );
        });

        hls.on(Hls.Events.ERROR, (_event, data) => {
          if (generation !== currentGeneration) return;
          if (data.fatal) {
            if (
              data.type === Hls.ErrorTypes.NETWORK_ERROR &&
              hlsRetryCount < MAX_HLS_RETRIES
            ) {
              hlsRetryCount++;
              hls.startLoad();
              return;
            }
            if (data.type === Hls.ErrorTypes.MEDIA_ERROR) {
              hls.recoverMediaError();
              return;
            }
            failoverToNextSource(`HLS fatal error: ${data.details}`);
          }
        });

        hlsInstance = hls;
      } else if (videoEl.canPlayType("application/vnd.apple.mpegurl")) {
        videoEl.src = src.url;
      }
    } catch (e: unknown) {
      if (generation !== currentGeneration) return;
      isLoading.value = false;
      playerError.value =
        e instanceof Error ? e.message : "Error initializing player";
    }
  }

  function applyResumeOrSeek(videoEl: HTMLVideoElement, targetPos: number) {
    if (hasAppliedResumeForGeneration) return;
    hasAppliedResumeForGeneration = true;
    pendingResumePosition = 0;

    if (targetPos > 5) {
      const clampedPos =
        Number.isFinite(videoEl.duration) && videoEl.duration > 0
          ? Math.min(targetPos, Math.max(0, videoEl.duration - 1))
          : targetPos;

      if (Math.abs(videoEl.currentTime - clampedPos) > 1) {
        videoEl.currentTime = clampedPos;
      }
    }

    if (pendingSeekTarget !== null) {
      pendingSeekTarget = null;
    }

    if (playbackIntent === "play" && videoEl.paused) {
      videoEl.play().catch(() => undefined);
    }
  }

  function failoverToNextSource(reason: string) {
    const sources = availableSources.value;
    if (activeSourceIndex.value < sources.length - 1) {
      const pos = currentTime.value;
      if (pos > 5) {
        pendingSeekTarget = pos;
      }
      playbackIntent = isPlaying.value ? "play" : "pause";
      activeSourceIndex.value++;
    } else {
      isLoading.value = false;
      playerError.value =
        "Tất cả nguồn phát đều không khả dụng. Vui lòng thử lại hoặc báo lỗi để được hỗ trợ.";
      playbackMetrics.recordFailure(
        "startup",
        currentSource.value?.url ?? "",
        currentSource.value?.format ?? "",
        reason,
      );
    }
  }

  function onNativeVideoError(_event?: Event) {
    if (!currentSource.value) return;
    failoverToNextSource("Native video error");
  }

  function retryPlayback() {
    activeSourceIndex.value = 0;
    playerError.value = "";
    initPlayer();
  }

  function destroyHls() {
    if (hlsInstance) {
      hlsInstance.destroy();
      hlsInstance = null;
    }
  }

  function selectEpisode(idx: number) {
    userHasExplicitlySelectedEpisode = true;
    if (idx === selectedEpisodeIndex.value) return;
    selectedEpisodeIndex.value = idx;
    activeSourceIndex.value = 0;
    currentTime.value = 0;
    pendingSeekTarget = null;
    playbackIntent = isPlaying.value ? "play" : "pause";
  }

  function selectSource(idx: number) {
    if (idx === activeSourceIndex.value) return;
    pendingSeekTarget = currentTime.value;
    playbackIntent = isPlaying.value ? "play" : "pause";
    activeSourceIndex.value = idx;
  }

  function onLoadedMetadata() {
    const el = video.value;
    if (!el) return;
    duration.value = Number.isFinite(el.duration) ? el.duration : 0;
    currentTime.value = el.currentTime;

    const initialPosition = pendingSeekTarget ?? pendingResumePosition;
    applyResumeOrSeek(el, initialPosition);
  }

  function onMediaLoading() {
    isLoading.value = true;
  }

  function onMediaReady() {
    isLoading.value = false;
    const el = video.value;
    if (!el) return;

    if (
      "requestVideoFrameCallback" in el &&
      typeof el.requestVideoFrameCallback === "function"
    ) {
      el.requestVideoFrameCallback(() => {
        playbackMetrics.recordFirstFrame(
          el,
          currentSource.value?.url ?? "",
          currentSource.value?.format ?? "",
          hasAppliedResumeForGeneration,
          currentTime.value,
        );
      });
    } else {
      playbackMetrics.recordFirstFrame(
        el,
        currentSource.value?.url ?? "",
        currentSource.value?.format ?? "",
        hasAppliedResumeForGeneration,
        currentTime.value,
      );
    }
  }

  function onSeeking() {
    isLoading.value = true;
  }

  function onSeeked() {
    const el = video.value;
    if (!el) return;
    if (pendingSeekTarget !== null) {
      if (
        Math.abs(el.currentTime - pendingSeekTarget) <= 0.5 ||
        (el.duration && el.duration - pendingSeekTarget < 0.5)
      ) {
        playbackMetrics.completeSeekMeasurement(
          el,
          currentSource.value?.url ?? "",
          currentSource.value?.format ?? "",
          "fallback-event",
        );
        pendingSeekTarget = null;
      }
    }
    if (el.readyState >= 3) {
      isLoading.value = false;
    }
  }

  function togglePlay() {
    const el = video.value;
    if (!el) return;
    if (el.paused) {
      playbackIntent = "play";
      playbackMetrics.recordPlayIntent();
      el.play().catch(() => undefined);
    } else {
      playbackIntent = "pause";
      el.pause();
    }
  }

  function seek(targetSeconds: number) {
    const el = video.value;
    if (!el) return;
    const maxDuration =
      duration.value > 0 ? duration.value : Number.MAX_SAFE_INTEGER;
    const target = Math.max(0, Math.min(targetSeconds, maxDuration));
    if (Math.abs(el.currentTime - target) < 0.05) return;

    pendingSeekTarget = target;
    isLoading.value = true;
    playbackMetrics.startSeekMeasurement(target);

    if (
      "requestVideoFrameCallback" in el &&
      typeof el.requestVideoFrameCallback === "function"
    ) {
      el.requestVideoFrameCallback(() => {
        if (
          pendingSeekTarget !== null &&
          Math.abs(el.currentTime - pendingSeekTarget) <= 0.5
        ) {
          playbackMetrics.completeSeekMeasurement(
            el,
            currentSource.value?.url ?? "",
            currentSource.value?.format ?? "",
            "rvfc",
          );
          pendingSeekTarget = null;
          isLoading.value = false;
        }
      });
    }

    el.currentTime = target;
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
      doRecordView(slug)
        .then((res) => {
          if (res?.counted) viewCount.value = res.viewCount;
        })
        .catch(() => undefined);
    }

    // Save progress periodically
    if (Math.floor(currentTime.value) % 5 === 0) {
      const epNum = parseEpisodeNumber(currentEpisode.value?.number);
      saveLocalProgress(slug, epNum, Math.floor(currentTime.value));
      if (authState.value === "authenticated") {
        doRecordHistory(slug, {
          episodeNumber: epNum,
          progressSeconds: Math.floor(currentTime.value),
        }).catch(() => undefined);
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
    if (!isClientEnvironment() || !playerFrame.value) return;
    scrollObserver = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (!entry) return;
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

  watch([currentSource, video], ([src, vid]) => {
    if (!src || !vid) return;
    if (isEmbed.value) {
      destroyHls();
      vid.pause();
      vid.removeAttribute("src");
      vid.load();
      currentTime.value = 0;
      duration.value = 0;
      return;
    }
    initPlayer();
  });

  onBeforeUnmount(() => {
    currentGeneration++;
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
    isResumeDecisionFrozen: computed(() => resumeDecisionFrozen),
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
    onMediaLoading,
    onMediaReady,
    onSeeking,
    onSeeked,
    onEnded,
    onNativeVideoError,
    retryPlayback,
    setupIntersectionObserver,
  };
}
