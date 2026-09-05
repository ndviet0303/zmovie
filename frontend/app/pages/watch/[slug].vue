<script setup lang="ts">
import { computed, onMounted } from "vue";
import { ChevronLeft } from "@lucide/vue";
import { useWatchPlayer } from "~/composables/useWatchPlayer";
import { usePlayerHotkeys } from "~/composables/usePlayerHotkeys";
import { useDanmaku } from "~/composables/useDanmaku";
import { useDualSub } from "~/composables/useDualSub";

const route = useRoute();
const slug = computed(() => String(route.params.slug || ""));

const player = useWatchPlayer(slug.value);
const episodeNumber = computed(() => player.currentEpisode.value?.number ?? 1);
const danmaku = useDanmaku(slug, episodeNumber);
const dualSub = useDualSub();

usePlayerHotkeys({
  togglePlay: player.togglePlay,
  seekBackward: () => player.seek(player.currentTime.value - 10),
  seekForward: () => player.seek(player.currentTime.value + 10),
  toggleMute: player.toggleMute,
  toggleFullscreen: () => {
    if (!document.fullscreenElement) {
      player.playerFrame.value?.requestFullscreen().catch(() => undefined);
    } else {
      document.exitFullscreen().catch(() => undefined);
    }
  },
});

onMounted(() => {
  player.loadData().then(() => {
    player.initPlayer();
    player.setupIntersectionObserver();
    danmaku.loadComments();
    danmaku.connectHub();
    dualSub.loadSampleDemoTracks();
  });
});
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 pb-20">
    <!-- Top Back Navigation -->
    <div class="mx-auto max-w-7xl px-4 py-4 flex items-center justify-between">
      <NuxtLink
        :to="`/movies/${slug}`"
        class="inline-flex items-center gap-1.5 text-xs text-zinc-400 hover:text-white transition"
      >
        <ChevronLeft class="h-4 w-4" />
        Chi tiết phim
      </NuxtLink>
      <div
        v-if="player.title.value"
        class="text-xs text-zinc-400 font-medium truncate max-w-md"
      >
        {{ player.title.value.title }}
      </div>
    </div>

    <!-- Main Player Area -->
    <div
      :class="[
        'mx-auto transition-all duration-300',
        player.isTheaterMode.value ? 'max-w-full px-0' : 'max-w-6xl px-4',
      ]"
    >
      <div
        ref="playerFrame"
        class="relative overflow-hidden rounded-xl border border-zinc-800 bg-black shadow-2xl"
      >
        <!-- Ambilight Ambient Glow -->
        <PlayerAmbilight
          :video-element="player.video.value"
          :is-playing="player.isPlaying.value"
          :enabled="true"
        />

        <!-- Viewport Container -->
        <PlayerViewport
          :source="player.currentSource.value"
          :is-embed="player.isEmbed.value"
          :is-loading="player.isLoading.value"
          :player-error="player.playerError.value"
          @timeupdate="player.onTimeUpdate"
          @play="player.isPlaying.value = true"
          @pause="player.isPlaying.value = false"
          @ended="player.onEnded"
          @loadedmetadata="player.onLoadedMetadata"
          @video-ref="(el) => (player.video.value = el)"
        >
          <!-- Danmaku Canvas Layer -->
          <DanmakuCanvas
            v-if="!player.isEmbed.value"
            :current-time="player.currentTime.value"
            :is-playing="player.isPlaying.value"
            :danmakus="danmaku.danmakus.value"
            :enabled="danmaku.isEnabled.value"
            :opacity="danmaku.opacity.value"
            :font-size="danmaku.fontSize.value"
            :speed-multiplier="danmaku.speedMultiplier.value"
          />

          <!-- Dual-Subtitles & Vocabulary Learning Layer -->
          <DualSubOverlay
            v-if="!player.isEmbed.value"
            :primary-cue="
              dualSub.getActiveCue(
                dualSub.primaryCues.value,
                player.currentTime.value,
              )
            "
            :secondary-cue="
              dualSub.getActiveCue(
                dualSub.secondaryCues.value,
                player.currentTime.value,
              )
            "
            :active-definition="dualSub.activeDefinition.value"
            :is-dictionary-open="dualSub.isDictionaryOpen.value"
            @lookup-word="dualSub.lookupWord"
            @close-dictionary="dualSub.isDictionaryOpen.value = false"
          />

          <!-- Overlays -->
          <div
            class="absolute inset-0 pointer-events-none flex flex-col justify-end"
          >
            <!-- Controls Bar -->
            <div class="pointer-events-auto">
              <PlayerControls
                :is-playing="player.isPlaying.value"
                :current-time="player.currentTime.value"
                :duration="player.duration.value"
                :volume="player.volume.value"
                :is-muted="player.isMuted.value"
                :playback-rate="player.playbackRate.value"
                :quality-options="player.qualityOptions.value"
                :selected-quality="player.selectedQuality.value"
                :subtitle-options="player.subtitleOptions.value"
                :selected-subtitle="player.selectedSubtitle.value"
                :available-sources="player.availableSources.value"
                :active-source-index="player.activeSourceIndex.value"
                :is-theater-mode="player.isTheaterMode.value"
                :show-skip-intro-prompt="player.showSkipIntroPrompt.value"
                @toggle-play="player.togglePlay"
                @seek="player.seek"
                @set-volume="player.setVolume"
                @toggle-mute="player.toggleMute"
                @set-rate="(r) => (player.playbackRate.value = r)"
                @set-quality="player.selectedQuality.value = $event"
                @set-subtitle="player.selectedSubtitle.value = $event"
                @select-source="player.selectSource"
                @toggle-theater="player.toggleTheater"
                @toggle-fullscreen="
                  () => {
                    if (!document.fullscreenElement)
                      player.playerFrame.value
                        ?.requestFullscreen()
                        .catch(() => undefined);
                    else document.exitFullscreen().catch(() => undefined);
                  }
                "
                @skip-intro="player.skipIntro"
              />
            </div>
          </div>
        </PlayerViewport>
      </div>
    </div>

    <!-- Scrolled Floating Mini-Player -->
    <MiniPlayer
      :is-active="player.isMiniPlayerActive.value"
      :is-playing="player.isPlaying.value"
      :title="player.title.value?.title || 'Đang xem'"
      :episode-name="player.currentEpisode.value?.name || ''"
      :video-element="player.video.value"
      @toggle-play="player.togglePlay"
      @close="player.isMiniPlayerActive.value = false"
      @maximize="
        () => {
          player.isMiniPlayerActive.value = false;
          player.playerFrame.value?.scrollIntoView({ behavior: 'smooth' });
        }
      "
    />

    <!-- Episode Selection Grid & Info Section -->
    <div class="mx-auto max-w-6xl px-4 mt-8">
      <div
        v-if="
          player.playback.value && player.playback.value.episodes.length > 1
        "
        class="mb-8"
      >
        <h3
          class="text-sm font-semibold text-zinc-300 uppercase tracking-wider mb-4"
        >
          Danh sách tập ({{ player.playback.value.episodes.length }})
        </h3>
        <div class="flex flex-wrap gap-2.5 max-h-60 overflow-y-auto p-1">
          <button
            v-for="(ep, idx) in player.playback.value.episodes"
            :key="ep.number"
            type="button"
            :class="[
              'rounded-lg px-4 py-2 text-xs font-medium transition',
              idx === player.selectedEpisodeIndex.value
                ? 'bg-amber-500 text-black font-semibold shadow-lg shadow-amber-500/20'
                : 'bg-zinc-900 border border-zinc-800 text-zinc-300 hover:bg-zinc-800',
            ]"
            @click="player.selectEpisode(idx)"
          >
            {{ ep.name || `Tập ${ep.number}` }}
          </button>
        </div>
      </div>

      <!-- Movie Details Card -->
      <div
        v-if="player.title.value"
        class="rounded-xl border border-zinc-800/80 bg-zinc-900/40 p-6"
      >
        <div class="flex flex-col md:flex-row gap-6 items-start">
          <img
            :src="player.title.value.posterUrl"
            :alt="player.title.value.title"
            class="w-32 rounded-lg object-cover shadow-md shrink-0"
          />
          <div class="flex-1">
            <h1 class="text-xl font-bold text-white mb-2">
              {{ player.title.value.title }}
            </h1>
            <div
              class="flex flex-wrap items-center gap-3 text-xs text-zinc-400 mb-4"
            >
              <span>{{ player.title.value.year }}</span>
              <span>•</span>
              <span>{{ player.title.value.genre }}</span>
              <span v-if="player.title.value.runtimeMinutes"
                >• {{ player.title.value.runtimeMinutes }} phút</span
              >
              <span v-if="player.viewCount.value"
                >• {{ player.viewCount.value.toLocaleString() }} lượt xem</span
              >
            </div>
            <p class="text-xs leading-relaxed text-zinc-300">
              {{ player.title.value.synopsis }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
