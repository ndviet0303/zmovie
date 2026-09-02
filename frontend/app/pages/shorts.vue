<script setup lang="ts">
import { ref } from "vue";
import {
  ChevronLeft,
  Film,
  Heart,
  Share2,
  Volume2,
  VolumeX,
} from "@lucide/vue";

interface ShortItem {
  id: string;
  title: string;
  videoUrl: string;
  targetMovieSlug: string;
  likes: number;
  isLiked?: boolean;
}

const clips = ref<ShortItem[]>([
  {
    id: "1",
    title: "Na Tra 2: Ma Đồng Náo Hải - Trận chiến đỉnh cao rực lửa 🔥",
    videoUrl:
      "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
    targetMovieSlug: "na-tra-ma-dong-nao-hai",
    likes: 1240,
  },
  {
    id: "2",
    title: "Dune 2: Phân cảnh cỡi giun cát huyền thoại trên sa mạc 🏜️",
    videoUrl:
      "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
    targetMovieSlug: "dune-phan-hai",
    likes: 3420,
  },
  {
    id: "3",
    title: "Spider-Man: Across the Spider-Verse - Vũ đạo thị giác đỉnh nóc 🕸️",
    videoUrl:
      "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4",
    targetMovieSlug: "spider-man-du-hanh-vu-tru-nhen",
    likes: 5120,
  },
]);

const activeIndex = ref(0);
const isMuted = ref(true);

function toggleLike(idx: number) {
  const clip = clips.value[idx];
  if (!clip) return;
  clip.isLiked = !clip.isLiked;
  clip.likes += clip.isLiked ? 1 : -1;
}

function toggleMute() {
  isMuted.value = !isMuted.value;
}

async function shareClip(clip: ShortItem) {
  if (import.meta.client && navigator.share) {
    try {
      await navigator.share({
        title: clip.title,
        url: window.location.href,
      });
    } catch {
      // ignore
    }
  }
}
</script>

<template>
  <div
    class="h-screen w-full overflow-y-scroll snap-y snap-mandatory bg-black text-white select-none"
  >
    <!-- Top Floating Back Header -->
    <div class="fixed top-4 left-4 z-40 flex items-center gap-3">
      <NuxtLink
        to="/"
        class="flex h-10 w-10 items-center justify-center rounded-full bg-black/60 text-white backdrop-blur-md transition hover:bg-black/80"
      >
        <ChevronLeft class="h-5 w-5" />
      </NuxtLink>
      <span
        class="text-sm font-bold tracking-wider uppercase text-amber-400 drop-shadow"
      >
        ZMovie Shorts
      </span>
    </div>

    <!-- Fullscreen Short Snap Slides -->
    <section
      v-for="(clip, idx) in clips"
      :key="clip.id"
      class="relative h-screen w-full snap-start overflow-hidden flex items-center justify-center bg-zinc-950"
    >
      <video
        :src="clip.videoUrl"
        class="h-full w-full object-cover sm:max-w-md"
        loop
        playsinline
        :muted="isMuted"
        :autoplay="idx === activeIndex"
      />

      <!-- Right Action Column -->
      <div
        class="absolute right-4 bottom-24 z-30 flex flex-col items-center gap-5"
      >
        <!-- Sound Mute/Unmute -->
        <button
          type="button"
          class="flex h-11 w-11 items-center justify-center rounded-full bg-black/50 backdrop-blur-md transition hover:scale-110 active:scale-95"
          @click="toggleMute"
        >
          <VolumeX v-if="isMuted" class="h-5 w-5 text-red-400" />
          <Volume2 v-else class="h-5 w-5 text-white" />
        </button>

        <!-- Like Button -->
        <button
          type="button"
          class="flex flex-col items-center gap-1 transition hover:scale-110 active:scale-95"
          @click="toggleLike(idx)"
        >
          <div
            class="flex h-11 w-11 items-center justify-center rounded-full bg-black/50 backdrop-blur-md"
            :class="{ 'text-red-500': clip.isLiked }"
          >
            <Heart class="h-5 w-5" :class="{ 'fill-current': clip.isLiked }" />
          </div>
          <span class="text-[11px] font-semibold">{{ clip.likes }}</span>
        </button>

        <!-- Share Button -->
        <button
          type="button"
          class="flex flex-col items-center gap-1 transition hover:scale-110 active:scale-95"
          @click="shareClip(clip)"
        >
          <div
            class="flex h-11 w-11 items-center justify-center rounded-full bg-black/50 backdrop-blur-md"
          >
            <Share2 class="h-5 w-5" />
          </div>
          <span class="text-[11px] font-semibold">Chia sẻ</span>
        </button>
      </div>

      <!-- Bottom Caption & Direct Movie Watch Link -->
      <div class="absolute bottom-6 left-4 right-16 z-30 flex flex-col gap-3">
        <p class="text-sm font-medium leading-snug drop-shadow-md line-clamp-2">
          {{ clip.title }}
        </p>

        <div>
          <NuxtLink
            :to="`/watch/${clip.targetMovieSlug}`"
            class="inline-flex items-center gap-2 rounded-full bg-amber-500 px-4 py-2 text-xs font-bold text-black shadow-lg shadow-amber-500/30 transition hover:bg-amber-400 active:scale-95"
          >
            <Film class="h-4 w-4" />
            <span>Xem phim ngay</span>
          </NuxtLink>
        </div>
      </div>
    </section>
  </div>
</template>
