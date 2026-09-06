<script setup lang="ts">
import { Play } from "@lucide/vue";

defineProps<{
  slug: string;
  episodeList: Array<{ episodeNumber: number; title?: string }>;
}>();

const activeAudio = ref<"sub" | "dual" | "dub">("sub");
const isCompact = ref(false);
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Audio & Server Toggle Row -->
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-3">
        <!-- Season Dropdown -->
        <div
          class="rounded-xl border border-white/10 bg-[#191b24] px-3.5 py-2 text-xs font-bold text-white"
        >
          Phần 1 ▾
        </div>

        <!-- Audio format buttons -->
        <div class="flex items-center gap-1.5">
          <button
            type="button"
            class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
            :class="
              activeAudio === 'sub'
                ? 'border-primary bg-primary/10 text-primary font-bold'
                : 'border-white/10 bg-white/5 text-white/80'
            "
            @click="activeAudio = 'sub'"
          >
            Phụ đề #1
          </button>
          <button
            type="button"
            class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
            :class="
              activeAudio === 'dual'
                ? 'border-primary bg-primary/10 text-primary font-bold'
                : 'border-white/10 bg-white/5 text-white/80'
            "
            @click="activeAudio = 'dual'"
          >
            Song ngữ
          </button>
          <button
            type="button"
            class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition"
            :class="
              activeAudio === 'dub'
                ? 'border-primary bg-primary/10 text-primary font-bold'
                : 'border-white/10 bg-white/5 text-white/80'
            "
            @click="activeAudio = 'dub'"
          >
            Thuyết Minh #1
          </button>
        </div>
      </div>

      <!-- Rút gọn toggle -->
      <div class="flex items-center gap-2 text-xs text-white/70">
        <span>Rút gọn</span>
        <button
          type="button"
          class="h-5 w-9 rounded-full bg-primary p-0.5 transition cursor-pointer"
          @click="isCompact = !isCompact"
        >
          <div
            class="size-4 rounded-full bg-black transition"
            :class="isCompact ? 'translate-x-0' : 'translate-x-4'"
          />
        </button>
      </div>
    </div>

    <!-- Episode Grid Buttons -->
    <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2.5">
      <NuxtLink
        v-for="ep in isCompact ? episodeList.slice(0, 12) : episodeList"
        :key="ep.episodeNumber"
        :to="{
          path: `/watch/${slug}`,
          query: { episode: ep.episodeNumber },
        }"
        class="flex items-center justify-center gap-2 rounded-2xl border border-white/10 bg-[#191b24] py-3 text-xs font-bold text-white transition hover:border-primary hover:bg-primary/15 hover:text-primary active:scale-95"
      >
        <Play class="size-3.5 fill-current" />
        <span>Tập {{ ep.episodeNumber }}</span>
      </NuxtLink>
    </div>
  </div>
</template>
