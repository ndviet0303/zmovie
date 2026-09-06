<script setup lang="ts">
import {
  Check,
  Copy,
  Maximize2,
  MessageCircle,
  Send,
  Users,
} from "@lucide/vue";
import Hls from "hls.js";
import {
  computed,
  nextTick,
  onBeforeUnmount,
  onMounted,
  ref,
  watch,
} from "vue";
import type { DanmakuItem } from "~/components/DanmakuCanvas.vue";
import {
  fetchCatalogPlayback,
  fetchCatalogTitleBySlug,
} from "~/services/catalog.service";
import type { PlaybackResponse, TitleDetail } from "~/types/catalog";

const route = useRoute();
const roomId = computed(() => String(route.params.roomId || "demo-room"));
const movieSlug = computed(() => String(route.query.movie || "sintel"));
const episodeNum = computed(() => Number(route.query.episode || 1));

const {
  activeUsers,
  chatMessages,
  connect,
  disconnect,
  syncPlay,
  syncPause,
  syncSeek,
  sendChat,
  sendDanmaku,
  onSyncPlayback,
  onDanmaku,
} = useWatchParty();

const video = ref<HTMLVideoElement | null>(null);
const playerContainer = ref<HTMLElement | null>(null);
const title = ref<TitleDetail | null>(null);
const playback = ref<PlaybackResponse | null>(null);
const isPlaying = ref(false);
const currentTime = ref(0);
const duration = ref(0);
const chatInput = ref("");
const danmakuInput = ref("");
const selectedColor = ref("#ffffff");
const isCopied = ref(false);
const chatScroll = ref<HTMLElement | null>(null);
const partyDanmakus = ref<DanmakuItem[]>([]);
const isDanmakuEnabled = ref(true);
const hudNotice = ref("");
let hudTimeout: number | null = null;
let isRemoteAction = false;
let hls: Hls | null = null;

const colors = [
  "#ffffff",
  "#f59e0b",
  "#38bdf8",
  "#4ade80",
  "#f43f5e",
  "#a855f7",
];

const quickReactions = [
  { emoji: "🔥", text: "Cháy quá anh em ơi!" },
  { emoji: "👏", text: "Đỉnh thật sự!" },
  { emoji: "🤣", text: "Hài hước vãi kkk" },
  { emoji: "😱", text: "Bất ngờ chưa!" },
  { emoji: "❤️", text: "Phim hay quá!" },
];

function showHud(text: string) {
  hudNotice.value = text;
  if (hudTimeout) clearTimeout(hudTimeout);
  hudTimeout = window.setTimeout(() => {
    hudNotice.value = "";
  }, 2000);
}

async function initRoom() {
  try {
    const [titleRes, playbackRes] = await Promise.all([
      fetchCatalogTitleBySlug(movieSlug.value, "vi"),
      fetchCatalogPlayback(movieSlug.value, "vi"),
    ]);
    title.value = titleRes;
    playback.value = playbackRes;

    const currentEpisode =
      playbackRes.episodes.find((e) => e.number === episodeNum.value) ||
      playbackRes.episodes[0];

    if (currentEpisode && video.value) {
      if (Hls.isSupported() && currentEpisode.hlsUrl.includes(".m3u8")) {
        hls = new Hls({ enableWorker: true });
        hls.loadSource(currentEpisode.hlsUrl);
        hls.attachMedia(video.value);
      } else {
        video.value.src = currentEpisode.hlsUrl;
      }
    }
  } catch (err) {
    console.error("[WatchParty:initRoom]", err);
  }

  const defaultUsername = `Khách_${Math.floor(1000 + Math.random() * 9000)}`;
  await connect(
    roomId.value,
    defaultUsername,
    movieSlug.value,
    episodeNum.value,
  );
}

onSyncPlayback((action, targetTime) => {
  if (!video.value) return;
  isRemoteAction = true;

  if (action === "seek") {
    video.value.currentTime = targetTime;
    showHud(`Tua đến ${formatTime(targetTime)}`);
  } else if (action === "play") {
    if (Math.abs(video.value.currentTime - targetTime) > 1.2) {
      video.value.currentTime = targetTime;
    }
    void video.value.play();
    isPlaying.value = true;
    showHud("Bạn cùng phòng đã bấm Phát");
  } else if (action === "pause") {
    video.value.currentTime = targetTime;
    video.value.pause();
    isPlaying.value = false;
    showHud("Bạn cùng phòng đã Tạm dừng");
  }

  window.setTimeout(() => {
    isRemoteAction = false;
  }, 500);
});

onDanmaku((item) => {
  partyDanmakus.value.push(item);
});

function handlePlay() {
  if (!video.value || isRemoteAction) return;
  isPlaying.value = true;
  void syncPlay(video.value.currentTime);
}

function handlePause() {
  if (!video.value || isRemoteAction) return;
  isPlaying.value = false;
  void syncPause(video.value.currentTime);
}

function handleSeeked() {
  if (!video.value || isRemoteAction) return;
  void syncSeek(video.value.currentTime);
}

