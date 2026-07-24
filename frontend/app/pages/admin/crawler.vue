<script setup lang="ts">
type CrawlerStatus = {
  isRunning: boolean;
  cancelRequested: boolean;
  startPage: number;
  endPage: number | null;
  includeEpisodes: boolean;
  currentPage: number;
  totalPages: number;
  titlesImported: number;
  episodesImported: number;
  message: string;
  error: string | null;
  startedAt: string | null;
  finishedAt: string | null;
};

const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const { $api } = useNuxtApp();
const startPage = ref(1);
const endPage = ref("");
const includeEpisodes = ref(true);
const status = ref<CrawlerStatus | null>(null);
const errorMessage = ref("");
const isSubmitting = ref(false);
let pollTimer: ReturnType<typeof setInterval> | undefined;

useZMovieSeo({
  title: "Crawler OPhim",
  description:
    "Công cụ crawl catalog và episode OPhim trực tiếp vào database ZMovie.",
});

const progress = computed(() => {
  if (!status.value?.totalPages) return 0;
  return Math.min(
    100,
    Math.round((status.value.currentPage / status.value.totalPages) * 100),
  );
});

const isRunning = computed(() => status.value?.isRunning ?? false);

async function refreshStatus() {
  try {
    status.value = await $api<CrawlerStatus>("/v1/admin/crawler/status");
    errorMessage.value = "";
  } catch {
    errorMessage.value =
      "Không kết nối được crawler API. Hãy chạy backend ở Development.";
  }
}

async function startCrawler() {
  if (isRunning.value || isSubmitting.value) return;
  const start = Math.max(1, Number(startPage.value) || 1);
  const end = endPage.value.trim()
    ? Math.max(start, Number(endPage.value))
    : null;
  isSubmitting.value = true;
  errorMessage.value = "";
  try {
    await $api("/v1/admin/crawler/start", {
      method: "POST",
      body: {
        startPage: start,
        endPage: end,
        includeEpisodes: includeEpisodes.value,
      },
    });
    await refreshStatus();
  } catch (error: unknown) {
    errorMessage.value =
      error instanceof Error ? error.message : "Không thể bắt đầu crawler.";
  } finally {
    isSubmitting.value = false;
  }
}

async function stopCrawler() {
  if (!isRunning.value) return;
  await $api("/v1/admin/crawler/stop", { method: "POST" }).catch(
    () => undefined,
  );
  await refreshStatus();
}

onMounted(async () => {
  await refreshStatus();
  pollTimer = setInterval(() => void refreshStatus(), 1500);
});

