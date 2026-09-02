<script setup lang="ts">
import { Bell, CheckCheck, Film, Sparkles } from "@lucide/vue";
import { onMounted, ref } from "vue";

interface AppNotification {
  id: string;
  title: string;
  message: string;
  slug: string;
  episodeNumber?: number;
  timeAgo: string;
  isRead: boolean;
  type: "episode" | "system" | "vip";
}

const isOpen = ref(false);
const notifications = ref<AppNotification[]>([
  {
    id: "notif-1",
    title: "Phim mới cập nhật",
    message: "Sintel (4K Ultra HD) đã sẵn sàng trên máy chủ Cloudflare R2!",
    slug: "sintel",
    episodeNumber: 1,
    timeAgo: "10 phút trước",
    isRead: false,
    type: "episode",
  },
  {
    id: "notif-2",
    title: "Tập mới ra mắt",
    message: "Tears of Steel vừa cập nhật phụ đề tiếng Việt chuẩn phòng thu.",
    slug: "tears-of-steel",
    episodeNumber: 1,
    timeAgo: "1 giờ trước",
    isRead: false,
    type: "episode",
  },
  {
    id: "notif-3",
    title: "Tính năng mới",
    message: "Watch Party & Danmaku trực tiếp đã được kích hoạt trên ZMovie!",
    slug: "big-buck-bunny",
    episodeNumber: 1,
    timeAgo: "Hôm qua",
    isRead: true,
    type: "system",
  },
]);

const unreadCount = computed(
  () => notifications.value.filter((n) => !n.isRead).length,
);

function markAllAsRead() {
  notifications.value.forEach((n) => (n.isRead = true));
}

function handleNotificationClick(item: AppNotification) {
  item.isRead = true;
  isOpen.value = false;
  if (item.slug) {
    void navigateTo(`/watch/${item.slug}?episode=${item.episodeNumber || 1}`);
  }
}
</script>

<template>
  <div class="relative z-[60]">
    <button
      class="relative grid size-10 place-items-center rounded-xl border border-white/10 bg-surface-container text-foreground/80 shadow-sm transition hover:border-primary/60 hover:bg-primary/10 hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      type="button"
      aria-label="Thông báo"
      title="Thông báo tập mới"
      :aria-expanded="isOpen"
      @click="isOpen = !isOpen"
    >
      <Bell class="size-[18px]" />
      <span
        v-if="unreadCount > 0"
        class="absolute right-2 top-2 size-2 rounded-full bg-rose-500 ring-2 ring-background animate-pulse"
      />
    </button>

    <!-- Notifications Popover -->
    <transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0 translate-y-2 scale-95"
      enter-to-class="opacity-100 translate-y-0 scale-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100 translate-y-0 scale-100"
      leave-to-class="opacity-0 translate-y-2 scale-95"
    >
      <div
        v-if="isOpen"
        class="absolute right-0 top-[calc(100%+8px)] z-[70] w-80 sm:w-96 overflow-hidden rounded-3xl border border-white/10 bg-surface-container-lowest/95 p-2 shadow-[0_20px_50px_rgba(0,0,0,.7)] backdrop-blur-2xl"
      >
        <!-- Header -->
        <div
          class="flex items-center justify-between border-b border-white/8 px-4 py-3"
        >
          <div class="flex items-center gap-2">
            <h3 class="text-xs font-semibold text-foreground">Thông báo</h3>
            <span
              v-if="unreadCount > 0"
              class="rounded-full bg-rose-500/20 px-2 py-0.5 text-[10px] font-semibold text-rose-400 border border-rose-500/30"
            >
              {{ unreadCount }} mới
            </span>
          </div>
          <button
            v-if="unreadCount > 0"
            class="flex items-center gap-1 text-[11px] font-medium text-muted-foreground transition hover:text-primary"
            @click="markAllAsRead"
          >
            <CheckCheck class="size-3.5" />
            <span>Đã đọc tất cả</span>
          </button>
        </div>

        <!-- List of notifications -->
        <div
          class="max-h-80 overflow-y-auto divide-y divide-white/5 p-1 text-xs"
        >
          <div
            v-for="item in notifications"
            :key="item.id"
            class="group flex cursor-pointer items-start gap-3 rounded-2xl p-3 transition hover:bg-surface-container"
            :class="item.isRead ? 'opacity-70' : 'bg-surface-container/40'"
            @click="handleNotificationClick(item)"
          >
            <div
              class="mt-0.5 grid size-7 shrink-0 place-items-center rounded-xl bg-primary/15 text-primary border border-primary/20"
            >
              <Film v-if="item.type === 'episode'" class="size-3.5" />
              <Sparkles v-else class="size-3.5" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="flex items-center justify-between gap-1">
                <p
                  class="truncate font-semibold text-foreground group-hover:text-primary transition-colors text-[11px]"
                >
                  {{ item.title }}
                </p>
                <span class="text-[9px] text-muted-foreground shrink-0">{{
                  item.timeAgo
                }}</span>
              </div>
              <p class="mt-1 text-[11px] text-muted-foreground leading-relaxed">
                {{ item.message }}
              </p>
            </div>
            <span
              v-if="!item.isRead"
              class="mt-2 size-1.5 shrink-0 rounded-full bg-rose-500"
            />
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>