function handleTimeUpdate() {
  if (!video.value) return;
  currentTime.value = video.value.currentTime;
  duration.value = video.value.duration || 0;
}

function handleSendChat() {
  if (!chatInput.value.trim()) return;
  void sendChat(chatInput.value.trim());
  chatInput.value = "";
  scrollToChatBottom();
}

function handleSendDanmaku(customText?: string) {
  const content = customText || danmakuInput.value;
  if (!content.trim()) return;

  const newDanmaku: DanmakuItem = {
    id: `${Date.now()}-${Math.random()}`,
    timeSeconds: currentTime.value,
    content: content.trim(),
    color: selectedColor.value,
  };
  partyDanmakus.value.push(newDanmaku);
  void sendDanmaku(
    "ep-1",
    currentTime.value,
    content.trim(),
    selectedColor.value,
  );

  if (!customText) {
    danmakuInput.value = "";
  }
}

function copyInviteLink() {
  if (import.meta.client) {
    void navigator.clipboard.writeText(window.location.href);
    isCopied.value = true;
    window.setTimeout(() => {
      isCopied.value = false;
    }, 2000);
  }
}

async function toggleFullscreen() {
  if (!playerContainer.value) return;
  if (document.fullscreenElement) {
    await document.exitFullscreen();
    return;
  }
  await playerContainer.value.requestFullscreen();
}

function scrollToChatBottom() {
  nextTick(() => {
    if (chatScroll.value) {
      chatScroll.value.scrollTop = chatScroll.value.scrollHeight;
    }
  });
}

