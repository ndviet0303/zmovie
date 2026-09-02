<script setup lang="ts">
import { computed } from "vue";

const props = withDefaults(
  defineProps<{
    level: number;
    title?: string;
    currentExp?: number;
    nextExp?: number;
    showTooltip?: boolean;
  }>(),
  {
    title: "Thành viên",
    currentExp: 0,
    nextExp: 100,
    showTooltip: true,
  },
);

const levelConfig = computed(() => {
  const lvl = Math.max(1, Math.min(6, props.level));
  switch (lvl) {
    case 1:
      return {
        bg: "bg-slate-700 text-slate-200 border-slate-500",
        label: "Lv1",
        glow: "hover:shadow-slate-500/20",
      };
    case 2:
      return {
        bg: "bg-emerald-800 text-emerald-100 border-emerald-500",
        label: "Lv2",
        glow: "hover:shadow-emerald-500/30",
      };
    case 3:
      return {
        bg: "bg-sky-800 text-sky-100 border-sky-400",
        label: "Lv3",
        glow: "hover:shadow-sky-500/40",
      };
    case 4:
      return {
        bg: "bg-amber-600 text-amber-950 font-bold border-amber-300",
        label: "Lv4",
        glow: "hover:shadow-amber-500/50",
      };
    case 5:
      return {
        bg: "bg-gradient-to-r from-purple-600 to-indigo-600 text-white font-bold border-purple-300",
        label: "Lv5",
        glow: "hover:shadow-purple-500/60",
      };
    case 6:
    default:
      return {
        bg: "bg-gradient-to-r from-pink-500 via-red-500 to-amber-500 text-white font-black border-yellow-200 animate-pulse",
        label: "Lv6",
        glow: "hover:shadow-pink-500/70 shadow-lg",
      };
  }
});

const progressPercent = computed(() => {
  if (!props.nextExp || props.nextExp === 0) return 100;
  return Math.min(100, Math.round((props.currentExp / props.nextExp) * 100));
});
</script>

<template>
  <div class="group relative inline-flex items-center">
    <span
      :class="[
        'inline-flex items-center justify-center rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wider border transition-all duration-300 shadow-sm cursor-help',
        levelConfig.bg,
        levelConfig.glow,
      ]"
    >
      {{ levelConfig.label }}
    </span>

    <!-- Hover EXP Tooltip -->
    <div
      v-if="showTooltip"
      class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden w-44 rounded-lg border border-zinc-700 bg-zinc-900/95 p-2.5 text-xs text-zinc-200 shadow-xl backdrop-blur-md group-hover:block z-50"
    >
      <div class="flex items-center justify-between font-bold mb-1">
        <span>{{ levelConfig.label }} - {{ title }}</span>
        <span class="text-amber-400">{{ progressPercent }}%</span>
      </div>
      <div class="h-1.5 w-full overflow-hidden rounded-full bg-zinc-800">
        <div
          class="h-full bg-gradient-to-r from-amber-500 to-yellow-300 transition-all duration-300"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>
      <div class="mt-1 flex justify-between text-[10px] text-zinc-400">
        <span>EXP: {{ currentExp.toLocaleString() }}</span>
        <span>Mục tiêu: {{ nextExp.toLocaleString() }}</span>
      </div>
    </div>
  </div>
</template>
