<script setup lang="ts">
import {
  Bookmark,
  Bot,
  ChevronDown,
  Film,
  Globe,
  Layers,
  LogOut,
  ShieldCheck,
  Tv,
  User,
  X,
} from "@lucide/vue";

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  close: [];
}>();

const { user, isAdmin, signOut } = useAuthSession();

const isTheLoaiOpen = ref(false);
const isQuocGiaOpen = ref(false);
const isThemOpen = ref(false);

const theLoaiList = [
  { name: "Hành Động", slug: "hanh-dong" },
  { name: "Cổ Trang", slug: "co-trang" },
  { name: "Hài Hước", slug: "hai-huoc" },
  { name: "Tình Cảm", slug: "tinh-cam" },
  { name: "Kinh Dị", slug: "kinh-di" },
  { name: "Viễn Tưởng", slug: "vien-tuong" },
  { name: "Tâm Lý", slug: "tam-ly" },
  { name: "Hình Sự", slug: "hinh-su" },
  { name: "Hoạt Hình", slug: "hoat-hinh" },
  { name: "Võ Thuật", slug: "vo-thuat" },
];

const quocGiaList = [
  { name: "Trung Quốc", slug: "trung-quoc" },
  { name: "Hàn Quốc", slug: "han-quoc" },
  { name: "Âu Mỹ", slug: "au-my" },
  { name: "Nhật Bản", slug: "nhat-ban" },
  { name: "Thái Lan", slug: "thai-lan" },
  { name: "Việt Nam", slug: "viet-nam" },
  { name: "Hồng Kông", slug: "hong-kong" },
];

async function handleLogout() {
  await signOut();
  emit("close");
  await navigateTo("/");
}
</script>