watch(
  chatMessages,
  () => {
    scrollToChatBottom();
  },
  { deep: true },
);

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? "0" : ""}${secs}`;
}

onMounted(() => {
  void initRoom();
});

onBeforeUnmount(() => {
  if (hls) {
    hls.destroy();
    hls = null;
  }
  void disconnect();
});
</script>

<template>
  <main class="min-h-screen bg-[#0d0d0e] text-foreground flex flex-col">
    <!-- Header bar -->
    <header
      class="h-14 border-b border-white/8 bg-surface-container-lowest/80 backdrop-blur-md px-5 flex items-center justify-between z-30"
    >
      <div class="flex items-center gap-3">
        <NuxtLink
          to="/"
          class="font-display text-lg font-bold text-primary flex items-center gap-2"
        >
          <span>ZMovie</span>
          <span
            class="rounded bg-rose-500/20 px-2 py-0.5 text-[10px] font-semibold text-rose-400 border border-rose-500/30"
          >
            WATCH PARTY
          </span>
        </NuxtLink>
        <span class="text-xs text-muted-foreground hidden sm:inline">|</span>
        <h1
          class="text-xs font-semibold text-foreground truncate max-w-xs hidden sm:inline"
        >
          {{ title?.title || "Phòng xem chung" }}
        </h1>
      </div>

      <div class="flex items-center gap-3 text-xs">
        <div
          class="flex items-center gap-1.5 rounded-full bg-surface-container px-3 py-1 text-muted-foreground border border-white/8"
        >
          <Users class="size-3.5 text-primary" />
          <span>{{ activeUsers.length || 1 }} đang xem</span>
        </div>

        <button
          class="flex items-center gap-1.5 rounded-xl bg-surface-container px-3 py-1.5 font-medium text-foreground transition hover:border-primary/40 border border-white/10"
          @click="copyInviteLink"
        >
          <component
            :is="isCopied ? Check : Copy"
            class="size-3.5 text-primary"
          />
          <span>{{ isCopied ? "Đã chép link!" : "Mời bạn" }}</span>
        </button>
        <button
          class="grid size-8 place-items-center rounded-xl border border-white/10 bg-surface-container text-foreground transition hover:border-primary/40"
          aria-label="Toàn màn hình"
          @click="toggleFullscreen"
        >
          <Maximize2 class="size-3.5" />
        </button>
      </div>
    </header>

    <!-- Main Party Grid -->
    <div class="flex-1 flex flex-col lg:flex-row overflow-hidden">
      <!-- Video Area -->
      <div
        class="flex-1 flex flex-col bg-black relative justify-center items-center"
      >
        <div
          ref="playerContainer"
          class="relative w-full aspect-video max-h-[82vh] flex items-center justify-center bg-black"
        >
          <video
            ref="video"
            class="size-full object-contain"
            playsinline
            controls
            @play="handlePlay"
            @pause="handlePause"
            @seeked="handleSeeked"
            @timeupdate="handleTimeUpdate"
          />

          <!-- Danmaku Canvas Layer -->
          <DanmakuCanvas
            :current-time="currentTime"
            :is-playing="isPlaying"
            :danmakus="partyDanmakus"
            :enabled="isDanmakuEnabled"
          />

          <!-- HUD Sync Notification -->
          <transition
            enter-active-class="transition duration-200 ease-out transform"
            enter-from-class="opacity-0 scale-90"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in transform"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-90"
          >
            <div
              v-if="hudNotice"
              class="pointer-events-none absolute top-6 z-40 rounded-full bg-black/80 px-4 py-2 text-xs font-semibold text-white shadow-lg backdrop-blur-md border border-white/10"
            >
              {{ hudNotice }}
            </div>
          </transition>
        </div>

        <!-- Danmaku Bar under video -->
        <div
          class="w-full bg-surface-container-lowest border-t border-white/8 px-4 py-2.5 flex items-center gap-3"
        >
          <button
            class="px-2.5 py-1 rounded-lg text-xs font-semibold border transition"
            :class="
              isDanmakuEnabled
                ? 'bg-primary/15 text-primary border-primary/40'
                : 'bg-surface-container text-muted-foreground border-white/10'
            "
            @click="isDanmakuEnabled = !isDanmakuEnabled"
          >
            Đạn mạc
          </button>

          <!-- Color circles -->
          <div class="hidden sm:flex items-center gap-1.5">
            <button
              v-for="c in colors"
              :key="c"
              class="size-4 rounded-full border border-white/20 transition hover:scale-125"
              :class="
                selectedColor === c ? 'ring-2 ring-primary scale-110' : ''
              "
              :style="{ backgroundColor: c }"
              @click="selectedColor = c"
            />
          </div>

          <!-- Danmaku Input -->
          <form
            class="flex-1 flex items-center gap-2"
            @submit.prevent="handleSendDanmaku()"
          >
            <input
              v-model="danmakuInput"
              type="text"
              placeholder="Gửi bình luận bay trên màn hình (Danmaku)..."
              class="flex-1 rounded-xl border border-white/10 bg-surface-container px-3 py-1.5 text-xs text-foreground placeholder:text-muted-foreground/60 outline-none focus:border-primary/60 transition"
            />
            <button
              type="submit"
              :disabled="!danmakuInput.trim()"
              class="rounded-xl bg-primary px-3 py-1.5 text-xs font-semibold text-primary-container-foreground transition hover:opacity-90 disabled:opacity-40"
            >
              Bắn
            </button>
          </form>

          <!-- Quick reactions -->
          <div class="hidden md:flex items-center gap-1">
            <button
              v-for="r in quickReactions"
              :key="r.emoji"
              class="rounded-lg p-1.5 text-sm transition hover:bg-white/10 hover:scale-125 active:scale-95"
              :title="r.text"
              @click="handleSendDanmaku(r.text)"
            >
              {{ r.emoji }}
            </button>
          </div>
        </div>
      </div>

      <!-- Right Chat Panel -->
      <aside
        class="w-full lg:w-80 h-72 lg:h-auto border-t lg:border-t-0 lg:border-l border-white/8 bg-surface-container-lowest flex flex-col"
      >
        <div
          class="px-4 py-3 border-b border-white/8 flex items-center justify-between"
        >
          <div class="flex items-center gap-2">
            <MessageCircle class="size-4 text-primary" />
            <h2 class="text-xs font-semibold text-foreground">
              Trò chuyện trực tiếp
            </h2>
          </div>
          <span class="text-[10px] text-muted-foreground"
            >Phòng: {{ roomId }}</span
          >
        </div>

        <!-- Chat messages list -->
        <div
          ref="chatScroll"
          class="flex-1 p-3.5 space-y-3 overflow-y-auto text-xs"
        >
          <div
            v-if="!chatMessages.length"
            class="text-center text-muted-foreground py-8 text-xs"
          >
            Chưa có tin nhắn nào. Hãy gửi lời chào đến mọi người trong phòng!
          </div>
          <div
            v-for="(msg, i) in chatMessages"
            :key="i"
            class="flex flex-col gap-0.5 rounded-xl bg-surface-container p-2.5 border border-white/5"
          >
            <div class="flex items-center justify-between">
              <span class="font-semibold text-[11px] text-primary">{{
                msg.username
              }}</span>
              <span class="text-[9px] text-muted-foreground">
                {{
                  new Date(msg.timestamp).toLocaleTimeString([], {
                    hour: "2-digit",
                    minute: "2-digit",
                  })
                }}
              </span>
            </div>
            <p class="text-foreground text-xs leading-relaxed break-words">
              {{ msg.message }}
            </p>
          </div>
        </div>

        <!-- Chat Input Form -->
        <footer class="p-3 border-t border-white/8 bg-surface-container/40">
          <form
            class="flex items-center gap-2"
            @submit.prevent="handleSendChat"
          >
            <input
              v-model="chatInput"
              type="text"
              placeholder="Nhập tin nhắn..."
              class="flex-1 rounded-xl border border-white/10 bg-surface-container-lowest px-3 py-2 text-xs text-foreground placeholder:text-muted-foreground/60 outline-none focus:border-primary/60 transition"
            />
            <button
              type="submit"
              :disabled="!chatInput.trim()"
              class="grid size-8 place-items-center rounded-xl bg-primary text-primary-container-foreground transition hover:opacity-90 disabled:opacity-40"
              aria-label="Gửi tin nhắn"
            >
              <Send class="size-3.5" />
            </button>
          </form>
        </footer>
      </aside>
    </div>
  </main>
</template>
