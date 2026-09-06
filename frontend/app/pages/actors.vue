<script setup lang="ts">
import { Search, UsersRound, X } from "@lucide/vue";

const {
  locale,
  query,
  people,
  total,
  pending,
  error,
  hasMore,
  load,
  loadMore,
} = usePeople();
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section class="mx-auto max-w-360 px-5 pb-20 pt-10 lg:px-12">
      <div
        class="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between"
      >
        <div>
          <h1
            class="font-display text-3xl font-semibold tracking-[-.03em] lg:text-4xl"
          >
            Danh bạ nghệ sĩ
          </h1>
          <p class="mt-2 text-sm text-muted-foreground">
            {{ total }} diễn viên và đạo diễn trong kho phim ZMovie
          </p>
        </div>
        <label class="relative block w-full max-w-md">
          <Search
            class="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
          />
          <input
            v-model="query"
            type="search"
            placeholder="Tìm tên nghệ sĩ..."
            class="h-11 w-full rounded-full border border-white/10 bg-white/5 pl-11 pr-11 text-sm outline-none transition focus:border-primary/60"
          />
          <button
            v-if="query"
            type="button"
            class="absolute right-4 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-white"
            aria-label="Xóa tìm kiếm"
            @click="query = ''"
          >
            <X class="size-4" />
          </button>
        </label>
      </div>

      <div
        v-if="pending && !people.length"
        class="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
      >
        <div
          v-for="index in 8"
          :key="index"
          class="h-28 animate-pulse rounded-2xl bg-white/5"
        />
      </div>
      <div
        v-else-if="error && !people.length"
        class="mt-8 rounded-2xl border border-destructive/30 bg-destructive/10 p-8 text-center"
      >
        <p class="text-sm">Không thể tải danh bạ nghệ sĩ.</p>
        <button
          class="mt-4 text-sm font-semibold text-primary"
          @click="load(true)"
        >
          Thử lại
        </button>
      </div>
      <div
        v-else-if="people.length"
        class="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
      >
        <NuxtLink
          v-for="person in people"
          :key="person.slug"
          :to="`/actors/${person.slug}`"
          class="group flex items-center gap-4 rounded-2xl border border-white/8 bg-[#171717] p-4 transition hover:-translate-y-0.5 hover:border-primary/40"
        >
          <span
            class="grid size-16 shrink-0 place-items-center rounded-full bg-gradient-to-br from-primary/35 to-primary/5 font-display text-xl font-black text-primary"
          >
            {{ person.name.slice(0, 2).toUpperCase() }}
          </span>
          <span class="min-w-0">
            <strong
              class="block truncate transition group-hover:text-primary"
              >{{ person.name }}</strong
            >
            <span class="mt-1 block truncate text-xs text-muted-foreground">{{
              person.roles.join(" · ")
            }}</span>
            <span class="mt-2 block text-[11px] text-white/40"
              >{{ person.titleCount }} phim</span
            >
          </span>
        </NuxtLink>
      </div>
      <div
        v-else
        class="mt-8 grid min-h-72 place-items-center rounded-2xl border border-dashed border-white/10 text-center text-muted-foreground"
      >
        <div>
          <UsersRound class="mx-auto mb-3 size-10" />
          <p>Không tìm thấy nghệ sĩ phù hợp.</p>
        </div>
      </div>

      <div v-if="hasMore" class="mt-8 text-center">
        <button
          class="rounded-xl bg-primary px-6 py-3 text-sm font-bold text-primary-foreground transition hover:brightness-110 disabled:opacity-50"
          :disabled="pending"
          @click="loadMore"
        >
          {{ pending ? "Đang tải..." : "Xem thêm" }}
        </button>
      </div>
    </section>
  </main>
</template>
