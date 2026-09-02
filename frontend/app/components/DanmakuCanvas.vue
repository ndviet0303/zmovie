<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from "vue";

export interface DanmakuItem {
  id?: string;
  timeSeconds: number;
  content: string;
  color?: string;
}

interface ActiveBullet {
  id: string;
  text: string;
  color: string;
  x: number;
  y: number;
  width: number;
  speed: number;
}

const props = withDefaults(
  defineProps<{
    currentTime: number;
    isPlaying: boolean;
    danmakus: DanmakuItem[];
    enabled?: boolean;
    opacity?: number;
    fontSize?: number;
    speedMultiplier?: number;
  }>(),
  {
    enabled: true,
    opacity: 0.85,
    fontSize: 18,
    speedMultiplier: 1.0,
  },
);

const canvas = ref<HTMLCanvasElement | null>(null);
let animationFrameId: number | null = null;
let lastTimestamp = 0;
const activeBullets: ActiveBullet[] = [];
const firedBulletIds = new Set<string>();

const TOTAL_LANES = 8;
const laneOccupancy: number[] = new Array(TOTAL_LANES).fill(0);

function getAvailableLane(bulletWidth: number, canvasWidth: number): number {
  let bestLane = 0;
  let minConflict = Infinity;

  for (let i = 0; i < TOTAL_LANES; i++) {
    const trailingX = laneOccupancy[i];
    if (trailingX < canvasWidth - 40) {
      laneOccupancy[i] = canvasWidth + bulletWidth + 20;
      return i;
    }
    if (trailingX < minConflict) {
      minConflict = trailingX;
      bestLane = i;
    }
  }

  laneOccupancy[bestLane] = canvasWidth + bulletWidth + 20;
  return bestLane;
}

function spawnBullet(
  item: DanmakuItem,
  canvasWidth: number,
  canvasHeight: number,
) {
  if (!canvas.value) return;
  const ctx = canvas.value.getContext("2d");
  if (!ctx) return;

  ctx.font = `bold ${props.fontSize}px 'Plus Jakarta Sans', system-ui, sans-serif`;
  const textMetrics = ctx.measureText(item.content);
  const width = textMetrics.width;

  const lane = getAvailableLane(width, canvasWidth);
  const laneHeight = canvasHeight / (TOTAL_LANES + 2);
  const y = (lane + 1) * laneHeight;

  const baseSpeed = (canvasWidth + width) / 6; // traverse screen in ~6 seconds
  const speed = baseSpeed * props.speedMultiplier;

  activeBullets.push({
    id: item.id || `${item.timeSeconds}-${item.content}-${Math.random()}`,
    text: item.content,
    color: item.color || "#ffffff",
    x: canvasWidth,
    y,
    width,
    speed,
  });
}

function renderFrame(timestamp: number) {
  if (!lastTimestamp) lastTimestamp = timestamp;
  const delta = Math.min((timestamp - lastTimestamp) / 1000, 0.1);
  lastTimestamp = timestamp;

  const cvs = canvas.value;
  if (!cvs) return;
  const ctx = cvs.getContext("2d");
  if (!ctx) return;

  ctx.clearRect(0, 0, cvs.width, cvs.height);

  if (props.enabled) {
    // Check for incoming bullets at current video time
    for (const item of props.danmakus) {
      const bulletId = item.id || `${item.timeSeconds}-${item.content}`;
      if (!firedBulletIds.has(bulletId)) {
        if (
          props.currentTime >= item.timeSeconds &&
          props.currentTime <= item.timeSeconds + 0.4
        ) {
          firedBulletIds.add(bulletId);
          spawnBullet(item, cvs.width, cvs.height);
        }
      }
    }

    // Update & draw active bullets
    ctx.save();
    ctx.globalAlpha = props.opacity;
    ctx.font = `bold ${props.fontSize}px 'Plus Jakarta Sans', system-ui, sans-serif`;
    ctx.lineWidth = 3;
    ctx.strokeStyle = "rgba(0, 0, 0, 0.85)";

    for (let i = activeBullets.length - 1; i >= 0; i--) {
      const bullet = activeBullets[i];

      if (props.isPlaying) {
        bullet.x -= bullet.speed * delta;
      }

      // Draw stroke & fill
      ctx.strokeText(bullet.text, bullet.x, bullet.y);
      ctx.fillStyle = bullet.color;
      ctx.fillText(bullet.text, bullet.x, bullet.y);

      // Remove if moved past left boundary
      if (bullet.x + bullet.width < 0) {
        activeBullets.splice(i, 1);
      }
    }

    // Decay lane occupancies
    for (let l = 0; l < TOTAL_LANES; l++) {
      if (laneOccupancy[l] > 0 && props.isPlaying) {
        laneOccupancy[l] = Math.max(0, laneOccupancy[l] - 160 * delta);
      }
    }

    ctx.restore();
  }

  animationFrameId = requestAnimationFrame(renderFrame);
}

function resizeCanvas() {
  if (!canvas.value || !canvas.value.parentElement) return;
  const rect = canvas.value.parentElement.getBoundingClientRect();
  canvas.value.width = rect.width;
  canvas.value.height = rect.height;
}

// When seeking, reset fired history for forward/backward
watch(
  () => props.currentTime,
  (newTime, oldTime) => {
    if (Math.abs(newTime - (oldTime || 0)) > 2) {
      firedBulletIds.clear();
      activeBullets.length = 0;
      laneOccupancy.fill(0);
    }
  },
);

onMounted(() => {
  resizeCanvas();
  window.addEventListener("resize", resizeCanvas);
  animationFrameId = requestAnimationFrame(renderFrame);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", resizeCanvas);
  if (animationFrameId !== null) {
    cancelAnimationFrame(animationFrameId);
  }
});
</script>

<template>
  <canvas
    ref="canvas"
    class="pointer-events-none absolute inset-0 z-20 size-full"
    aria-hidden="true"
  />
</template>
