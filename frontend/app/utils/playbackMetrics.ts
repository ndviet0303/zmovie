/**
 * Opt-in playback performance measurement tooling for startup and seek latency.
 * Complies with OpenSpec optimize-playback-startup-seeking requirements:
 * - Frame-based measurement using requestVideoFrameCallback when available
 * - Milestone tracking: open-to-frame, play-intent-to-frame, seek-to-frame
 * - Distinguishes cold, warm, and unknown cache cohorts
 * - Explicitly sanitizes URLs (strips query parameters and auth tokens)
 * - Records timeouts and failures without dropping samples
 * - Exports raw samples for statistical analysis (median, p95)
 */

export type CacheCohort = "cold" | "warm" | "unknown";
export type FrameMeasurementMethod = "rvfc" | "fallback-event";

export interface PlaybackMetricSample {
  id: string;
  type: "startup" | "resume" | "seek";
  targetTimeSeconds?: number;
  actualTimeSeconds?: number;
  timeDeltaSeconds?: number;
  latencyMs: number;
  intentToFrameMs?: number;
  cacheCohort: CacheCohort;
  frameMethod: FrameMeasurementMethod;
  sanitizedSourceUrl: string;
  sourceFormat: string;
  success: boolean;
  errorCode?: string;
  timestamp: number;
}

export interface PlaybackSessionMetrics {
  sessionId: string;
  slug: string;
  cacheCohort: CacheCohort;
  openTimestamp: number;
  playIntentTimestamp?: number;
  firstFrameTimestamp?: number;
  startupDurationMs?: number;
  playIntentToFrameMs?: number;
  samples: PlaybackMetricSample[];
}

interface WindowWithMetrics extends Window {
  __ZMOVIE_PLAYBACK_METRICS__?: {
    exportSamples: () => PlaybackMetricSample[];
    exportSession: () => PlaybackSessionMetrics | null;
    clear: () => void;
    enable: (cohort?: CacheCohort) => void;
    disable: () => void;
  };
}

const STORAGE_KEY = "zmovie.playback-metrics.enabled";

export function isMetricsEnabled(): boolean {
  if (typeof window === "undefined") return false;
  try {
    const urlParams = new URLSearchParams(window.location.search);
    if (
      urlParams.get("metrics") === "1" ||
      urlParams.get("metrics") === "true"
    ) {
      return true;
    }
    return window.localStorage?.getItem(STORAGE_KEY) === "1";
  } catch {
    return false;
  }
}

export function setMetricsEnabled(enabled: boolean): void {
  if (typeof window === "undefined") return;
  try {
    if (enabled) {
      window.localStorage?.setItem(STORAGE_KEY, "1");
    } else {
      window.localStorage?.removeItem(STORAGE_KEY);
    }
  } catch {
    // ignore
  }
}

export function sanitizeUrl(rawUrl: string): string {
  if (!rawUrl) return "";
  try {
    const parsed = new URL(rawUrl, "http://localhost");
    return `${parsed.protocol}//${parsed.host}${parsed.pathname}`;
  } catch {
    return rawUrl.split("?")[0].split("#")[0];
  }
}

class PlaybackMetricsCollector {
  private activeSession: PlaybackSessionMetrics | null = null;
  private allSamples: PlaybackMetricSample[] = [];
  private pendingSeek: {
    targetSeconds: number;
    startTime: number;
    timerId?: number;
  } | null = null;

