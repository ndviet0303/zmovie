<script setup lang="ts">
import { LoaderCircle } from "@lucide/vue";
import type { PlaybackSource } from "~/types/catalog";

defineProps<{
  source: PlaybackSource | null;
  isEmbed: boolean;
  isLoading: boolean;
  playerError: string;
}>();

const emit = defineEmits<{
  timeupdate: [];
  play: [];
  pause: [];
  ended: [];
  loadedmetadata: [];
  "loading-start": [];
  "loading-end": [];
  seeking: [];
  seeked: [];
  error: [e: Event];
  retry: [];
  "video-ref": [el: HTMLVideoElement | null];
}>();

const videoEl = ref<HTMLVideoElement | null>(null);

watch(videoEl, (el) => {
  emit("video-ref", el);
});
</script>

<template>
  <div
    class="relative aspect-video w-full overflow-hidden bg-black"
    :aria-busy="isLoading"
  >
    <!-- Embed Iframe Mode -->
    <iframe
      v-if="isEmbed && source"
      :src="source.url"
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
      @load="emit('loading-end')"
    />

    <!-- Native HLS Video Mode -->
    <video
      v-else
      ref="videoEl"
      class="h-full w-full object-contain"
      playsinline
      crossorigin="anonymous"
      @timeupdate="emit('timeupdate')"
      @play="emit('play')"
      @canplay="emit('loading-end')"
      @playing="emit('loading-end')"
      @pause="emit('pause')"
      @ended="emit('ended')"
      @loadedmetadata="emit('loadedmetadata')"
      @loadstart="emit('loading-start')"
      @waiting="emit('loading-start')"
      @stalled="emit('loading-start')"
      @seeking="emit('seeking')"
      @seeked="emit('seeked')"
      @error="emit('error', $event)"
    />

    <!-- Overlays Slot (Danmaku, DualSub, SkipIntro) -->
    <slot />

    <!-- Loading Spinner -->
    <Transition
      enter-active-class="transition-opacity duration-150"
      enter-from-class="opacity-0"
      leave-active-class="transition-opacity duration-150"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isLoading"
        class="pointer-events-none absolute inset-0 flex items-center justify-center bg-black/55 backdrop-blur-[1px]"
        role="status"
        aria-label="Đang tải phim"
      >
        <div
          class="relative flex h-16 w-16 items-center justify-center rounded-full bg-black/45 shadow-lg shadow-black/40"
        >
          <span
            class="absolute inset-2 animate-ping rounded-full border border-amber-400/35"
          />
          <LoaderCircle
            class="relative h-10 w-10 animate-spin text-amber-500 drop-shadow-[0_0_10px_rgba(245,158,11,0.45)]"
          />
        </div>
      </div>
    </Transition>

    <!-- Error Message -->
    <div
      v-if="playerError"
      class="absolute inset-0 flex flex-col items-center justify-center bg-black/85 p-6 text-center"
    >
      <p class="max-w-md text-sm text-red-400">{{ playerError }}</p>
      <button
        type="button"
        class="mt-4 rounded-lg bg-amber-500 px-4 py-2 text-xs font-semibold text-black transition hover:bg-amber-400 focus:outline-none focus:ring-2 focus:ring-amber-400/50"
        @click="emit('retry')"
      >
        Thử lại
      </button>
    </div>
  </div>
</template>
