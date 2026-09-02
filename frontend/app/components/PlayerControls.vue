<script setup lang="ts">
import {
  FastForward,
  Maximize,
  Pause,
  Play,
  Settings,
  Tv,
  Volume2,
  VolumeX,
} from "@lucide/vue";

import { computed, ref } from "vue";
import type { PlaybackSource } from "~/types/catalog";
import type { QualityOption, SubtitleOption } from "~/types/watch";

const props = defineProps<{
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  volume: number;
  isMuted: boolean;
  playbackRate: number;
  qualityOptions: QualityOption[];
  selectedQuality: number;
  subtitleOptions: SubtitleOption[];
  selectedSubtitle: number;
  availableSources: PlaybackSource[];
  activeSourceIndex: number;
  isTheaterMode: boolean;
  showSkipIntroPrompt: boolean;
}>();

const emit = defineEmits<{
  "toggle-play": [];
  seek: [seconds: number];
  "set-volume": [v: number];
  "toggle-mute": [];
  "set-rate": [rate: number];
  "set-quality": [level: number];
  "set-subtitle": [idx: number];
  "select-source": [idx: number];
  "toggle-theater": [];
  "toggle-fullscreen": [];
  "skip-intro": [];
}>();

const isSettingsOpen = ref(false);
const hoverTime = ref<number | null>(null);
const hoverPositionX = ref(0);

const progressPercent = computed(() => {
  if (!props.duration) return 0;
  return Math.min(100, (props.currentTime / props.duration) * 100);
});

function formatTime(seconds: number): string {
  if (!seconds || isNaN(seconds)) return "00:00";
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  const parts = [m.toString().padStart(2, "0"), s.toString().padStart(2, "0")];
  if (h > 0) parts.unshift(h.toString());
  return parts.join(":");
}

function onSeekbarMouseMove(e: MouseEvent) {
  const target = e.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const offsetX = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
  const ratio = offsetX / rect.width;
  hoverPositionX.value = offsetX;
  hoverTime.value = ratio * props.duration;
}

function onSeekbarMouseLeave() {
  hoverTime.value = null;
}

function onSeekbarClick(e: MouseEvent) {
  const target = e.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const offsetX = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
  const ratio = offsetX / rect.width;
  emit("seek", ratio * props.duration);
}
</script>

