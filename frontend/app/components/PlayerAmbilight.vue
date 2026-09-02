<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from "vue";

const props = defineProps<{
  videoElement: HTMLVideoElement | null;
  isPlaying: boolean;
  enabled?: boolean;
}>();

const canvas = ref<HTMLCanvasElement | null>(null);
const glowStyle = ref<Record<string, string>>({});
let animationFrameId: number | null = null;
let lastSampleTime = 0;
const SAMPLE_INTERVAL_MS = 1000 / 25; // 25 fps sample rate
const isPowerSaver = ref(false);

function checkPowerAndDevice() {
  if (!import.meta.client) return;
  // Disable on mobile / touch screen to preserve battery
  if (window.matchMedia && window.matchMedia("(pointer: coarse)").matches) {
    isPowerSaver.value = true;
    return;
  }

  // Battery API check
  if ("getBattery" in navigator) {
    (
      navigator as unknown as {
        getBattery: () => Promise<{ level: number; charging: boolean }>;
      }
    )
      .getBattery()
      .then((battery) => {
        if (battery.level < 0.2 && !battery.charging) {
          isPowerSaver.value = true;
        }
      })
      .catch(() => undefined);
  }
}

function sampleFrame() {
  if (
    !props.enabled ||
    isPowerSaver.value ||
    !props.isPlaying ||
    !props.videoElement ||
    !canvas.value
  ) {
    animationFrameId = requestAnimationFrame(sampleFrame);
    return;
  }

  const now = performance.now();
  if (now - lastSampleTime < SAMPLE_INTERVAL_MS) {
    animationFrameId = requestAnimationFrame(sampleFrame);
    return;
  }
  lastSampleTime = now;

  const video = props.videoElement;
  if (video.readyState < 2 || video.videoWidth === 0) {
    animationFrameId = requestAnimationFrame(sampleFrame);
    return;
  }

  const cvs = canvas.value;
  const ctx = cvs.getContext("2d", { willReadFrequently: true });
  if (!ctx) {
    animationFrameId = requestAnimationFrame(sampleFrame);
    return;
  }

  try {
    ctx.drawImage(video, 0, 0, cvs.width, cvs.height);
    const imgData = ctx.getImageData(0, 0, cvs.width, cvs.height).data;

    // Sample top, bottom, left, right quadrants
    let r = 0,
      g = 0,
      b = 0,
      count = 0;
    for (let i = 0; i < imgData.length; i += 16) {
      r += imgData[i] || 0;
      g += imgData[i + 1] || 0;
      b += imgData[i + 2] || 0;
      count++;
    }

    if (count > 0) {
      const avgR = Math.round(r / count);
      const avgG = Math.round(g / count);
      const avgB = Math.round(b / count);

      glowStyle.value = {
        background: `radial-gradient(circle at 50% 50%, rgba(${avgR}, ${avgG}, ${avgB}, 0.45) 0%, rgba(${avgR}, ${avgG}, ${avgB}, 0.15) 50%, transparent 80%)`,
        filter: "blur(48px)",
      };
    }
  } catch {
    // Cross-origin canvas security catch
  }

  animationFrameId = requestAnimationFrame(sampleFrame);
}

onMounted(() => {
  checkPowerAndDevice();
  if (props.isPlaying) {
    animationFrameId = requestAnimationFrame(sampleFrame);
  }
});

watch(
  () => props.isPlaying,
  (playing) => {
    if (playing && !animationFrameId) {
      animationFrameId = requestAnimationFrame(sampleFrame);
    }
  },
);

onBeforeUnmount(() => {
  if (animationFrameId) {
    cancelAnimationFrame(animationFrameId);
    animationFrameId = null;
  }
});
</script>

<template>
  <div
    class="pointer-events-none absolute inset-0 -z-10 overflow-visible transition-opacity duration-700"
  >
    <canvas ref="canvas" width="16" height="9" class="hidden" />
    <div
      v-if="enabled && !isPowerSaver"
      class="absolute -inset-10 opacity-75 transition-all duration-300 md:-inset-16"
      :style="glowStyle"
    />
  </div>
</template>