  startSession(slug: string, cacheCohort: CacheCohort = "unknown"): void {
    if (!isMetricsEnabled()) return;
    this.activeSession = {
      sessionId: `session_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
      slug,
      cacheCohort,
      openTimestamp: performance.now(),
      samples: [],
    };
    this.attachGlobalBridge();
  }

  recordPlayIntent(): void {
    if (!this.activeSession || this.activeSession.playIntentTimestamp) return;
    this.activeSession.playIntentTimestamp = performance.now();
  }

  recordFirstFrame(
    video: HTMLVideoElement,
    sourceUrl: string,
    sourceFormat: string,
    isResume: boolean,
    targetPosition?: number,
  ): void {
    if (!this.activeSession || this.activeSession.firstFrameTimestamp) return;

    const now = performance.now();
    this.activeSession.firstFrameTimestamp = now;
    const startupDurationMs = now - this.activeSession.openTimestamp;
    const playIntentMs = this.activeSession.playIntentTimestamp
      ? now - this.activeSession.playIntentTimestamp
      : undefined;

    this.activeSession.startupDurationMs = startupDurationMs;
    this.activeSession.playIntentToFrameMs = playIntentMs;

    const hasRvfc =
      "requestVideoFrameCallback" in video &&
      typeof video.requestVideoFrameCallback === "function";
    const sample: PlaybackMetricSample = {
      id: `sample_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
      type: isResume ? "resume" : "startup",
      targetTimeSeconds: targetPosition ?? 0,
      actualTimeSeconds: video.currentTime,
      timeDeltaSeconds: Math.abs(video.currentTime - (targetPosition ?? 0)),
      latencyMs: startupDurationMs,
      intentToFrameMs: playIntentMs,
      cacheCohort: this.activeSession.cacheCohort,
      frameMethod: hasRvfc ? "rvfc" : "fallback-event",
      sanitizedSourceUrl: sanitizeUrl(sourceUrl),
      sourceFormat,
      success: true,
      timestamp: Date.now(),
    };

    this.activeSession.samples.push(sample);
    this.allSamples.push(sample);
  }

  startSeekMeasurement(targetSeconds: number): void {
    if (!isMetricsEnabled()) return;
    this.pendingSeek = {
      targetSeconds,
      startTime: performance.now(),
    };
  }

  completeSeekMeasurement(
    video: HTMLVideoElement,
    sourceUrl: string,
    sourceFormat: string,
    method: FrameMeasurementMethod,
  ): void {
    if (!this.pendingSeek) return;

    const { targetSeconds, startTime } = this.pendingSeek;
    const durationMs = performance.now() - startTime;
    const timeDelta = Math.abs(video.currentTime - targetSeconds);

    // Accept within 0.5s tolerance per spec
    const isMatched =
      timeDelta <= 0.5 ||
      (video.duration && video.duration - targetSeconds < 0.5);

    const sample: PlaybackMetricSample = {
      id: `seek_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
      type: "seek",
      targetTimeSeconds: targetSeconds,
      actualTimeSeconds: video.currentTime,
      timeDeltaSeconds: timeDelta,
      latencyMs: durationMs,
      cacheCohort: this.activeSession?.cacheCohort ?? "unknown",
      frameMethod: method,
      sanitizedSourceUrl: sanitizeUrl(sourceUrl),
      sourceFormat,
      success: Boolean(isMatched),
      errorCode: isMatched ? undefined : "SEEK_TARGET_MISMATCH",
      timestamp: Date.now(),
    };

    this.pendingSeek = null;
    this.activeSession?.samples.push(sample);
    this.allSamples.push(sample);
  }

  recordFailure(
    type: "startup" | "resume" | "seek",
    sourceUrl: string,
    sourceFormat: string,
    errorCode: string,
  ): void {
    if (!isMetricsEnabled()) return;
    const sample: PlaybackMetricSample = {
      id: `err_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
      type,
      latencyMs: -1,
      cacheCohort: this.activeSession?.cacheCohort ?? "unknown",
      frameMethod: "fallback-event",
      sanitizedSourceUrl: sanitizeUrl(sourceUrl),
      sourceFormat,
      success: false,
      errorCode,
      timestamp: Date.now(),
    };
    this.activeSession?.samples.push(sample);
    this.allSamples.push(sample);
  }

  exportSamples(): PlaybackMetricSample[] {
    return [...this.allSamples];
  }

  exportSession(): PlaybackSessionMetrics | null {
    return this.activeSession
      ? JSON.parse(JSON.stringify(this.activeSession))
      : null;
  }

  clear(): void {
    this.allSamples = [];
    this.activeSession = null;
    this.pendingSeek = null;
  }

  private attachGlobalBridge(): void {
    if (typeof window === "undefined") return;
    const win = window as WindowWithMetrics;
    win.__ZMOVIE_PLAYBACK_METRICS__ = {
      exportSamples: () => this.exportSamples(),
      exportSession: () => this.exportSession(),
      clear: () => this.clear(),
      enable: (cohort: CacheCohort = "unknown") => {
        setMetricsEnabled(true);
        this.startSession("manual", cohort);
      },
      disable: () => setMetricsEnabled(false),
    };
  }
}

export const playbackMetrics = new PlaybackMetricsCollector();
