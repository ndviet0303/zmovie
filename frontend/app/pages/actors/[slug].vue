<script setup lang="ts">
import { ArrowLeft, Clapperboard, UserRound } from "@lucide/vue";

const { locale, person, pending, error, refresh } = await usePersonDetail();
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section class="mx-auto max-w-360 px-5 pb-20 pt-8 lg:px-12">
      <NuxtLink
        to="/actors"
        class="inline-flex items-center gap-2 text-sm text-muted-foreground transition hover:text-primary"
      >
        <ArrowLeft class="size-4" /> Danh bạ nghệ sĩ
      </NuxtLink>

      <div
        v-if="pending"
        class="mt-8 h-56 animate-pulse rounded-3xl bg-white/5"
      />
      <div
        v-else-if="error || !person"
        class="mt-8 rounded-3xl border border-destructive/30 bg-destructive/10 p-10 text-center"
      >
        <p>Không tìm thấy hồ sơ nghệ sĩ.</p>
        <button
          class="mt-4 text-sm font-semibold text-primary"
          @click="refresh"
        >
          Thử lại
        </button>
      </div>
      <template v-else>
        <header
          class="mt-8 flex flex-col items-center gap-6 rounded-3xl border border-white/8 bg-[#171717] p-8 text-center sm:flex-row sm:text-left"
        >
          <span
            class="grid size-28 shrink-0 place-items-center rounded-full bg-gradient-to-br from-primary/35 to-primary/5 text-primary"
          >
            <UserRound class="size-12" />
          </span>
          <div>
            <p
              class="text-xs font-bold uppercase tracking-[.18em] text-primary"
            >
              Hồ sơ nghệ sĩ
            </p>
            <h1
              class="mt-2 font-display text-3xl font-bold tracking-tight lg:text-4xl"
            >
              {{ person.name }}
            </h1>
            <div
              class="mt-3 flex flex-wrap justify-center gap-2 sm:justify-start"
            >
              <span
                v-for="role in person.roles"
                :key="role"
                class="rounded-full bg-white/7 px-3 py-1 text-xs text-muted-foreground"
                >{{ role }}</span
              >
            </div>
          </div>
        </header>

        <section class="mt-10">
          <div class="mb-5 flex items-center gap-2">
            <Clapperboard class="size-5 text-primary" />
            <h2 class="font-display text-xl font-bold">Phim đã tham gia</h2>
            <span class="text-xs text-muted-foreground"
              >({{ person.titles.length }})</span
            >
          </div>
          <div
            class="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6"
          >
            <MovieCard
              v-for="title in person.titles"
              :key="title.slug"
              :title="title"
            />
          </div>
        </section>
      </template>
    </section>
  </main>
</template>
