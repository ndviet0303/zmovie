<script setup lang="ts">
import {
  ArrowLeft,
  Film,
  Gauge,
  LogOut,
  MessageSquare,
  Tags,
  Users,
} from "@lucide/vue";

const route = useRoute();
const { user, signOut } = useAuthSession();

const navItems = [
  { label: "Tổng quan", to: "/admin", icon: Gauge },
  { label: "Phim", to: "/admin/titles", icon: Film },
  { label: "Người dùng", to: "/admin/users", icon: Users },
  { label: "Đánh giá", to: "/admin/reviews", icon: MessageSquare },
  { label: "Thể loại", to: "/admin/genres", icon: Tags },
] as const;

function isActive(to: string) {
  return to === "/admin" ? route.path === "/admin" : route.path.startsWith(to);
}

async function logout() {
  await signOut();
  await navigateTo("/");
}
</script>

<template>
  <div class="min-h-screen bg-background text-foreground lg:flex">
    <aside
      class="border-b border-white/10 bg-surface-container-lowest lg:min-h-screen lg:w-64 lg:shrink-0 lg:border-b-0 lg:border-r"
    >
      <div class="flex items-center justify-between gap-3 px-5 py-5 lg:px-6">
        <NuxtLink
          to="/admin"
          class="font-display text-xl font-extrabold tracking-tight text-primary"
        >
          ZMovie<span class="ml-1 text-sm font-bold text-muted-foreground"
            >admin</span
          >
        </NuxtLink>
        <NuxtLink
          to="/"
          class="inline-flex items-center gap-1 text-xs font-semibold text-muted-foreground transition hover:text-primary lg:hidden"
        >
          <ArrowLeft class="size-3.5" /> Về site
        </NuxtLink>
      </div>

      <nav
        class="flex gap-1 overflow-x-auto px-3 pb-3 lg:flex-col lg:gap-1 lg:overflow-visible lg:px-3"
      >
        <NuxtLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="inline-flex shrink-0 items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-semibold transition"
          :class="
            isActive(item.to)
              ? 'bg-primary/10 text-primary'
              : 'text-muted-foreground hover:bg-surface-container hover:text-foreground'
          "
        >
          <component :is="item.icon" class="size-4" />
          {{ item.label }}
        </NuxtLink>
      </nav>

      <div class="hidden px-3 pb-5 lg:mt-auto lg:block">
        <NuxtLink
          to="/"
          class="inline-flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-semibold text-muted-foreground transition hover:bg-surface-container hover:text-foreground"
        >
          <ArrowLeft class="size-4" /> Về site
        </NuxtLink>
      </div>
    </aside>

    <div class="min-w-0 flex-1">
      <header
        class="flex items-center justify-between gap-4 border-b border-white/10 px-5 py-4 lg:px-8"
      >
        <p class="text-sm text-muted-foreground">Khu vực quản trị</p>
        <div class="flex items-center gap-3">
          <div class="text-right">
            <p class="text-sm font-bold leading-tight">
              {{ user?.displayName ?? "—" }}
            </p>
            <p class="text-xs leading-tight text-muted-foreground">
              {{ user?.email ?? "" }}
            </p>
          </div>
          <img
            v-if="user?.avatarUrl"
            :src="user.avatarUrl"
            :alt="user.displayName"
            referrerpolicy="no-referrer"
            class="size-9 rounded-full border border-white/10 object-cover"
          />
          <button
            type="button"
            aria-label="Đăng xuất"
            title="Đăng xuất"
            class="grid size-9 place-items-center rounded-xl border border-white/10 bg-surface-container text-muted-foreground transition hover:border-destructive/40 hover:text-destructive"
            @click="logout"
          >
            <LogOut class="size-4" />
          </button>
        </div>
      </header>

      <main class="px-5 py-7 lg:px-8 lg:py-9">
        <slot />
      </main>
    </div>
  </div>
</template>