<template>
  <Teleport to="body">
    <!-- Backdrop overlay -->
    <Transition
      enter-active-class="transition-opacity duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm"
        @click="emit('close')"
      />
    </Transition>

    <!-- Slide-out Drawer -->
    <Transition
      enter-active-class="transition-transform duration-300 ease-out"
      enter-from-class="-translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition-transform duration-250 ease-in"
      leave-from-class="translate-x-0"
      leave-to-class="-translate-x-full"
    >
      <aside
        v-if="isOpen"
        class="fixed inset-y-0 left-0 z-50 flex w-full max-w-[320px] flex-col border-r border-white/10 bg-[#191b24] p-5 text-white shadow-2xl overflow-y-auto"
      >
        <!-- Top row with close icon -->
        <div
          class="flex items-center justify-between pb-4 border-b border-white/10"
        >
          <ZMovieLogo />
          <button
            type="button"
            class="grid size-9 place-items-center rounded-full bg-white/5 text-gray-300 hover:bg-white/10 hover:text-white transition"
            aria-label="Đóng menu"
            @click="emit('close')"
          >
            <X class="size-5" />
          </button>
        </div>

        <!-- Thành viên (Account / Auth CTA button) -->
        <div class="mt-5">
          <NuxtLink
            v-if="!user"
            to="/login"
            class="flex w-full items-center justify-center gap-2.5 rounded-2xl bg-white/15 px-4 py-3 text-sm font-bold text-white transition hover:bg-white/20 active:scale-[0.98]"
            @click="emit('close')"
          >
            <User class="size-4.5" />
            <span>Thành viên</span>
          </NuxtLink>
          <div v-else class="rounded-2xl border border-white/10 bg-white/5 p-3">
            <div class="flex items-center gap-3">
              <img
                :src="user.avatarUrl || '/default-meme-avatar.png'"
                :alt="user.displayName"
                class="size-10 rounded-full object-cover border border-primary/50"
              />
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-bold text-white">
                  {{ user.displayName }}
                </p>
                <p class="truncate text-xs text-muted-foreground">
                  {{ user.email }}
                </p>
              </div>
            </div>
            <div class="mt-3 grid grid-cols-2 gap-2 text-xs font-semibold">
              <NuxtLink
                to="/profile"
                class="flex items-center justify-center gap-1.5 rounded-xl bg-white/10 py-2 hover:bg-white/15"
                @click="emit('close')"
              >
                Hồ sơ
              </NuxtLink>
              <button
                type="button"
                class="flex items-center justify-center gap-1.5 rounded-xl bg-red-500/20 text-red-300 py-2 hover:bg-red-500/30"
                @click="handleLogout"
              >
                <LogOut class="size-3.5" /> Đăng xuất
              </button>
            </div>
          </div>
        </div>

        <!-- Main Navigation Links -->
        <nav class="mt-6 flex flex-col gap-1.5 text-sm font-medium">
          <!-- Thể loại Dropdown -->
          <div>
            <button
              type="button"
              class="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
              @click="isTheLoaiOpen = !isTheLoaiOpen"
            >
              <span class="flex items-center gap-3">
                <Layers class="size-4 text-primary" />
                <span>Thể loại</span>
              </span>
              <ChevronDown
                class="size-4 transition-transform duration-200"
                :class="{ 'rotate-180 text-primary': isTheLoaiOpen }"
              />
            </button>
            <div
              v-if="isTheLoaiOpen"
              class="ml-8 mt-1 grid grid-cols-2 gap-1 border-l border-white/10 pl-3 py-1"
            >
              <NuxtLink
                v-for="item in theLoaiList"
                :key="item.slug"
                :to="`/browse?genre=${item.name}`"
                class="rounded-lg px-2 py-1.5 text-xs text-white/70 hover:bg-white/10 hover:text-white"
                @click="emit('close')"
              >
                {{ item.name }}
              </NuxtLink>
            </div>
          </div>

          <!-- Phim Lẻ -->
          <NuxtLink
            to="/browse?type=single"
            class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
            @click="emit('close')"
          >
            <Film class="size-4 text-primary" />
            <span>Phim Lẻ</span>
          </NuxtLink>

          <!-- Phim Bộ -->
          <NuxtLink
            to="/browse?type=series"
            class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
            @click="emit('close')"
          >
            <Tv class="size-4 text-primary" />
            <span>Phim Bộ</span>
          </NuxtLink>

          <!-- Quốc gia Dropdown -->
          <div>
            <button
              type="button"
              class="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
              @click="isQuocGiaOpen = !isQuocGiaOpen"
            >
              <span class="flex items-center gap-3">
                <Globe class="size-4 text-primary" />
                <span>Quốc gia</span>
              </span>
              <ChevronDown
                class="size-4 transition-transform duration-200"
                :class="{ 'rotate-180 text-primary': isQuocGiaOpen }"
              />
            </button>
            <div
              v-if="isQuocGiaOpen"
              class="ml-8 mt-1 grid grid-cols-2 gap-1 border-l border-white/10 pl-3 py-1"
            >
              <NuxtLink
                v-for="item in quocGiaList"
                :key="item.slug"
                :to="`/browse?country=${item.name}`"
                class="rounded-lg px-2 py-1.5 text-xs text-white/70 hover:bg-white/10 hover:text-white"
                @click="emit('close')"
              >
                {{ item.name }}
              </NuxtLink>
            </div>
          </div>

          <!-- Danh sách của tôi -->
          <NuxtLink
            to="/my-list"
            class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
            @click="emit('close')"
          >
            <Bookmark class="size-4 text-primary" />
            <span>Danh sách của tôi</span>
          </NuxtLink>

          <!-- Thêm Dropdown -->
          <div>
            <button
              type="button"
              class="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-white/90 hover:bg-white/10 hover:text-primary transition"
              @click="isThemOpen = !isThemOpen"
            >
              <span class="flex items-center gap-3">
                <Bot class="size-4 text-primary" />
                <span>Thêm</span>
              </span>
              <ChevronDown
                class="size-4 transition-transform duration-200"
                :class="{ 'rotate-180 text-primary': isThemOpen }"
              />
            </button>
            <div
              v-if="isThemOpen"
              class="ml-8 mt-1 flex flex-col gap-1 border-l border-white/10 pl-3 py-1"
            >
              <NuxtLink
                to="/assistant"
                class="flex items-center gap-2 rounded-lg px-2 py-1.5 text-xs text-white/70 hover:bg-white/10 hover:text-white"
                @click="emit('close')"
              >
                <Bot class="size-3.5" /> AI Movie Bot
              </NuxtLink>
              <NuxtLink
                v-if="isAdmin"
                to="/admin"
                class="flex items-center gap-2 rounded-lg px-2 py-1.5 text-xs text-amber-300 hover:bg-white/10"
                @click="emit('close')"
              >
                <ShieldCheck class="size-3.5" /> Quản trị ZMovie
              </NuxtLink>
            </div>
          </div>
        </nav>

        <!-- Footer in Drawer -->
        <div
          class="mt-auto pt-6 text-center text-xs text-muted-foreground border-t border-white/10"
        >
          <p class="text-primary/80 font-medium">ZMovie</p>
          <p class="mt-1 text-[11px] opacity-60">Phiên bản 2026</p>
        </div>
      </aside>
    </Transition>
  </Teleport>
</template>
