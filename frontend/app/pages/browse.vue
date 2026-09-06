<script setup lang="ts">
import { Filter, Search, X } from "@lucide/vue";

const {
  locale,
  query,
  selectedGenre,
  selectedCountry,
  selectedYear,
  selectedFormat,
  filtersOpen,
  sortOrder,
  page,
  totalResults,
  totalPages,
  years,
  isLoading,
  visibleTitles,
  changeLocale,
  genreLabel,
  goToPage,
} = useBrowse();

// Filter options matching CôBéPhim
const countryOptions = [
  "Tất cả",
  "Trung Quốc",
  "Âu Mỹ",
  "Hàn Quốc",
  "Nhật Bản",
  "Thái Lan",
  "Hồng Kông",
  "Việt Nam",
  "Đài Loan",
  "Anh",
  "Pháp",
  "Canada",
  "Ấn Độ",
  "Nga",
  "Singapore",
  "Indonesia",
  "Philippines",
];

const formatOptions = [
  { label: "Tất cả", value: "all" },
  { label: "Phim lẻ", value: "movie" },
  { label: "Phim bộ", value: "series" },
];

const genreOptions = [
  "Tất cả",
  "Hài Hước",
  "Gia Đình",
  "Hành Động",
  "Hình Sự",
  "Kinh Dị",
  "Cổ Trang",
  "Võ Thuật",
  "Short Drama",
  "Tình Cảm",
  "Tài Liệu",
  "Tâm Lý",
  "Chiến Tranh",
  "Thần Thoại",
  "Học Đường",
  "Hoạt hình",
  "Chiếu rạp",
  "Khoa Học Viễn Tưởng",
  "Lãng Mạn",
  "Phiêu Lưu",
  "Song Ngữ",
];

// Page Heading
const pageTitle = computed(() => {
  if (query.value) return `Kết quả tìm kiếm "${query.value}"`;
  if (selectedGenre.value !== "all")
    return `Phim ${genreLabel(selectedGenre.value)}`;
  if (selectedFormat.value === "series") return "Phim Bộ";
  if (selectedFormat.value === "movie") return "Phim Lẻ";
  if (selectedCountry.value !== "all") return `Phim ${selectedCountry.value}`;
  return "Kho Phim Tổng Hợp";
});

// Auto open filters if user selected filter
onMounted(() => {
  if (selectedGenre.value !== "all" || selectedCountry.value !== "all") {
    filtersOpen.value = true;
  }
});
</script>

