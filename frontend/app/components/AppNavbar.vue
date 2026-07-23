<script setup lang="ts">
import {
  Bell,
  Bot,
  ChevronRight,
  CircleUserRound,
  LogOut,
  Search,
  UserRound,
} from "@lucide/vue";

const props = defineProps<{ locale: "vi" | "en" }>();
const emit = defineEmits<{ localeChange: [locale: "vi" | "en"] }>();
const { $api } = useNuxtApp();
const route = useRoute();
const isLanguageOpen = ref(false);
const isAccountOpen = ref(false);
const user = ref<{
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
} | null>(null);
const languages = [
  {
    code: "vi" as const,
    label: "Tiếng Việt",
    flag: "https://flagcdn.com/w40/vn.png",
  },
  {
    code: "en" as const,
    label: "English",
    flag: "https://flagcdn.com/w40/gb.png",
  },
] as const;
const navItems = computed(() =>
  props.locale === "vi"
    ? [
        { label: "Trang chủ", to: "/" },
        { label: "Phim lẻ", to: "/browse" },
        { label: "Phim bộ", to: "/browse?type=series" },
        { label: "Thể loại", to: "/genres" },
        { label: "Danh sách của tôi", to: "/my-list" },
      ]
    : [
        { label: "Home", to: "/" },
        { label: "Movies", to: "/browse" },
        { label: "Series", to: "/browse?type=series" },
        { label: "Genres", to: "/genres" },
        { label: "My list", to: "/my-list" },
      ],
);

function isActive(index: number) {
  if (index === 0) return route.path === "/";
  if (index === 1)
    return route.path === "/browse" && route.query.type !== "series";
  if (index === 2)
    return route.path === "/browse" && route.query.type === "series";
  if (index === 3) return route.path === "/genres";
  return route.path === "/my-list";
}

function selectLocale(locale: "vi" | "en") {
  isLanguageOpen.value = false;
  emit("localeChange", locale);
}

async function loadUser() {
  try {
    user.value = await $api("/v1/auth/me", { credentials: "include" });
  } catch {
    user.value = null;
  }
}

async function logout() {
  await $api("/v1/auth/logout", {
    method: "POST",
    credentials: "include",
  });
  user.value = null;
  isAccountOpen.value = false;
  await navigateTo("/");
}

onMounted(() => {
  void loadUser();
});
</script>

<template>
  <header
    class="sticky top-0 z-50 border-b border-white/10 bg-background/90 px-5 py-4 backdrop-blur-xl lg:px-12"
  >
    <div class="mx-auto flex max-w-360 items-center justify-between">
      <div class="flex items-center gap-10">
        <NuxtLink
          to="/"
          class="font-display text-2xl font-semibold tracking-tight text-primary"
          >ZMovie</NuxtLink
        >
        <nav class="hidden items-center gap-6 md:flex">
          <NuxtLink
            v-for="(item, index) in navItems"
            :key="item.label"
            :to="item.to"
            class="border-b-2 pb-1 text-sm transition-colors"
            :class="
              isActive(index)
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-primary'
            "
            >{{ item.label }}</NuxtLink
          >
        </nav>
      </div>
      <div class="flex items-center gap-4 text-muted-foreground sm:gap-6">
        <NuxtLink
          to="/browse"
          aria-label="Search"
          title="Tìm kiếm"
          class="grid size-10 place-items-center rounded-xl border border-white/10 bg-surface-container text-foreground/80 shadow-sm transition hover:border-primary/60 hover:bg-primary/10 hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          ><Search class="size-[18px]"
        /></NuxtLink>
        <NuxtLink
          to="/assistant"
          aria-label="ZMovie Bot"
          title="ZMovie Bot"
          class="hidden size-10 place-items-center rounded-xl border border-white/10 bg-surface-container text-foreground/80 shadow-sm transition hover:border-primary/60 hover:bg-primary/10 hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary xl:grid"
          ><Bot class="size-[18px]"
        /></NuxtLink>
        <span class="hidden h-6 w-px bg-white/10 sm:block" />
        <div class="relative z-[60]">
          <button
            class="flex items-center gap-2 rounded-full border border-white/10 bg-surface-container px-3 py-2 text-xs font-semibold text-foreground transition hover:border-primary/60"
            type="button"
            :aria-expanded="isLanguageOpen"
            @click="isLanguageOpen = !isLanguageOpen"
          >
            <img
              :src="locale === 'vi' ? languages[0].flag : languages[1].flag"
              class="size-4 rounded-full object-cover"
              alt="Current language"
            />
            <span>{{ locale === "vi" ? "VI" : "EN" }}</span
            ><ChevronRight class="size-3 rotate-90" />
          </button>
          <div
            v-if="isLanguageOpen"
            class="absolute right-0 top-[calc(100%+8px)] z-[70] w-40 overflow-hidden rounded-2xl border border-white/10 bg-surface-container p-1.5 shadow-[0_16px_40px_rgba(0,0,0,.42)]"
          >
            <button
              v-for="language in languages"
              :key="language.code"
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm transition hover:bg-white/7"
              :class="
                locale === language.code
                  ? 'bg-primary-container text-primary-container-foreground'
                  : 'text-foreground'
              "
              type="button"
              @click="selectLocale(language.code)"
            >
              <img
                :src="language.flag"
                :alt="`${language.label} flag`"
                class="size-5 rounded-full object-cover"
              /><span>{{ language.label }}</span>
            </button>
          </div>
        </div>
        <Bell class="hidden size-4 sm:block" />
        <NuxtLink
          v-if="!user"
          to="/login"
          aria-label="Đăng nhập"
          class="grid size-9 place-items-center rounded-full border border-white/10 bg-surface-container transition hover:border-primary/60 hover:bg-primary/10 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          ><CircleUserRound class="size-5 text-primary"
        /></NuxtLink>
        <div v-else class="relative">
          <button
            type="button"
            class="flex items-center gap-2 rounded-full border border-white/10 bg-surface-container p-1 transition hover:border-primary/60"
            :aria-expanded="isAccountOpen"
            @click="isAccountOpen = !isAccountOpen"
          >
            <img
              :src="user.avatarUrl || '/default-meme-avatar.png'"
              :alt="user.displayName"
              class="size-7 rounded-full object-cover"
              referrerpolicy="no-referrer"
            />
            <span
              class="hidden max-w-28 truncate pr-2 text-xs font-semibold text-foreground lg:block"
              >{{ user.displayName }}</span
            >
          </button>
          <div
            v-if="isAccountOpen"
            class="absolute right-0 top-[calc(100%+8px)] z-[70] w-56 overflow-hidden rounded-2xl border border-white/10 bg-surface-container p-1.5 shadow-[0_16px_40px_rgba(0,0,0,.42)]"
          >
            <div class="border-b border-white/10 px-3 py-2.5">
              <p class="truncate text-sm font-semibold text-foreground">
                {{ user.displayName }}
              </p>
              <p class="mt-0.5 truncate text-xs text-muted-foreground">
                {{ user.email }}
              </p>
            </div>
            <NuxtLink
              to="/profile"
              class="mt-1 flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm text-muted-foreground transition hover:bg-white/7 hover:text-foreground"
              @click="isAccountOpen = false"
              ><UserRound class="size-4" /> Hồ sơ</NuxtLink
            >
            <button
              type="button"
              class="mt-1 flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-left text-sm text-muted-foreground transition hover:bg-white/7 hover:text-foreground"
              @click="logout"
            >
              <LogOut class="size-4" /> Đăng xuất
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>
