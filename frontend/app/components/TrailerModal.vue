<script setup lang="ts">
import { X } from "@lucide/vue";

const props = defineProps<{
  open: boolean;
  title: string;
  trailerUrl?: string;
}>();

const emit = defineEmits<{
  (e: "close"): void;
}>();

const embedUrl = computed(() => {
  if (!props.trailerUrl) {
    return `https://www.youtube.com/embed?listType=search&list=${encodeURIComponent(`${props.title} official trailer`)}&autoplay=1`;
  }

  // Handle standard YouTube links: https://www.youtube.com/watch?v=VIDEO_ID
  const watchMatch = props.trailerUrl.match(
    /(?:watch\?v=|youtu\.be\/)([\w-]+)/,
  );
  if (watchMatch && watchMatch[1]) {
    return `https://www.youtube.com/embed/${watchMatch[1]}?autoplay=1`;
  }

  // Already an embed URL or other video source
  return props.trailerUrl;
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-md transition-opacity"
      role="dialog"
      aria-modal="true"
      @click.self="emit('close')"
    >
      <div
        class="relative w-full max-w-4xl overflow-hidden rounded-3xl border border-white/10 bg-surface-container-high p-4 shadow-2xl sm:p-6"
      >
        <div class="mb-4 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span
              class="rounded-md bg-primary px-2 py-0.5 text-xs font-bold text-primary-container-foreground"
              >TRAILER</span
            >
            <h3
              class="font-display text-lg font-semibold text-foreground sm:text-xl"
            >
              {{ title }}
            </h3>
          </div>
          <button
            class="grid size-9 place-items-center rounded-full border border-white/10 text-muted-foreground transition hover:border-white/30 hover:text-foreground"
            aria-label="Close modal"
            @click="emit('close')"
          >
            <X class="size-5" />
          </button>
        </div>

        <div
          class="relative aspect-video w-full overflow-hidden rounded-2xl bg-black"
        >
          <iframe
            :src="embedUrl"
            class="size-full border-0"
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
  </Teleport>
</template>
