<script setup lang="ts">
import type { HTMLAttributes } from "vue";
import { cn } from "@/lib/utils";

defineOptions({ inheritAttrs: false });

const props = defineProps<{
  class?: HTMLAttributes["class"];
}>();

const model = defineModel<string>({ default: "" });
</script>

<template>
  <div
    data-slot="input-field"
    :class="
      cn(
        'flex h-14 w-full items-center gap-3 overflow-hidden rounded-2xl border border-transparent bg-input px-5 text-base text-foreground transition focus-within:border-primary focus-within:bg-primary/8 focus-within:ring-2 focus-within:ring-primary/15',
        props.class,
      )
    "
  >
    <slot name="leading" />
    <input
      v-model="model"
      v-bind="$attrs"
      class="min-w-0 flex-1 bg-transparent text-inherit outline-none placeholder:text-muted-foreground"
    />
    <slot name="trailing" />
  </div>
</template>