<template>
  <div class="min-h-screen bg-[#0f111a] text-white flex flex-col">
    <AppNavbar :locale="locale" @locale-change="changeLocale" />

    <main class="mx-auto w-full max-w-360 flex-1 px-4 sm:px-6 lg:px-10 py-8">
      <!-- Title & Search Input Header -->
      <div
        class="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"
      >
        <div>
          <h1
            class="text-2xl sm:text-3xl font-black tracking-tight text-white font-display"
          >
            {{ pageTitle }}
          </h1>
        </div>

        <!-- Search Input Form -->
        <div class="relative w-full max-w-md">
          <div
            class="pointer-events-none absolute inset-y-0 left-3.5 flex items-center text-white/50"
          >
            <Search class="size-4.5" />
          </div>
          <input
            v-model="query"
            type="text"
            placeholder="Tìm kiếm phim, diễn viên, đạo diễn..."
            class="h-11 w-full rounded-full border border-white/10 bg-[#191b24] pl-10 pr-10 text-sm text-white placeholder-white/40 outline-none transition focus:border-primary focus:bg-white/10"
          />
          <button
            v-if="query"
            type="button"
            class="absolute inset-y-0 right-3.5 flex items-center text-white/50 hover:text-white"
            @click="query = ''"
          >
            <X class="size-4" />
          </button>
        </div>
      </div>

      <div v-if="query" class="mt-5 flex items-center gap-2">
        <span
          class="rounded-full bg-white px-5 py-2 text-xs font-bold text-black shadow-md"
        >
          Phim
        </span>
        <NuxtLink
          :to="{ path: '/actors', query: { query } }"
          class="rounded-full bg-white/10 px-5 py-2 text-xs font-bold text-white/80 transition hover:bg-white/15"
        >
          Diễn viên
        </NuxtLink>
      </div>

      <!-- Filter Toggle Trigger Button -->
      <div class="mt-6 flex items-center gap-3">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl border border-white/10 bg-[#191b24] px-4 py-2.5 text-xs font-bold text-white transition hover:border-primary/50 hover:text-primary active:scale-95"
          @click="filtersOpen = !filtersOpen"
        >
          <Filter class="size-3.5 text-primary" />
          <span>Bộ lọc</span>
        </button>

        <!-- Active Filter Quick Chips -->
        <span
          v-if="selectedGenre !== 'all'"
          class="inline-flex items-center gap-1.5 rounded-full border border-primary/40 bg-primary/10 px-3 py-1 text-xs font-semibold text-primary"
        >
          {{ genreLabel(selectedGenre) }}
          <button type="button" @click="selectedGenre = 'all'">
            <X class="size-3" />
          </button>
        </span>
        <span
          v-if="selectedCountry !== 'all'"
          class="inline-flex items-center gap-1.5 rounded-full border border-primary/40 bg-primary/10 px-3 py-1 text-xs font-semibold text-primary"
        >
          {{ selectedCountry }}
          <button type="button" @click="selectedCountry = 'all'">
            <X class="size-3" />
          </button>
        </span>
      </div>

      <!-- Expandable CôBéPhim Multi-Tier Filter Matrix -->
      <transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="opacity-0 -translate-y-2"
        enter-to-class="opacity-100 translate-y-0"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100 translate-y-0"
        leave-to-class="opacity-0 -translate-y-2"
      >
        <div
          v-if="filtersOpen"
          class="mt-4 rounded-3xl border border-white/10 bg-[#141622] p-5 sm:p-7 shadow-2xl flex flex-col gap-5 text-xs"
        >
          <!-- Row 1: Quốc gia -->
          <div
            class="grid grid-cols-1 sm:grid-cols-[100px_1fr] items-start gap-2 sm:gap-4 pb-4 border-b border-white/5"
          >
            <span class="font-bold text-white/70 pt-1.5">Quốc gia:</span>
            <div class="flex flex-wrap gap-1.5">
              <button
                v-for="c in countryOptions"
                :key="c"
                type="button"
                class="rounded-lg px-3 py-1.5 font-semibold transition"
                :class="
                  (c === 'Tất cả' && selectedCountry === 'all') ||
                  selectedCountry === c
                    ? 'bg-primary text-black font-bold shadow-sm'
                    : 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white'
                "
                @click="selectedCountry = c === 'Tất cả' ? 'all' : c"
              >
                {{ c }}
              </button>
            </div>
          </div>

          <!-- Row 2: Loại phim -->
          <div
            class="grid grid-cols-1 sm:grid-cols-[100px_1fr] items-start gap-2 sm:gap-4 pb-4 border-b border-white/5"
          >
            <span class="font-bold text-white/70 pt-1.5">Loại phim:</span>
            <div class="flex flex-wrap gap-1.5">
              <button
                v-for="fmt in formatOptions"
                :key="fmt.value"
                type="button"
                class="rounded-lg px-3 py-1.5 font-semibold transition"
                :class="
                  selectedFormat === fmt.value
                    ? 'bg-primary text-black font-bold shadow-sm'
                    : 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white'
                "
                @click="selectedFormat = fmt.value"
              >
                {{ fmt.label }}
              </button>
            </div>
          </div>

          <!-- Row 3: Năm phát hành -->
          <div
            class="grid grid-cols-1 sm:grid-cols-[100px_1fr] items-start gap-2 sm:gap-4 pb-4 border-b border-white/5"
          >
            <span class="font-bold text-white/70 pt-1.5">Năm:</span>
            <div class="flex max-h-24 flex-wrap gap-1.5 overflow-y-auto">
              <button
                v-for="year in years"
                :key="year"
                type="button"
                class="rounded-lg px-3 py-1.5 font-semibold transition"
                :class="
                  selectedYear === year
                    ? 'bg-primary text-black font-bold shadow-sm'
                    : 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white'
                "
                @click="selectedYear = year"
              >
                {{ year === "all" ? "Tất cả" : year }}
              </button>
            </div>
          </div>

          <!-- Row 4: Thể loại -->
          <div
            class="grid grid-cols-1 sm:grid-cols-[100px_1fr] items-start gap-2 sm:gap-4"
          >
            <span class="font-bold text-white/70 pt-1.5">Thể loại:</span>
            <div class="flex flex-wrap gap-1.5">
              <button
                v-for="g in genreOptions"
                :key="g"
                type="button"
                class="rounded-lg px-3 py-1.5 font-semibold transition"
                :class="
                  (g === 'Tất cả' && selectedGenre === 'all') ||
                  selectedGenre === g
                    ? 'bg-primary text-black font-bold shadow-sm'
                    : 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white'
                "
                @click="selectedGenre = g === 'Tất cả' ? 'all' : g"
              >
                {{ g }}
              </button>
            </div>
          </div>

          <!-- Row 5: Sắp xếp -->
          <div
            class="grid grid-cols-1 sm:grid-cols-[100px_1fr] items-start gap-2 sm:gap-4"
          >
            <span class="font-bold text-white/70 pt-1.5">Sắp xếp:</span>
            <div class="flex flex-wrap gap-1.5">
              <button
                v-for="option in [
                  { value: 'latest', label: 'Mới nhất' },
                  { value: 'oldest', label: 'Cũ nhất' },
                  { value: 'title', label: 'Tên A-Z' },
                ]"
                :key="option.value"
                type="button"
                class="rounded-lg px-3 py-1.5 font-semibold transition"
                :class="
                  sortOrder === option.value
                    ? 'bg-primary text-black font-bold shadow-sm'
                    : 'bg-white/5 text-white/80 hover:bg-white/10 hover:text-white'
                "
                @click="sortOrder = option.value"
              >
                {{ option.label }}
              </button>
            </div>
          </div>
        </div>
      </transition>

      <!-- Movie Card Grid (6 Columns on Desktop, 2 on Mobile) -->
      <section class="mt-8">
        <div
          v-if="isLoading"
          class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5"
        >
          <div
            v-for="i in 12"
            :key="i"
            class="aspect-[2/3] rounded-2xl bg-[#191b24] animate-pulse"
          />
        </div>

        <div
          v-else-if="visibleTitles.length"
          class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5"
        >
          <MovieCard
            v-for="item in visibleTitles"
            :key="item.slug"
            :title="item"
            variant="vertical"
          />
        </div>

        <div
          v-if="!isLoading && totalResults"
          class="mt-8 flex flex-wrap items-center justify-between gap-4 border-t border-white/8 pt-6"
        >
          <p class="text-xs text-white/50">
            {{ totalResults }} phim · Trang {{ page }} / {{ totalPages }}
          </p>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="rounded-xl border border-white/10 bg-white/5 px-4 py-2 text-xs font-semibold transition hover:border-primary/50 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="page <= 1"
              @click="goToPage(page - 1)"
            >
              Trang trước
            </button>
            <button
              type="button"
              class="rounded-xl border border-white/10 bg-white/5 px-4 py-2 text-xs font-semibold transition hover:border-primary/50 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="page >= totalPages"
              @click="goToPage(page + 1)"
            >
              Trang sau
            </button>
          </div>
        </div>

        <!-- Empty State -->
        <div
          v-else
          class="flex flex-col items-center justify-center py-24 text-center"
        >
          <div
            class="size-16 rounded-full bg-white/5 grid place-items-center text-white/40 mb-4"
          >
            <Search class="size-8" />
          </div>
          <h3 class="text-lg font-bold text-white">
            Không tìm thấy phim phù hợp
          </h3>
          <p class="text-sm text-muted-foreground mt-1">
            Hãy thử tìm bằng từ khóa khác hoặc xóa bớt bộ lọc
          </p>
        </div>
      </section>
    </main>

    <AppFooter />
  </div>
</template>
