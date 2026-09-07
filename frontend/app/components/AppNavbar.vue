<script setup lang="ts">
import {
  Menu,
  Search,
  ChevronDown,
  User as UserIcon,
  LogOut,
} from "@lucide/vue";

defineProps<{ locale?: "vi" | "en" }>();
defineEmits<{ localeChange: [locale: "vi" | "en"] }>();

const isDrawerOpen = ref(false);
const { user, signOut } = useAuthSession();
const {
  query: searchQuery,
  results: searchResults,
  isOpen: isSearchOpen,
  isLoading: isSearchLoading,
  open: openSearch,
  blur: blurSearch,
  submit: submitSearch,
  select: selectSearchResult,
} = useGlobalSearch();
const isTheLoaiOpen = ref(false);
const isQuocGiaOpen = ref(false);
const isThemOpen = ref(false);

const genres = [
  "Hành Động",
  "Cổ Trang",
  "Chiến Tranh",
  "Viễn Tưởng",
  "Kinh Dị",
  "Tài Liệu",
  "Bí Ẩn",
  "Phim Hài",
  "Tình Cảm",
  "Tâm Lý",
  "Thể Thao",
  "Phiêu Lưu",
  "Âm Nhạc",
  "Gia Đình",
  "Học Đường",
  "Hình Sự",
  "Võ Thuật",
  "Khoa Học",
  "Thần Thoại",
];

const countries = [
  "Trung Quốc",
  "Hàn Quốc",
  "Âu Mỹ",
  "Nhật Bản",
  "Thái Lan",
  "Ấn Độ",
  "Đài Loan",
  "Hồng Kông",
  "Việt Nam",
];

const moreItems = [
  { name: "Lướt Shorts", path: "/shorts" },
  { name: "AI Movie Bot", path: "/assistant" },
  { name: "Cài đặt ứng dụng", path: "/app" },
  { name: "Top IMDb", path: "/browse?sort=rating" },
  { name: "Phim Chiếu Rạp", path: "/browse?genre=Chiếu%20Rạp" },
  { name: "Anime", path: "/browse?genre=Hoạt%20Hình" },
  { name: "Netflix", path: "/browse?country=Âu%20Mỹ" },
  { name: "Phim 4K", path: "/browse?collection=recommended" },
];

async function handleLogout() {
  await signOut();
  await navigateTo("/");
}
</script>

