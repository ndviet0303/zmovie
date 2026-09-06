<script setup lang="ts">
import { Crown, Play, Plus, RefreshCw, Trash2, UsersRound } from "@lucide/vue";

const {
  locale,
  visibleRooms,
  titles,
  titleBySlug,
  ownedTokens,
  activeTab,
  selectedMovie,
  selectedEpisode,
  pending,
  creating,
  error,
  refresh,
  createRoom,
  removeRoom,
} = usePartyLobby();

const lobbyTabs = [
  { value: "latest", label: "Mới nhất" },
  { value: "popular", label: "Phổ biến" },
  { value: "mine", label: "Phòng của tôi" },
] as const;
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section class="mx-auto max-w-360 px-5 pb-20 pt-10 lg:px-12">
      <div class="grid gap-8 lg:grid-cols-[1fr_22rem]">
        <div>
          <div
            class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"
          >
            <div>
              <p
                class="mb-2 text-xs font-bold uppercase tracking-[.18em] text-primary"
              >
                Watch Party
              </p>
              <h1
                class="font-display text-3xl font-bold tracking-tight lg:text-4xl"
              >
                Xem chung cùng bạn bè
              </h1>
              <p class="mt-2 text-sm text-muted-foreground">
                Phát, dừng và tua phim đồng bộ trong phòng.
              </p>
            </div>
            <button
              class="inline-flex items-center gap-2 self-start rounded-xl border border-white/10 px-4 py-2 text-xs font-semibold transition hover:border-primary/50"
              @click="refresh"
            >
              <RefreshCw class="size-3.5" /> Làm mới
            </button>
          </div>

          <div class="mt-7 flex flex-wrap gap-2">
            <button
              v-for="tab in lobbyTabs"
              :key="tab.value"
              class="rounded-full px-4 py-2 text-xs font-bold transition"
              :class="
                activeTab === tab.value
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-white/6 text-muted-foreground hover:text-white'
              "
              @click="activeTab = tab.value"
            >
              {{ tab.label }}
            </button>
          </div>

          <div v-if="pending" class="mt-6 space-y-3">
            <div
              v-for="index in 4"
              :key="index"
              class="h-28 animate-pulse rounded-2xl bg-white/5"
            />
          </div>
          <div v-else-if="visibleRooms.length" class="mt-6 space-y-3">
            <article
              v-for="room in visibleRooms"
              :key="room.roomId"
              class="flex flex-col gap-4 rounded-2xl border border-white/8 bg-[#171717] p-4 sm:flex-row sm:items-center"
            >
              <img
                v-if="titleBySlug[room.movieSlug]?.posterUrl"
                :src="titleBySlug[room.movieSlug]?.posterUrl"
                :alt="titleBySlug[room.movieSlug]?.title"
                class="h-24 w-17 shrink-0 rounded-xl object-cover"
              />
              <div class="min-w-0 flex-1">
                <div class="flex flex-wrap items-center gap-2">
                  <h2 class="truncate font-semibold">
                    {{ titleBySlug[room.movieSlug]?.title || room.movieSlug }}
                  </h2>
                  <span
                    v-if="ownedTokens[room.roomId]"
                    class="inline-flex items-center gap-1 rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-bold text-primary"
                  >
                    <Crown class="size-3" /> Của bạn
                  </span>
                </div>
                <p class="mt-2 text-xs text-muted-foreground">
                  Tập {{ room.episodeNumber }} · Phòng {{ room.roomId }}
                </p>
                <p class="mt-2 flex items-center gap-1.5 text-xs text-white/50">
                  <UsersRound class="size-3.5" />
                  {{ room.activeUserCount }} người đang xem
                </p>
              </div>
              <div class="flex items-center gap-2">
                <NuxtLink
                  :to="{
                    path: `/party/${room.roomId}`,
                    query: {
                      movie: room.movieSlug,
                      episode: room.episodeNumber,
                    },
                  }"
                  class="inline-flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-xs font-bold text-primary-foreground"
                >
                  <Play class="size-3.5 fill-current" /> Vào phòng
                </NuxtLink>
                <button
                  v-if="ownedTokens[room.roomId]"
                  class="grid size-10 place-items-center rounded-xl border border-destructive/30 text-destructive transition hover:bg-destructive/10"
                  aria-label="Xóa phòng"
                  @click="removeRoom(room.roomId)"
                >
                  <Trash2 class="size-4" />
                </button>
              </div>
            </article>
          </div>
          <div
            v-else
            class="mt-6 grid min-h-64 place-items-center rounded-2xl border border-dashed border-white/10 text-center text-muted-foreground"
          >
            <div>
              <UsersRound class="mx-auto mb-3 size-10" />
              <p>Chưa có phòng phù hợp.</p>
            </div>
          </div>
        </div>

        <aside
          class="h-fit rounded-3xl border border-primary/20 bg-primary/[.04] p-6 lg:sticky lg:top-24"
        >
          <span
            class="grid size-11 place-items-center rounded-2xl bg-primary text-primary-foreground"
          >
            <Plus class="size-5" />
          </span>
          <h2 class="mt-4 font-display text-xl font-bold">Tạo phòng mới</h2>
          <p class="mt-2 text-xs leading-5 text-muted-foreground">
            Chọn phim và tập. Bạn sẽ nhận quyền quản lý phòng trên thiết bị này.
          </p>
          <label class="mt-5 block text-xs font-semibold text-white/70"
            >Phim</label
          >
          <select
            v-model="selectedMovie"
            class="mt-2 h-11 w-full rounded-xl border border-white/10 bg-[#191b24] px-3 text-sm outline-none focus:border-primary/60"
          >
            <option
              v-for="title in titles"
              :key="title.slug"
              :value="title.slug"
            >
              {{ title.title }}
            </option>
          </select>
          <label class="mt-4 block text-xs font-semibold text-white/70"
            >Tập</label
          >
          <input
            v-model.number="selectedEpisode"
            type="number"
            min="1"
            class="mt-2 h-11 w-full rounded-xl border border-white/10 bg-[#191b24] px-3 text-sm outline-none focus:border-primary/60"
          />
          <button
            class="mt-5 w-full rounded-xl bg-primary px-4 py-3 text-sm font-bold text-primary-foreground transition hover:brightness-110 disabled:opacity-50"
            :disabled="creating || !selectedMovie"
            @click="createRoom"
          >
            {{ creating ? "Đang tạo..." : "Tạo và vào phòng" }}
          </button>
          <p v-if="error" class="mt-3 text-xs text-destructive">
            {{ error }}
          </p>
        </aside>
      </div>
    </section>
  </main>
</template>
