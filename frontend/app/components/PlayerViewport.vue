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
  "video-ref": [el: HTMLVideoElement | null];
}>();

const videoEl = ref<HTMLVideoElement | null>(null);

watch(videoEl, (el) => {
  emit("video-ref", el);
});
</script>

<template>
  <div class="relative aspect-video w-full overflow-hidden bg-black">
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
      @pause="emit('pause')"
      @ended="emit('ended')"
      @loadedmetadata="emit('loadedmetadata')"
    />

    <!-- Overlays Slot (Danmaku, DualSub, SkipIntro) -->
    <slot />

    <!-- Loading Spinner -->
    <div
      v-if="isLoading"
      class="pointer-events-none absolute inset-0 flex items-center justify-center bg-black/50"
    >
      <LoaderCircle class="h-10 w-10 animate-spin text-amber-500" />
    </div>

    <!-- Error Message -->
    <div
      v-if="playerError"
      class="absolute inset-0 flex flex-col items-center justify-center bg-black/85 p-6 text-center"
    >
      <p class="max-w-md text-sm text-red-400">{{ playerError }}</p>
    </div>
  </div>
</template>
