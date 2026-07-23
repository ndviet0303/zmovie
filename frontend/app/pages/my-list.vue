<script setup lang="ts">
import { BookmarkPlus, Clock3, Plus, Play } from "@lucide/vue";

useHead({ title: "Khu vực cá nhân — ZMovie" });

type Title = {
  slug: string;
  title: string;
  genre: string;
  year: number;
  posterUrl: string;
  runtimeMinutes: number;
};
type History = {
  title: Title;
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: string;
};
type Library = { saved: Title[]; history: History[] };
type Tab = "saved" | "history";

const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const library = ref<Library | null>(null);
const loading = ref(true);
const loadError = ref("");
const activeTab = ref<Tab>("saved");
const items = computed(() =>
  activeTab.value === "saved"
    ? (library.value?.saved ?? [])
    : (library.value?.history ?? []),
);

function progress(item: History) {
  return Math.min(
    100,
    Math.round(
      (item.progressSeconds / Math.max(item.title.runtimeMinutes * 60, 1)) *
        100,
    ),
  );
}
function retryLoad() {
  window.location.reload();
}

onMounted(async () => {
  try {
    library.value = await $fetch<Library>("/api/v1/me/library", {
      credentials: "include",
      query: { locale: locale.value },
    });
  } catch (error: unknown) {
    const fetchError = error as {
      status?: number;
      statusCode?: number;
      response?: { status?: number };
    };
    const status =
      fetchError.status ?? fetchError.statusCode ?? fetchError.response?.status;
    if (status === 401) await navigateTo("/login");
    else
      loadError.value =
        status === 404
          ? "Chức năng thư viện chưa có trên API đang chạy. Hãy khởi động lại API để nhận bản cập nhật mới."
          : "Không thể tải thư viện của bạn lúc này. Vui lòng thử lại.";
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section class="mx-auto max-w-360 px-5 pb-20 pt-10 lg:px-12">
      <h1
        class="font-display text-3xl font-semibold tracking-[-.03em] text-foreground lg:text-4xl"
      >
        Khu vực cá nhân
      </h1>
      <div class="mt-5 flex items-center gap-6 border-b border-white/8">
        <button
          class="relative pb-4 text-xs font-semibold transition"
          :class="
            activeTab === 'saved'
              ? 'text-primary'
              : 'text-muted-foreground hover:text-foreground'
          "
          @click="activeTab = 'saved'"
        >
          Danh sách của tôi<span
            v-if="library?.saved.length"
            class="ml-1.5 text-[10px] opacity-70"
            >{{ library.saved.length }}</span
          ><span
            v-if="activeTab === 'saved'"
            class="absolute inset-x-0 -bottom-px h-px bg-primary"
          />
        </button>
        <button
          class="relative pb-4 text-xs font-semibold transition"
          :class="
            activeTab === 'history'
              ? 'text-primary'
              : 'text-muted-foreground hover:text-foreground'
          "
          @click="activeTab = 'history'"
        >
          Lịch sử xem<span
            v-if="library?.history.length"
            class="ml-1.5 text-[10px] opacity-70"
            >{{ library.history.length }}</span
          ><span
            v-if="activeTab === 'history'"
            class="absolute inset-x-0 -bottom-px h-px bg-primary"
          />
        </button>
      </div>

      <div
        v-if="loading"
        class="mt-6 min-h-[21rem] rounded-3xl border border-white/6 bg-[#181717] p-5 shadow-[0_20px_55px_rgba(0,0,0,.16)]"
      >
        <div class="flex gap-4">
          <div
            v-for="index in 4"
            :key="index"
            class="h-48 w-32 animate-pulse rounded-2xl bg-surface-container"
          />
        </div>
      </div>
      <div
        v-else-if="loadError"
        class="mt-5 rounded-3xl border border-destructive/30 bg-destructive/8 p-7 text-center"
      >
        <p class="text-sm text-foreground">{{ loadError }}</p>
        <button
          class="mt-4 text-sm font-semibold text-primary transition hover:text-primary-container"
          @click="retryLoad"
        >
          Thử lại
        </button>
      </div>
      <section
        v-else
        class="mt-6 min-h-[21rem] rounded-3xl border border-white/6 bg-[#181717] p-5 shadow-[0_20px_55px_rgba(0,0,0,.16)] sm:p-6"
      >
        <div v-if="items.length" class="flex flex-wrap gap-4 sm:gap-5">
          <NuxtLink
            v-for="item in items"
            :key="
              activeTab === 'saved'
                ? (item as Title).slug
                : (item as History).title.slug
            "
            :to="
              activeTab === 'saved'
                ? `/movies/${(item as Title).slug}`
                : {
                    path: `/watch/${(item as History).title.slug}`,
                    query: (item as History).episodeNumber
                      ? { episode: (item as History).episodeNumber }
                      : {},
                  }
            "
            class="group relative h-48 w-32 shrink-0 overflow-hidden rounded-2xl border border-white/8 bg-surface-container transition hover:-translate-y-1 hover:border-primary/50 sm:h-52 sm:w-36"
          >
            <img
              :src="
                activeTab === 'saved'
                  ? (item as Title).posterUrl
                  : (item as History).title.posterUrl
              "
              :alt="
                activeTab === 'saved'
                  ? (item as Title).title
                  : (item as History).title.title
              "
              class="size-full object-cover opacity-80 transition duration-300 group-hover:scale-105 group-hover:opacity-100"
            />
            <span
              class="absolute inset-0 bg-gradient-to-t from-black via-black/10 to-transparent"
            />
            <span
              v-if="activeTab === 'history'"
              class="absolute inset-x-0 bottom-0 h-1 bg-black/50"
              ><span
                class="block h-full bg-primary"
                :style="{ width: `${progress(item as History)}%` }"
            /></span>
            <span
              v-if="activeTab === 'history'"
              class="absolute right-2 top-2 grid size-7 place-items-center rounded-full bg-primary-container text-primary-container-foreground"
              ><Play class="size-3 fill-current"
            /></span>
            <span class="absolute inset-x-0 bottom-0 p-2.5"
              ><b class="block truncate text-[11px] font-semibold text-white">{{
                activeTab === "saved"
                  ? (item as Title).title
                  : (item as History).title.title
              }}</b
              ><small
                class="mt-0.5 block truncate text-[9px] font-medium text-white/70"
                >{{
                  activeTab === "saved"
                    ? `${(item as Title).genre} · ${(item as Title).year}`
                    : `${progress(item as History)}% đã xem`
                }}</small
              ></span
            >
          </NuxtLink>
          <NuxtLink
            to="/browse"
            class="grid h-48 w-32 shrink-0 place-items-center rounded-2xl border border-dashed border-white/15 text-center text-muted-foreground transition hover:border-primary/50 hover:text-primary sm:h-52 sm:w-36"
            ><span
              ><Plus class="mx-auto size-6" /><span
                class="mt-2 block text-[10px] font-semibold"
                >Khám phá thêm</span
              ></span
            ></NuxtLink
          >
        </div>
        <div v-else class="grid min-h-[18rem] place-items-center text-center">
          <div
            class="rounded-2xl border border-dashed border-white/12 bg-black/10 px-12 py-8"
          >
            <component
              :is="activeTab === 'saved' ? BookmarkPlus : Clock3"
              class="mx-auto size-7 text-primary"
            />
            <p class="mt-4 text-sm font-medium text-foreground">
              {{
                activeTab === "saved"
                  ? "Danh sách của bạn đang trống."
                  : "Bạn chưa có lịch sử xem."
              }}
            </p>
            <p class="mt-1 text-xs text-muted-foreground">
              {{
                activeTab === "saved"
                  ? "Lưu những phim bạn muốn xem sau."
                  : "Bắt đầu xem để theo dõi tiến độ tại đây."
              }}
            </p>
            <NuxtLink
              to="/browse"
              class="mt-5 inline-flex rounded-full border border-primary/40 px-4 py-2 text-xs font-semibold text-primary transition hover:bg-primary-container hover:text-primary-container-foreground"
              >Khám phá phim <span class="ml-1">→</span></NuxtLink
            >
          </div>
        </div>
      </section>
    </section>
  </main>
</template>
