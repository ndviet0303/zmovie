<script setup lang="ts">
import { Maximize2, Pause, Play, Tv, X } from "@lucide/vue";

const props = defineProps<{
  isActive: boolean;
  isPlaying: boolean;
  title: string;
  episodeName: string;
  videoElement: HTMLVideoElement | null;
}>();

const emit = defineEmits<{
  "toggle-play": [];
  close: [];
  maximize: [];
}>();

async function toggleNativePiP() {
  if (!import.meta.client || !props.videoElement) return;
  try {
    if (document.pictureInPictureElement) {
      await document.exitPictureInPicture();
    } else if (document.pictureInPictureEnabled) {
      await props.videoElement.requestPictureInPicture();
    }
  } catch {
    // PiP not supported or user denied
  }
}
</script>

<template>
  <div
    v-if="isActive"
    class="fixed bottom-6 right-6 z-50 flex w-80 flex-col overflow-hidden rounded-xl border border-zinc-700 bg-zinc-900/95 shadow-2xl backdrop-blur-md transition-all duration-300"
  >
    <!-- Mini Header Bar -->
    <div
      class="flex items-center justify-between border-b border-zinc-800 bg-zinc-950/80 px-3 py-2 text-xs"
    >
      <div class="truncate font-medium text-zinc-200">
        {{ title }} - {{ episodeName }}
      </div>
      <div class="flex items-center gap-1 text-zinc-400">
        <button
          type="button"
          class="rounded p-1 hover:text-white"
          title="Picture-in-Picture"
          @click="toggleNativePiP"
        >
          <Tv class="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          class="rounded p-1 hover:text-white"
          title="Xem lại trên đầu trang"
          @click="emit('maximize')"
        >
          <Maximize2 class="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          class="rounded p-1 hover:text-red-400"
          title="Đóng"
          @click="emit('close')"
        >
          <X class="h-3.5 w-3.5" />
        </button>
      </div>
    </div>

    <!-- Mini Controls Action Bar -->
    <div class="flex items-center justify-between bg-zinc-900 px-3 py-2">
      <span class="text-[11px] text-zinc-400">Đang phát nền</span>
      <button
        type="button"
        class="flex h-8 w-8 items-center justify-center rounded-full bg-amber-500 text-black transition hover:bg-amber-400"
        @click="emit('toggle-play')"
      >
        <Pause v-if="isPlaying" class="h-4 w-4" />
        <Play v-else class="h-4 w-4 ml-0.5" />
      </button>
    </div>
  </div>
</template>
