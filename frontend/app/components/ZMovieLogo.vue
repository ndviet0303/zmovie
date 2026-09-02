<template>
  <div class="inline-flex items-center gap-3 select-none">
    <!-- Play smile logo icon -->
    <div class="relative flex items-center justify-center shrink-0">
      <svg
        class="size-10 sm:size-11"
        viewBox="0 0 40 40"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <!-- Glowing outer circle -->
        <circle
          cx="20"
          cy="20"
          r="18"
          stroke="#ffd875"
          stroke-width="2.5"
          fill="#141722"
        />
        <!-- Eyes (two cheerful dots) -->
        <circle cx="16" cy="14" r="1.5" fill="#ffd875" />
        <circle cx="24" cy="14" r="1.5" fill="#ffd875" />
        <!-- Play mark inside smile -->
        <polygon points="18,17 25,21 18,25" fill="#ffd875" />
        <!-- Sweet smile curve below -->
        <path
          d="M12 24C14.5 29 25.5 29 28 24"
          stroke="#ffd875"
          stroke-width="2.5"
          stroke-linecap="round"
        />
        <!-- Sparkle star -->
        <path
          d="M33 9L34 6L37 7L35 10L37 13L34 11L33 14L32 11L29 13L31 10L29 7L32 6Z"
          fill="#ffd875"
          opacity="0.9"
        />
        <circle cx="7" cy="16" r="1.2" fill="#ffd875" />
      </svg>
    </div>
    <!-- Brand text -->
    <div
      class="flex items-center text-2xl sm:text-[25px] font-black tracking-tight text-white font-display leading-none"
    >
      <span>Z</span><span class="text-[#ffd875] ml-0.5">Movie</span>
    </div>
  </div>
</template>