onBeforeUnmount(() => {
  if (pollTimer) clearInterval(pollTimer);
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />

    <section class="mx-auto max-w-360 px-5 pb-20 pt-12 lg:px-12 lg:pt-16">
      <div class="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p
            class="text-sm font-semibold uppercase tracking-[.18em] text-primary"
          >
            ZMovie tools
          </p>
          <h1 class="mt-2 font-display text-4xl font-extrabold tracking-tight">
            OPhim Crawler
          </h1>
          <p class="mt-3 max-w-2xl text-sm text-muted-foreground">
            Crawl catalog và episode trực tiếp vào database. Chạy background nên
            có thể đóng terminal, chỉ cần giữ backend hoạt động.
          </p>
        </div>
        <span
          class="rounded-full border border-primary/30 bg-primary/10 px-4 py-2 text-xs font-semibold text-primary"
        >
          {{ isRunning ? "Đang chạy" : "Sẵn sàng" }}
        </span>
      </div>

      <div class="mt-10 grid gap-6 lg:grid-cols-[.8fr_1.2fr]">
        <section
          class="rounded-3xl border border-white/10 bg-surface-container p-6 lg:p-7"
        >
          <h2 class="font-display text-xl font-bold">Cấu hình crawl</h2>
          <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-1">
            <label class="grid gap-2 text-sm font-semibold">
              Page bắt đầu
              <input
                v-model.number="startPage"
                type="number"
                min="1"
                class="h-12 rounded-2xl border border-border bg-input px-4 text-foreground outline-none transition focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </label>
            <label class="grid gap-2 text-sm font-semibold">
              Page kết thúc
              <span class="font-normal text-muted-foreground"
                >(để trống = hết)</span
              >
              <input
                v-model="endPage"
                type="number"
                min="1"
                placeholder="Ví dụ: 1502"
                class="h-12 rounded-2xl border border-border bg-input px-4 text-foreground outline-none transition focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </label>
          </div>

          <label
            class="mt-5 flex cursor-pointer items-center gap-3 rounded-2xl border border-border bg-input p-4 text-sm font-semibold"
          >
            <input
              v-model="includeEpisodes"
              type="checkbox"
              class="size-4 accent-primary"
            />
            Crawl cả episode / HLS
          </label>

          <div class="mt-6 flex gap-3">
            <button
              :disabled="isRunning || isSubmitting"
              class="h-12 flex-1 rounded-2xl bg-primary px-5 text-sm font-extrabold text-white transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
              @click="startCrawler"
            >
              {{ isSubmitting ? "Đang mở…" : "Bắt đầu crawl" }}
            </button>
            <button
              :disabled="!isRunning"
              class="h-12 rounded-2xl border border-destructive/40 px-5 text-sm font-bold text-destructive transition hover:bg-destructive/10 disabled:cursor-not-allowed disabled:opacity-40"
              @click="stopCrawler"
            >
              Dừng
            </button>
          </div>
        </section>

        <section
          class="rounded-3xl border border-white/10 bg-surface-container p-6 lg:p-7"
        >
          <div class="flex items-center justify-between gap-4">
            <h2 class="font-display text-xl font-bold">Tiến trình</h2>
            <span
              v-if="status?.totalPages"
              class="text-sm font-bold text-primary"
              >{{ progress }}%</span
            >
          </div>

          <div
            class="mt-6 h-3 overflow-hidden rounded-full bg-surface-container-lowest"
          >
            <div
              class="h-full rounded-full bg-primary transition-all duration-500"
              :style="{ width: `${progress}%` }"
            />
          </div>

          <div class="mt-7 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div class="rounded-2xl bg-surface-container-lowest p-4">
              <p class="text-xs text-muted-foreground">Page</p>
              <p class="mt-1 text-xl font-extrabold">
                {{ status?.currentPage ?? 0
                }}<span class="text-sm text-muted-foreground"
                  >/{{ status?.totalPages || "—" }}</span
                >
              </p>
            </div>
            <div class="rounded-2xl bg-surface-container-lowest p-4">
              <p class="text-xs text-muted-foreground">Phim</p>
              <p class="mt-1 text-xl font-extrabold">
                {{ (status?.titlesImported ?? 0).toLocaleString("vi-VN") }}
              </p>
            </div>
            <div class="rounded-2xl bg-surface-container-lowest p-4">
              <p class="text-xs text-muted-foreground">Episodes</p>
              <p class="mt-1 text-xl font-extrabold">
                {{ (status?.episodesImported ?? 0).toLocaleString("vi-VN") }}
              </p>
            </div>
            <div class="rounded-2xl bg-surface-container-lowest p-4">
              <p class="text-xs text-muted-foreground">Mode</p>
              <p class="mt-1 text-sm font-extrabold">
                {{ status?.includeEpisodes ? "Full" : "Catalog" }}
              </p>
            </div>
          </div>

          <div
            class="mt-6 rounded-2xl border border-border bg-input px-4 py-4 text-sm"
          >
            <p
              class="font-semibold"
              :class="status?.error ? 'text-destructive' : 'text-foreground'"
            >
              {{ status?.error || status?.message || "Chưa có phiên crawl." }}
            </p>
            <p
              v-if="status?.startedAt"
              class="mt-1 text-xs text-muted-foreground"
            >
              Bắt đầu: {{ new Date(status.startedAt).toLocaleString("vi-VN") }}
            </p>
          </div>
          <p
            v-if="errorMessage"
            class="mt-4 rounded-2xl bg-destructive/10 px-4 py-3 text-sm text-destructive"
          >
            {{ errorMessage }}
          </p>
        </section>
      </div>
    </section>
  </main>
</template>
