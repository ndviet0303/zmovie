<script setup lang="ts">
import {
  AlertTriangle,
  Bookmark,
  Check,
  ChevronLeft,
  ChevronRight,
  Lightbulb,
  Send,
  Share2,
  Users,
} from "@lucide/vue";

withDefaults(
  defineProps<{
    slug: string;
    episodeNumber: number;
    totalEpisodes: number;
    currentTime?: number;
    isDanmakuEnabled: boolean;
    autoNext?: boolean;
    isSaved?: boolean;
    isTheater?: boolean;
  }>(),
  {
    currentTime: 0,
    autoNext: true,
    isSaved: false,
    isTheater: false,
  },
);

const emit = defineEmits<{
  "prev-episode": [];
  "next-episode": [];
  "toggle-danmaku": [];
  "send-danmaku": [content: string, color: string];
  "toggle-auto-next": [];
  "toggle-saved": [];
  "toggle-theater": [];
  "open-report": [];
}>();

const danmakuInput = ref("");
const selectedColor = ref("#ffffff");
const isCopied = ref(false);

const colors = [
  "#ffffff",
  "#f59e0b",
  "#38bdf8",
  "#4ade80",
  "#f43f5e",
  "#a855f7",
];

function handleSendDanmaku() {
  const text = danmakuInput.value.trim();
  if (!text) return;
  emit("send-danmaku", text, selectedColor.value);
  danmakuInput.value = "";
}

function handleShare() {
  if (!import.meta.client) return;
  void navigator.clipboard.writeText(window.location.href);
  isCopied.value = true;
  setTimeout(() => {
    isCopied.value = false;
  }, 2000);
}
</script>