<template>
  <header
    class="sticky top-0 z-40 w-full transition-all duration-300 bg-gradient-to-b from-black/85 via-black/40 to-transparent"
  >
    <div
      class="mx-auto flex h-21 sm:h-22 max-w-[1600px] items-center justify-between px-4 sm:px-6 lg:px-8 xl:px-10"
    >
      <!-- LEFT: Logo + Desktop Search + Navigation Links -->
      <div class="flex items-center gap-3 xl:gap-4 2xl:gap-6 min-w-0">
        <!-- Mobile/Tablet hamburger toggle -->
        <button
          type="button"
          class="grid size-11 place-items-center rounded-xl text-white/90 transition hover:bg-white/10 hover:text-primary active:scale-95 xl:hidden"
          aria-label="Mở menu"
          @click="isDrawerOpen = true"
        >
          <Menu class="size-6 stroke-[2.2]" />
        </button>

        <!-- Brand Logo -->
        <NuxtLink
          to="/"
          class="transition hover:opacity-90 active:scale-[0.99] shrink-0"
        >
          <ZMovieLogo />
        </NuxtLink>

        <!-- Desktop Search Input (Clean Pill) -->
        <form
          class="relative hidden md:block"
          @submit.prevent="submitSearch"
          @focusin="openSearch"
          @focusout="blurSearch"
        >
          <Search
            class="pointer-events-none absolute left-4 top-1/2 size-4.5 -translate-y-1/2 text-gray-400"
          />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm kiếm phim, diễn viên..."
            class="h-11 w-44 sm:w-52 lg:w-64 xl:w-56 2xl:w-72 rounded-full border border-white/12 bg-black/40 pl-11 pr-5 text-sm font-medium text-white placeholder-gray-400/80 backdrop-blur-md transition focus:border-primary/60 focus:bg-black/60 focus:outline-none focus:ring-1 focus:ring-primary/40"
          />
          <div
            v-if="isSearchOpen && searchQuery.trim().length >= 2"
            class="absolute left-0 top-[calc(100%+0.5rem)] z-50 w-96 overflow-hidden rounded-2xl border border-white/10 bg-[#191b24]/98 p-2 shadow-2xl backdrop-blur-xl"
          >
            <p
              v-if="isSearchLoading"
              class="px-3 py-4 text-center text-xs text-gray-400"
            >
              Đang tìm kiếm…
            </p>
            <template v-else-if="searchResults.length">
              <button
                v-for="result in searchResults"
                :key="result.slug"
                type="button"
                class="flex w-full items-center gap-3 rounded-xl p-2 text-left transition hover:bg-white/10"
                @mousedown.prevent
                @click="selectSearchResult(result.slug)"
              >
                <img
                  :src="result.posterUrl"
                  :alt="result.title"
                  class="h-14 w-10 shrink-0 rounded-md object-cover"
                />
                <span class="min-w-0">
                  <span class="block truncate text-sm font-semibold text-white">
                    {{ result.title }}
                  </span>
                  <span class="mt-1 block truncate text-[11px] text-gray-400">
                    {{ result.year }} · {{ result.genre }}
                  </span>
                </span>
              </button>
              <button
                type="submit"
                class="mt-1 w-full rounded-xl px-3 py-2 text-center text-xs font-semibold text-primary transition hover:bg-primary/10"
                @mousedown.prevent
              >
                Xem tất cả kết quả
              </button>
            </template>
            <p v-else class="px-3 py-4 text-center text-xs text-gray-400">
              Không tìm thấy phim phù hợp.
            </p>
          </div>
        </form>

        <!-- Desktop Navigation Bar -->
        <nav
          class="hidden xl:flex items-center gap-0.5 xl:gap-1 2xl:gap-2 text-[13.5px] 2xl:text-[15px] font-semibold text-white/90 whitespace-nowrap shrink-0"
        >
          <!-- 1. Thể loại Dropdown -->
          <div
            class="relative shrink-0"
            @mouseenter="isTheLoaiOpen = true"
            @mouseleave="isTheLoaiOpen = false"
          >
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap transition hover:bg-white/5 hover:text-primary shrink-0"
              :class="{ 'text-primary': isTheLoaiOpen }"
            >
              <span class="whitespace-nowrap">Thể loại</span>
              <ChevronDown
                class="size-4 shrink-0 transition-transform duration-200"
                :class="{ 'rotate-180': isTheLoaiOpen }"
              />
            </button>

            <!-- Dropdown Menu -->
            <div
              v-show="isTheLoaiOpen"
              class="absolute left-0 top-full z-50 mt-1 w-96 rounded-2xl border border-white/10 bg-[#191b24]/95 p-4 shadow-2xl backdrop-blur-xl transition-all"
            >
              <div class="grid grid-cols-3 gap-1.5 text-xs font-medium">
                <NuxtLink
                  v-for="g in genres"
                  :key="g"
                  :to="`/browse?genre=${encodeURIComponent(g)}`"
                  class="rounded-lg px-2.5 py-2 text-gray-300 transition hover:bg-white/10 hover:text-primary"
                  @click="isTheLoaiOpen = false"
                >
                  {{ g }}
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- 2. Phim Lẻ Link -->
          <NuxtLink
            to="/browse?format=movie"
            class="rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap shrink-0 transition hover:bg-white/5 hover:text-primary"
          >
            Phim Lẻ
          </NuxtLink>

          <!-- 3. Phim Bộ Link -->
          <NuxtLink
            to="/browse?format=series"
            class="rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap shrink-0 transition hover:bg-white/5 hover:text-primary"
          >
            Phim Bộ
          </NuxtLink>

          <!-- 4. Lịch Chiếu Link -->
          <NuxtLink
            to="/schedule"
            class="rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap shrink-0 transition hover:bg-white/5 hover:text-primary"
          >
            Lịch Chiếu
          </NuxtLink>

          <!-- 5. Xem Chung Link -->
          <NuxtLink
            to="/party"
            class="rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap shrink-0 transition hover:bg-white/5 hover:text-primary"
          >
            Xem Chung
          </NuxtLink>

          <!-- 6. Nghệ Sĩ Link -->
          <NuxtLink
            to="/actors"
            class="rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap shrink-0 transition hover:bg-white/5 hover:text-primary"
          >
            Nghệ Sĩ
          </NuxtLink>
          <!-- 4. Quốc gia Dropdown -->
          <div
            class="relative shrink-0"
            @mouseenter="isQuocGiaOpen = true"
            @mouseleave="isQuocGiaOpen = false"
          >
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap transition hover:bg-white/5 hover:text-primary shrink-0"
              :class="{ 'text-primary': isQuocGiaOpen }"
            >
              <span class="whitespace-nowrap">Quốc gia</span>
              <ChevronDown
                class="size-4 shrink-0 transition-transform duration-200"
                :class="{ 'rotate-180': isQuocGiaOpen }"
              />
            </button>

            <!-- Dropdown Menu -->
            <div
              v-show="isQuocGiaOpen"
              class="absolute left-0 top-full z-50 mt-1 w-64 rounded-2xl border border-white/10 bg-[#191b24]/95 p-3 shadow-2xl backdrop-blur-xl"
            >
              <div class="grid grid-cols-2 gap-1 text-xs font-medium">
                <NuxtLink
                  v-for="c in countries"
                  :key="c"
                  :to="`/browse?country=${encodeURIComponent(c)}`"
                  class="rounded-lg px-2.5 py-2 text-gray-300 transition hover:bg-white/10 hover:text-primary"
                  @click="isQuocGiaOpen = false"
                >
                  {{ c }}
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- 5. Thêm Dropdown -->
          <div
            class="relative shrink-0"
            @mouseenter="isThemOpen = true"
            @mouseleave="isThemOpen = false"
          >
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-xl px-2 xl:px-2.5 2xl:px-3 py-1.5 xl:py-2 whitespace-nowrap transition hover:bg-white/5 hover:text-primary shrink-0"
              :class="{ 'text-primary': isThemOpen }"
            >
              <span class="whitespace-nowrap">Thêm</span>
              <ChevronDown
                class="size-4 shrink-0 transition-transform duration-200"
                :class="{ 'rotate-180': isThemOpen }"
              />
            </button>

            <!-- Dropdown Menu -->
            <div
              v-show="isThemOpen"
              class="absolute left-0 top-full z-50 mt-1 w-48 rounded-2xl border border-white/10 bg-[#191b24]/95 p-2 shadow-2xl backdrop-blur-xl"
            >
              <div class="flex flex-col gap-0.5 text-xs font-medium">
                <NuxtLink
                  v-for="m in moreItems"
                  :key="m.name"
                  :to="m.path"
                  class="rounded-lg px-3 py-2 text-gray-300 transition hover:bg-white/10 hover:text-primary"
                  @click="isThemOpen = false"
                >
                  {{ m.name }}
                </NuxtLink>
              </div>
            </div>
          </div>
        </nav>
      </div>

      <!-- RIGHT: Thành Viên Button -->
      <div class="flex items-center gap-3 xl:gap-4 shrink-0">
        <!-- Mobile search button (when screen < md) -->
        <NuxtLink
          to="/browse"
          aria-label="Tìm kiếm"
          class="grid size-10 place-items-center rounded-xl text-white/90 transition hover:bg-white/10 hover:text-primary active:scale-95 md:hidden"
        >
          <Search class="size-5" />
        </NuxtLink>

        <!-- Thành viên Button (Clean White Pill on Desktop) -->
        <NuxtLink
          v-if="!user"
          to="/login"
          class="flex items-center gap-2.5 rounded-full bg-white px-5 py-2.5 text-sm font-bold text-black shadow-lg transition hover:bg-gray-100 hover:scale-105 active:scale-95"
        >
          <UserIcon class="size-4" />
          <span>Thành viên</span>
        </NuxtLink>

        <!-- Logged in state dropdown -->
        <div v-else class="relative group">
          <NuxtLink
            to="/profile"
            class="flex items-center gap-2.5 rounded-full border border-primary/50 bg-[#191b24] py-1.5 pl-2 pr-4 text-sm font-bold text-white transition hover:border-primary"
          >
            <img
              :src="user.avatarUrl || '/default-meme-avatar.png'"
              :alt="user.displayName"
              class="size-7 rounded-full object-cover"
            />
            <span class="max-w-[120px] truncate">{{ user.displayName }}</span>
          </NuxtLink>

          <!-- Profile quick menu on hover -->
          <div
            class="absolute right-0 top-full hidden group-hover:flex flex-col z-50 mt-1.5 w-48 rounded-2xl border border-white/10 bg-[#191b24]/95 p-2 shadow-2xl backdrop-blur-xl text-xs font-medium"
          >
            <NuxtLink
              to="/profile"
              class="rounded-lg px-3 py-2 text-gray-200 hover:bg-white/10 hover:text-primary transition"
            >
              Hồ sơ của tôi
            </NuxtLink>
            <NuxtLink
              to="/my-list"
              class="rounded-lg px-3 py-2 text-gray-200 hover:bg-white/10 hover:text-primary transition"
            >
              Danh sách xem
            </NuxtLink>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg px-3 py-2 text-red-400 hover:bg-red-500/10 transition text-left"
              @click="handleLogout"
            >
              <LogOut class="size-3.5" /> Đăng xuất
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Slide-out Drawer Component (for Mobile) -->
    <AppDrawer :is-open="isDrawerOpen" @close="isDrawerOpen = false" />
  </header>
</template>
