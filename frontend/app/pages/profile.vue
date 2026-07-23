<script setup lang="ts">
import { BadgeCheck, Film, LogOut, Mail, ShieldCheck } from "@lucide/vue";

useHead({ title: "Hồ sơ — ZMovie" });

type User = {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
};
const user = ref<User | null>(null);
const loading = ref(true);
const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const { $api } = useNuxtApp();

async function logout() {
  await $api("/v1/auth/logout", { method: "POST", credentials: "include" });
  await navigateTo("/login");
}

onMounted(async () => {
  try {
    user.value = await $api<User>("/v1/auth/me", { credentials: "include" });
  } catch {
    await navigateTo("/login");
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" />
    <section v-if="loading" class="mx-auto max-w-240 px-5 py-20 lg:px-12">
      <div class="h-56 animate-pulse rounded-3xl bg-surface-container" />
    </section>
    <section
      v-else-if="user"
      class="mx-auto max-w-240 px-5 py-12 lg:px-12 lg:py-16"
    >
      <p class="text-xs font-semibold uppercase tracking-[.18em] text-primary">
        Tài khoản
      </p>
      <h1
        class="font-display mt-3 text-5xl font-semibold tracking-[-.04em] lg:text-6xl"
      >
        Hồ sơ của bạn
      </h1>
      <p class="mt-4 max-w-xl text-muted-foreground">
        Quản lý thông tin và trải nghiệm ZMovie của bạn.
      </p>

      <div class="mt-10 grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <section
          class="overflow-hidden rounded-3xl border border-white/10 bg-surface-container"
        >
          <div
            class="bg-[radial-gradient(circle_at_top_right,rgba(255,181,157,.2),transparent_35%),linear-gradient(120deg,#2a1c1a,#201f1f_60%)] px-6 py-8 sm:px-8"
          >
            <div class="flex flex-col gap-5 sm:flex-row sm:items-center">
              <img
                :src="user.avatarUrl || '/default-meme-avatar.png'"
                :alt="user.displayName"
                class="size-22 rounded-3xl border-2 border-white/20 object-cover shadow-xl"
                referrerpolicy="no-referrer"
              />
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="font-display text-3xl font-semibold">
                    {{ user.displayName }}
                  </h2>
                  <BadgeCheck class="size-5 text-primary" />
                </div>
                <p class="mt-2 text-sm text-white/60">Thành viên ZMovie</p>
              </div>
            </div>
          </div>
          <div class="divide-y divide-white/8 px-6 sm:px-8">
            <div class="flex items-center gap-4 py-5">
              <span
                class="grid size-10 place-items-center rounded-xl bg-primary/10 text-primary"
                ><Mail class="size-5"
              /></span>
              <div>
                <p
                  class="text-xs font-medium uppercase tracking-[.12em] text-tertiary"
                >
                  Email
                </p>
                <p class="mt-1 text-sm text-foreground">{{ user.email }}</p>
              </div>
            </div>
            <div class="flex items-center gap-4 py-5">
              <span
                class="grid size-10 place-items-center rounded-xl bg-primary/10 text-primary"
                ><ShieldCheck class="size-5"
              /></span>
              <div>
                <p
                  class="text-xs font-medium uppercase tracking-[.12em] text-tertiary"
                >
                  Phương thức đăng nhập
                </p>
                <p class="mt-1 text-sm text-foreground">Google · Đã xác thực</p>
              </div>
            </div>
          </div>
        </section>

        <aside class="space-y-5">
          <div
            class="rounded-3xl border border-white/10 bg-surface-container p-6"
          >
            <Film class="size-6 text-primary" />
            <h2 class="font-display mt-5 text-2xl font-semibold">
              Rạp phim của bạn
            </h2>
            <p class="mt-2 text-sm leading-6 text-muted-foreground">
              Lưu phim yêu thích và tiếp tục hành trình xem bất cứ lúc nào.
            </p>
            <NuxtLink
              to="/my-list"
              class="mt-5 inline-flex text-sm font-semibold text-primary transition hover:text-primary-container"
              >Mở danh sách của tôi →</NuxtLink
            >
          </div>
          <button
            type="button"
            class="flex w-full items-center justify-center gap-2 rounded-2xl border border-white/12 px-4 py-3 text-sm font-semibold text-muted-foreground transition hover:border-destructive/50 hover:bg-destructive/10 hover:text-destructive"
            @click="logout"
          >
            <LogOut class="size-4" /> Đăng xuất
          </button>
        </aside>
      </div>
    </section>
  </main>
</template>