<template>
  <div
    class="mt-3 flex flex-col gap-3 rounded-2xl border border-white/8 bg-[#14151c] p-3 sm:p-4"
  >
    <!-- Top Row: Danmaku Controls & Input -->
    <div class="flex flex-wrap items-center gap-2 sm:gap-3">
      <button
        type="button"
        class="rounded-xl px-3 py-2 text-xs font-bold transition"
        :class="
          isDanmakuEnabled
            ? 'bg-primary/20 text-primary border border-primary/40'
            : 'bg-white/5 text-muted-foreground border border-white/10 hover:text-white'
        "
        @click="emit('toggle-danmaku')"
      >
        {{ isDanmakuEnabled ? "Bật đạn mạc" : "Tắt đạn mạc" }}
      </button>

      <!-- Color picker dots -->
      <div class="hidden sm:flex items-center gap-1.5 px-1">
        <button
          v-for="color in colors"
          :key="color"
          type="button"
          class="size-4 rounded-full border border-white/20 transition hover:scale-125"
          :class="
            selectedColor === color ? 'ring-2 ring-primary scale-110' : ''
          "
          :style="{ backgroundColor: color }"
          :aria-label="`Màu ${color}`"
          @click="selectedColor = color"
        />
      </div>

      <!-- Danmaku input -->
      <form
        class="relative flex flex-1 items-center min-w-[12rem]"
        @submit.prevent="handleSendDanmaku"
      >
        <input
          v-model="danmakuInput"
          type="text"
          maxlength="120"
          placeholder="Thêm bình luận bay qua màn hình..."
          class="h-9 w-full rounded-xl border border-white/10 bg-black/40 pl-3 pr-9 text-xs text-white placeholder-white/40 outline-none transition focus:border-primary/60"
        />
        <button
          type="submit"
          class="absolute right-1.5 grid size-6 place-items-center rounded-lg text-primary transition hover:bg-primary/20 disabled:opacity-40"
          :disabled="!danmakuInput.trim()"
          aria-label="Gửi đạn mạc"
        >
          <Send class="size-3.5" />
        </button>
      </form>
    </div>

    <!-- Bottom Row: Navigation, Report, Share, Watch Party, Auto-next -->
    <div
      class="flex flex-wrap items-center justify-between gap-2 border-t border-white/6 pt-3 text-xs"
    >
      <!-- Left: Episode Prev / Next & Auto next -->
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1 rounded-xl border border-white/10 bg-white/5 px-3 py-1.5 font-medium transition hover:border-primary/40 disabled:opacity-30 disabled:cursor-not-allowed"
          :disabled="episodeNumber <= 1"
          @click="emit('prev-episode')"
        >
          <ChevronLeft class="size-3.5" />
          <span class="hidden sm:inline">Tập trước</span>
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-1 rounded-xl border border-white/10 bg-white/5 px-3 py-1.5 font-medium transition hover:border-primary/40 disabled:opacity-30 disabled:cursor-not-allowed"
          :disabled="episodeNumber >= totalEpisodes"
          @click="emit('next-episode')"
        >
          <span class="hidden sm:inline">Tập sau</span>
          <ChevronRight class="size-3.5" />
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl px-2.5 py-1.5 transition"
          :class="
            autoNext ? 'text-primary' : 'text-muted-foreground hover:text-white'
          "
          @click="emit('toggle-auto-next')"
        >
          <span
            class="size-2 rounded-full"
            :class="autoNext ? 'bg-primary animate-pulse' : 'bg-white/30'"
          />
          <span>Tự chuyển tập</span>
        </button>
      </div>

      <!-- Right: Watch Party, Bookmark, Report, Share, Theater -->
      <div class="flex items-center gap-1.5 sm:gap-2">
        <!-- Watch Party shortcut -->
        <NuxtLink
          :to="{
            path: '/party',
            query: { movie: slug, episode: episodeNumber },
          }"
          class="inline-flex items-center gap-1.5 rounded-xl border border-rose-500/30 bg-rose-500/10 px-3 py-1.5 font-semibold text-rose-300 transition hover:bg-rose-500/20"
        >
          <Users class="size-3.5" />
          <span>Xem chung</span>
        </NuxtLink>

        <!-- Save to watchlist -->
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl border px-3 py-1.5 font-medium transition"
          :class="
            isSaved
              ? 'border-primary/40 bg-primary/10 text-primary'
              : 'border-white/10 bg-white/5 text-zinc-300 hover:border-primary/40 hover:text-white'
          "
          @click="emit('toggle-saved')"
        >
          <Bookmark class="size-3.5" :class="isSaved ? 'fill-current' : ''" />
          <span class="hidden sm:inline">{{
            isSaved ? "Đã lưu" : "Lưu phim"
          }}</span>
        </button>

        <!-- Report Issue -->
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl border border-amber-500/30 bg-amber-500/10 px-3 py-1.5 font-medium text-amber-300 transition hover:bg-amber-500/20"
          @click="emit('open-report')"
        >
          <AlertTriangle class="size-3.5" />
          <span>Báo lỗi</span>
        </button>

        <!-- Share -->
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl border border-white/10 bg-white/5 px-3 py-1.5 font-medium text-zinc-300 transition hover:border-primary/40 hover:text-white"
          @click="handleShare"
        >
          <component
            :is="isCopied ? Check : Share2"
            class="size-3.5 text-primary"
          />
          <span class="hidden sm:inline">{{
            isCopied ? "Đã chép!" : "Chia sẻ"
          }}</span>
        </button>

        <!-- Theater / Lightbulb -->
        <button
          type="button"
          class="grid size-8 place-items-center rounded-xl border border-white/10 bg-white/5 text-zinc-300 transition hover:border-primary/40 hover:text-primary"
          :title="isTheater ? 'Bật sáng' : 'Tắt đèn rạp chiếu'"
          :aria-label="isTheater ? 'Bật sáng' : 'Tắt đèn rạp chiếu'"
          @click="emit('toggle-theater')"
        >
          <Lightbulb
            class="size-3.5"
            :class="isTheater ? 'text-primary fill-current' : ''"
          />
        </button>
      </div>
    </div>
  </div>
</template>
