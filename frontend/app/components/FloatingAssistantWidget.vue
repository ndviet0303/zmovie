<script setup lang="ts">
import {
  Bot,
  ChevronDown,
  CornerDownLeft,
  Film,
  MessageSquare,
  Sparkles,
  X,
} from "@lucide/vue";
import type { TitleSummary } from "~/types/catalog";

const {
  locale,
  messages: chatMessages,
  prompt,
  isSending,
  copy,
  send,
  recordFeedback,
} = useAssistant();

const isOpen = ref(false);
const chatContainer = ref<HTMLElement | null>(null);

const quickSuggestions = [
  "Phim hoạt hình chiếu rạp hay",
  "Phim hành động kịch tính",
  "Gợi ý phim cuối tuần thư giãn",
  "Phim kinh dị giật gân",
];

function handleQuickPrompt(text: string) {
  void send(text);
}

function scrollToBottom() {
  nextTick(() => {
    if (chatContainer.value) {
      chatContainer.value.scrollTop = chatContainer.value.scrollHeight;
    }
  });
}

watch(
  chatMessages,
  () => {
    scrollToBottom();
  },
  { deep: true },
);

function toggleWidget() {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    scrollToBottom();
  }
}
</script>

<template>
  <div class="fixed bottom-6 right-6 z-50 flex flex-col items-end">
    <!-- Chat Window -->
    <transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="opacity-0 translate-y-6 scale-95"
      enter-to-class="opacity-100 translate-y-0 scale-100"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="opacity-100 translate-y-0 scale-100"
      leave-to-class="opacity-0 translate-y-6 scale-95"
    >
      <section
        v-if="isOpen"
        class="mb-3 flex h-[34rem] w-[24rem] max-w-[calc(100vw-2rem)] flex-col overflow-hidden rounded-3xl border border-white/10 bg-surface-container-lowest/95 shadow-2xl shadow-black/80 backdrop-blur-xl sm:w-[26rem]"
        role="dialog"
        aria-label="ZMovie AI Assistant"
      >
        <!-- Header -->
        <header
          class="flex items-center justify-between border-b border-white/10 bg-surface-container/60 px-5 py-3.5"
        >
          <div class="flex items-center gap-2.5">
            <div
              class="grid size-8 place-items-center rounded-xl bg-gradient-to-tr from-primary to-amber-400 text-primary-container-foreground shadow-sm"
            >
              <Sparkles class="size-4.5" />
            </div>
            <div>
              <div class="flex items-center gap-1.5">
                <h3 class="text-xs font-semibold text-foreground">ZMovie AI</h3>
                <span
                  class="size-1.5 rounded-full bg-emerald-400 animate-pulse"
                />
              </div>
              <p class="text-[10px] text-muted-foreground">
                Trợ lý gợi ý phim thông minh
              </p>
            </div>
          </div>
          <button
            class="rounded-full p-1.5 text-muted-foreground transition hover:bg-white/10 hover:text-foreground"
            aria-label="Close assistant"
            @click="isOpen = false"
          >
            <ChevronDown class="size-4.5" />
          </button>
        </header>

        <!-- Chat messages scroll area -->
        <div
          ref="chatContainer"
          class="flex-1 space-y-4 overflow-y-auto p-4 text-xs"
        >
          <div
            v-for="(msg, idx) in chatMessages"
            :key="idx"
            class="flex gap-2.5"
            :class="msg.role === 'user' ? 'justify-end' : 'justify-start'"
          >
            <!-- Bot avatar -->
            <div
              v-if="msg.role === 'bot'"
              class="grid size-6 shrink-0 place-items-center rounded-lg bg-primary/20 text-primary"
            >
              <Bot class="size-3.5" />
            </div>

            <!-- Message bubble -->
            <div
              class="max-w-[82%] rounded-2xl p-3 leading-relaxed"
              :class="
                msg.role === 'user'
                  ? 'bg-primary text-primary-container-foreground font-medium rounded-tr-sm'
                  : 'bg-surface-container border border-white/6 text-foreground rounded-tl-sm'
              "
            >
              <p class="whitespace-pre-wrap">{{ msg.text }}</p>

              <!-- Movie suggestions list -->
              <div
                v-if="msg.suggestions && msg.suggestions.length > 0"
                class="mt-3 space-y-2 pt-2 border-t border-white/10"
              >
                <p class="text-[10px] font-semibold text-muted-foreground">
                  Gợi ý phim cho bạn:
                </p>
                <div class="flex flex-col gap-1.5">
                  <NuxtLink
                    v-for="movie in msg.suggestions"
                    :key="movie.slug"
                    :to="`/movies/${movie.slug}`"
                    class="group flex items-center gap-2.5 rounded-xl border border-white/8 bg-surface-container-lowest/80 p-2 transition hover:border-primary/50 hover:bg-surface-container"
                    @click="recordFeedback(msg, movie.slug)"
                  >
                    <img
                      :src="movie.posterUrl"
                      :alt="movie.title"
                      class="size-9 rounded-lg object-cover"
                    />
                    <div class="min-w-0 flex-1">
                      <p
                        class="truncate font-semibold text-foreground group-hover:text-primary transition-colors text-[11px]"
                      >
                        {{ movie.title }}
                      </p>
                      <p class="truncate text-[9px] text-muted-foreground">
                        {{ movie.genre }} · {{ movie.year }}
                      </p>
                    </div>
                    <Film
                      class="size-3.5 text-muted-foreground group-hover:text-primary shrink-0"
                    />
                  </NuxtLink>
                </div>
              </div>
            </div>
          </div>

          <!-- Typing indicator -->
          <div
            v-if="isSending"
            class="flex items-center gap-2 text-muted-foreground pl-1"
          >
            <div
              class="grid size-6 place-items-center rounded-lg bg-primary/20 text-primary"
            >
              <Bot class="size-3.5" />
            </div>
            <div
              class="flex items-center gap-1 rounded-full bg-surface-container px-3 py-1.5"
            >
              <span
                class="size-1.5 rounded-full bg-primary animate-bounce [animation-delay:-0.3s]"
              />
              <span
                class="size-1.5 rounded-full bg-primary animate-bounce [animation-delay:-0.15s]"
              />
              <span class="size-1.5 rounded-full bg-primary animate-bounce" />
            </div>
          </div>
        </div>

        <!-- Quick suggestion chips -->
        <div
          v-if="chatMessages.length <= 2 && !isSending"
          class="flex flex-wrap gap-1.5 border-t border-white/6 bg-surface-container/30 px-3.5 py-2"
        >
          <button
            v-for="chip in quickSuggestions"
            :key="chip"
            class="rounded-full border border-white/10 bg-surface-container px-2.5 py-1 text-[10px] text-muted-foreground transition hover:border-primary/40 hover:text-primary"
            @click="handleQuickPrompt(chip)"
          >
            {{ chip }}
          </button>
        </div>

        <!-- Input Box -->
        <footer class="border-t border-white/10 bg-surface-container p-3">
          <form class="flex items-center gap-2" @submit.prevent="send()">
            <input
              v-model="prompt"
              type="text"
              :placeholder="copy.placeholder || 'Hỏi AI gợi ý phim...'"
              :disabled="isSending"
              class="flex-1 rounded-xl border border-white/10 bg-surface-container-lowest px-3 py-2 text-xs text-foreground placeholder:text-muted-foreground/60 outline-none focus:border-primary/60 transition disabled:opacity-50"
            />
            <button
              type="submit"
              :disabled="isSending || !prompt.trim()"
              class="grid size-8 place-items-center rounded-xl bg-primary text-primary-container-foreground transition hover:opacity-90 disabled:opacity-40"
              aria-label="Send message"
            >
              <CornerDownLeft class="size-3.5" />
            </button>
          </form>
        </footer>
      </section>
    </transition>

    <!-- Floating launcher button -->
    <button
      class="group relative flex size-10 items-center justify-center rounded-full border border-white/15 bg-[#191b24]/90 text-primary shadow-2xl shadow-black/80 backdrop-blur-md transition hover:scale-110 hover:border-primary/60 hover:bg-[#202331] active:scale-95"
      :class="isOpen ? 'border-primary/80 bg-primary text-black' : ''"
      title="ZMovie AI - Gợi ý phim"
      aria-label="Toggle AI Assistant"
      @click="toggleWidget"
    >
      <Sparkles class="size-4.5 transition-transform group-hover:rotate-12" />
      <span
        v-if="!isOpen"
        class="absolute -right-0.5 -top-0.5 size-2.5 rounded-full bg-primary ring-2 ring-background animate-ping"
      />
    </button>
  </div>
</template>