<template>
  <div class="relative">
    <!-- Skip Intro Prompt Button -->
    <div
      v-if="showSkipIntroPrompt"
      class="absolute bottom-16 right-6 z-30 transition-all duration-300"
    >
      <button
        type="button"
        class="flex items-center gap-2 rounded-full border border-amber-500/50 bg-black/80 px-4 py-2 text-xs font-semibold text-amber-400 backdrop-blur-md transition hover:bg-amber-500 hover:text-black"
        @click="emit('skip-intro')"
      >
        <FastForward class="h-4 w-4" />
        Bỏ qua giới thiệu (Skip Intro)
      </button>
    </div>

    <!-- Controls Overlay Bar -->
    <div class="bg-gradient-to-t from-black/90 via-black/40 to-transparent p-4">
      <!-- Progress Bar with Storyboard Tooltip -->
      <div
        class="group relative mb-3 h-2 w-full cursor-pointer rounded-full bg-zinc-700/60"
        @mousemove="onSeekbarMouseMove"
        @mouseleave="onSeekbarMouseLeave"
        @click="onSeekbarClick"
      >
        <!-- Storyboard Hover Tooltip -->
        <div
          v-if="hoverTime !== null"
          class="pointer-events-none absolute -top-10 -translate-x-1/2 rounded bg-zinc-900 px-2 py-1 text-xs text-white shadow"
          :style="{ left: `${hoverPositionX}px` }"
        >
          {{ formatTime(hoverTime) }}
        </div>

        <div
          class="h-full rounded-full bg-amber-500 transition-all"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>

      <!-- Action Buttons Row -->
      <div class="flex items-center justify-between text-zinc-300">
        <!-- Left: Play/Pause, Volume, Time -->
        <div class="flex items-center gap-4">
          <button
            type="button"
            class="transition hover:text-white"
            @click="emit('toggle-play')"
          >
            <Pause v-if="isPlaying" class="h-6 w-6 text-amber-400" />
            <Play v-else class="h-6 w-6" />
          </button>

          <div class="flex items-center gap-2">
            <button
              type="button"
              class="transition hover:text-white"
              @click="emit('toggle-mute')"
            >
              <VolumeX
                v-if="isMuted || volume === 0"
                class="h-5 w-5 text-red-400"
              />
              <Volume2 v-else class="h-5 w-5" />
            </button>
            <input
              type="range"
              min="0"
              max="1"
              step="0.05"
              :value="isMuted ? 0 : volume"
              class="h-1.5 w-16 cursor-pointer accent-amber-500"
              @input="
                (e) =>
                  emit(
                    'set-volume',
                    parseFloat((e.target as HTMLInputElement).value),
                  )
              "
            />
          </div>

          <span class="text-xs font-mono text-zinc-400">
            {{ formatTime(currentTime) }} / {{ formatTime(duration) }}
          </span>
        </div>

        <!-- Right: Source, Settings, Theater, Fullscreen -->
        <div class="flex items-center gap-3">
          <!-- Multi-Source Selector -->
          <div v-if="availableSources.length > 1" class="relative">
            <select
              :value="activeSourceIndex"
              class="rounded border border-zinc-700 bg-zinc-900 px-2 py-1 text-xs text-amber-400 focus:outline-none"
              @change="
                (e) =>
                  emit(
                    'select-source',
                    parseInt((e.target as HTMLSelectElement).value, 10),
                  )
              "
            >
              <option
                v-for="(src, idx) in availableSources"
                :key="src.url"
                :value="idx"
              >
                {{ src.provider }} ({{ src.format.toUpperCase() }})
              </option>
            </select>
          </div>

          <!-- Quality / Settings Button -->
          <div class="relative">
            <button
              type="button"
              class="transition hover:text-white"
              @click="isSettingsOpen = !isSettingsOpen"
            >
              <Settings class="h-5 w-5" />
            </button>

            <!-- Quick Settings Dropdown -->
            <div
              v-if="isSettingsOpen"
              class="absolute bottom-8 right-0 w-44 rounded-lg border border-zinc-800 bg-zinc-900/95 p-3 text-xs shadow-xl backdrop-blur-md"
            >
              <p class="mb-2 font-semibold text-zinc-200">Tốc độ phát</p>
              <div class="mb-3 grid grid-cols-4 gap-1">
                <button
                  v-for="rate in [0.75, 1, 1.25, 1.5]"
                  :key="rate"
                  type="button"
                  class="rounded py-0.5 text-center transition"
                  :class="
                    playbackRate === rate
                      ? 'bg-amber-500 text-black font-semibold'
                      : 'bg-zinc-800 text-zinc-300 hover:bg-zinc-700'
                  "
                  @click="
                    emit('set-rate', rate);
                    isSettingsOpen = false;
                  "
                >
                  {{ rate }}x
                </button>
              </div>

              <div v-if="qualityOptions.length > 0">
                <p class="mb-2 font-semibold text-zinc-200">Chất lượng</p>
                <div class="space-y-1">
                  <button
                    v-for="q in qualityOptions"
                    :key="q.level"
                    type="button"
                    class="block w-full rounded px-2 py-1 text-left transition"
                    :class="
                      selectedQuality === q.level
                        ? 'bg-amber-500 text-black font-semibold'
                        : 'hover:bg-zinc-800 text-zinc-300'
                    "
                    @click="
                      emit('set-quality', q.level);
                      isSettingsOpen = false;
                    "
                  >
                    {{ q.label }}
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Theater Mode -->
          <button
            type="button"
            class="transition hover:text-white"
            :title="isTheaterMode ? 'Mặc định' : 'Chế độ rạp'"
            @click="emit('toggle-theater')"
          >
            <Tv class="h-5 w-5" :class="{ 'text-amber-400': isTheaterMode }" />
          </button>

          <!-- Fullscreen -->
          <button
            type="button"
            class="transition hover:text-white"
            @click="emit('toggle-fullscreen')"
          >
            <Maximize class="h-5 w-5" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
