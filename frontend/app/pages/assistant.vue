<script setup lang="ts">
import { Bot, LoaderCircle, Send, Sparkles } from "@lucide/vue";

const {
  locale,
  copy,
  prompt,
  isSending,
  messages,
  send,
  recordFeedback,
  setLocale,
} = useAssistant();
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" @locale-change="setLocale" />
    <section
      class="mx-auto flex min-h-[calc(100vh-77px)] max-w-5xl flex-col px-5 py-10 lg:px-12"
    >
      <header class="mx-auto w-full max-w-3xl text-center">
        <div
          class="mx-auto grid size-12 place-items-center rounded-2xl bg-primary-container text-primary-container-foreground"
        >
          <Bot class="size-6" />
        </div>
        <h1 class="mt-4 font-display text-4xl font-semibold tracking-tight">
          {{ copy.title }}
        </h1>
        <p class="mt-2 text-muted-foreground">{{ copy.subtitle }}</p>
      </header>

      <div
        class="mx-auto mt-8 flex w-full max-w-3xl flex-1 flex-col rounded-3xl border border-white/10 bg-surface-container shadow-[0_20px_55px_rgba(0,0,0,.16)]"
      >
        <div class="flex-1 space-y-5 overflow-y-auto p-5 sm:p-7">
          <article
            v-for="(message, index) in messages"
            :key="index"
            class="flex gap-3"
            :class="message.role === 'user' ? 'flex-row-reverse' : ''"
          >
            <div
              v-if="message.role === 'bot'"
              class="grid size-8 shrink-0 place-items-center rounded-xl bg-primary-container text-primary-container-foreground"
            >
              <Sparkles class="size-4" />
            </div>
            <div
              class="max-w-[82%] rounded-2xl px-4 py-3 text-sm leading-6"
              :class="
                message.role === 'user'
                  ? 'bg-primary-container text-primary-container-foreground'
                  : 'bg-surface-container-high text-foreground'
              "
            >
              <p>{{ message.text }}</p>
              <div
                v-if="message.suggestions?.length"
                class="mt-4 grid gap-2 sm:grid-cols-3"
              >
                <NuxtLink
                  v-for="title in message.suggestions"
                  :key="title.slug"
                  :to="`/movies/${title.slug}`"
                  class="overflow-hidden rounded-xl border border-white/10 bg-black/20 transition hover:border-primary/60"
                  @click="recordFeedback(message, title.slug)"
                >
                  <img
                    :src="title.posterUrl"
                    :alt="title.title"
                    class="aspect-video w-full object-cover"
                    loading="lazy"
                  />
                  <span
                    class="block truncate px-2 py-2 text-xs font-semibold"
                    >{{ title.title }}</span
                  >
                </NuxtLink>
              </div>
            </div>
          </article>
          <div
            v-if="isSending"
            class="flex items-center gap-2 text-xs text-muted-foreground"
          >
            <LoaderCircle class="size-4 animate-spin" />{{
              locale === "vi" ? "Đang tìm phim…" : "Finding titles…"
            }}
          </div>
        </div>
        <div class="border-t border-white/10 p-4 sm:p-5">
          <div class="mb-3 flex flex-wrap gap-2">
            <button
              v-for="suggestion in copy.suggestions"
              :key="suggestion"
              class="rounded-full border border-white/10 px-3 py-1.5 text-xs text-muted-foreground transition hover:border-primary/50 hover:text-primary"
              @click="send(suggestion)"
            >
              {{ suggestion }}
            </button>
          </div>
          <form class="flex gap-3" @submit.prevent="send()">
            <input
              v-model="prompt"
              :placeholder="copy.placeholder"
              class="min-w-0 flex-1 rounded-2xl border border-white/10 bg-background px-4 py-3 text-sm outline-none transition placeholder:text-muted-foreground focus:border-primary"
              :disabled="isSending"
            /><button
              class="grid size-11 shrink-0 place-items-center rounded-2xl bg-primary-container text-primary-container-foreground transition hover:bg-primary disabled:opacity-50"
              :aria-label="copy.send"
              :disabled="isSending || !prompt.trim()"
            >
              <Send class="size-4" />
            </button>
          </form>
        </div>
      </div>
    </section>
  </main>
</template>
