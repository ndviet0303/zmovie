<script setup lang="ts">
import { BookOpen, X } from "@lucide/vue";
import type {
  SubtitleCue,
  VocabularyDefinition,
} from "~/composables/useDualSub";

defineProps<{
  primaryCue: SubtitleCue | null;
  secondaryCue: SubtitleCue | null;
  activeDefinition: VocabularyDefinition | null;
  isDictionaryOpen: boolean;
}>();

const emit = defineEmits<{
  "lookup-word": [word: string];
  "close-dictionary": [];
}>();

function tokenizeSentence(text: string): string[] {
  return text.split(/\s+/);
}
</script>

<template>
  <div
    class="pointer-events-none absolute inset-x-0 bottom-14 z-20 flex flex-col items-center justify-end px-6 pb-2 select-none"
  >
    <!-- Dictionary Lookup Popover -->
    <div
      v-if="isDictionaryOpen && activeDefinition"
      class="pointer-events-auto mb-3 flex w-72 flex-col rounded-xl border border-amber-500/40 bg-zinc-900/95 p-3 text-xs shadow-2xl backdrop-blur-md"
    >
      <div
        class="flex items-center justify-between border-b border-zinc-800 pb-2"
      >
        <div class="flex items-center gap-1.5 font-bold text-amber-400">
          <BookOpen class="h-4 w-4" />
          <span>{{ activeDefinition.word }}</span>
        </div>
        <button
          type="button"
          class="rounded p-1 text-zinc-400 hover:text-white"
          @click="emit('close-dictionary')"
        >
          <X class="h-3.5 w-3.5" />
        </button>
      </div>

      <div class="pt-2">
        <div
          v-if="activeDefinition.phonetic"
          class="text-zinc-400 font-mono mb-1"
        >
          {{ activeDefinition.phonetic }}
          <span
            v-if="activeDefinition.partOfSpeech"
            class="italic text-zinc-500"
          >
            ({{ activeDefinition.partOfSpeech }})
          </span>
        </div>
        <div class="text-white font-medium">
          {{ activeDefinition.translation }}
        </div>
      </div>
    </div>

    <!-- Dual Subtitle Text Container -->
    <div
      v-if="primaryCue || secondaryCue"
      class="flex flex-col items-center gap-1 rounded-lg bg-black/75 px-4 py-2 text-center backdrop-blur-sm"
    >
      <!-- Primary Subtitle (Clickable words) -->
      <div
        v-if="primaryCue"
        class="pointer-events-auto flex flex-wrap justify-center gap-1 text-base font-semibold text-white md:text-lg"
      >
        <span
          v-for="(word, idx) in tokenizeSentence(primaryCue.text)"
          :key="idx"
          class="cursor-pointer rounded px-1 transition hover:bg-amber-500/30 hover:text-amber-300"
          @click="emit('lookup-word', word)"
        >
          {{ word }}
        </span>
      </div>

      <!-- Secondary Subtitle (Translated) -->
      <div
        v-if="secondaryCue"
        class="text-xs text-amber-300/90 md:text-sm font-medium"
      >
        {{ secondaryCue.text }}
      </div>
    </div>
  </div>
</template>
