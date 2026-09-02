<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    label: string;
    alert?: boolean;
    panelClass?: string;
  }>(),
  { alert: false, panelClass: "max-w-md" },
);
const emit = defineEmits<{ close: [] }>();

const panel = ref<HTMLElement | null>(null);
let previouslyFocused: HTMLElement | null = null;

// A keydown bound to the overlay div only fires once something inside it has focus,
// which is why the dialogs need a document-level listener to be closable by Escape.
function onKeydown(event: KeyboardEvent) {
  if (event.key === "Escape") {
    event.stopPropagation();
    emit("close");
  }
}

onMounted(() => {
  previouslyFocused = document.activeElement as HTMLElement | null;
  document.addEventListener("keydown", onKeydown);
  panel.value?.focus();
});

onBeforeUnmount(() => {
  document.removeEventListener("keydown", onKeydown);
  previouslyFocused?.focus?.();
});
</script>

<template>
  <div
    class="fixed inset-0 z-50 grid place-items-center bg-black/70 p-4"
    @click.self="emit('close')"
  >
    <div
      ref="panel"
      tabindex="-1"
      :role="props.alert ? 'alertdialog' : 'dialog'"
      aria-modal="true"
      :aria-label="props.label"
      class="w-full overflow-y-auto rounded-2xl border border-white/10 bg-surface-container p-6 outline-none"
      :class="props.panelClass"
    >
      <slot />
    </div>
  </div>
</template>
