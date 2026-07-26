<script setup lang="ts">
import type { AdminOverview } from "~/types/admin";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Tổng quan — ZMovie admin" });

const { $api } = useNuxtApp();
const overview = ref<AdminOverview | null>(null);
const pending = ref(true);
const errorMessage = ref("");

async function load() {
  pending.value = true;
  errorMessage.value = "";
  try {
    overview.value = await $api<AdminOverview>("/v1/admin/overview", {
      credentials: "include",
    });
  } catch {
    errorMessage.value = "Không tải được số liệu tổng quan.";
  } finally {
    pending.value = false;
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString("vi-VN");
}

onMounted(() => void load());
</script>

<template>
  <div class="space-y-8">
    <AdminPageHeader
      title="Tổng quan"
      description="Số liệu catalog, người dùng và lượt xem của ZMovie."
    >
      <template #actions>
        <Button size="sm" variant="outline" :disabled="pending" @click="load">
          Làm mới
        </Button>
      </template>
    </AdminPageHeader>

    <p
      v-if="errorMessage"
      class="rounded-2xl bg-destructive/10 px-4 py-3 text-sm text-destructive"
    >
      {{ errorMessage }}
    </p>

    <p v-else-if="pending" class="text-sm text-muted-foreground">Đang tải…</p>

    <template v-else-if="overview">
      <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <AdminStatCard
          label="Phim"
          :value="overview.titleCount"
          :hint="`${overview.movieCount.toLocaleString('vi-VN')} phim lẻ · ${overview.seriesCount.toLocaleString('vi-VN')} phim bộ`"
        />
        <AdminStatCard
          label="Tập phim"
          :value="overview.episodeCount"
          :hint="`${overview.genreCount.toLocaleString('vi-VN')} thể loại`"
        />
        <AdminStatCard
          label="Người dùng"
          :value="overview.userCount"
          :hint="`${overview.adminCount.toLocaleString('vi-VN')} quản trị viên`"
        />
        <AdminStatCard
          label="Nổi bật"
          :value="overview.featuredCount"
          hint="Phim được gắn featured"
        />
        <AdminStatCard
          label="Lượt xem 24 giờ"
          :value="overview.viewsLast24Hours"
        />
        <AdminStatCard
          label="Lượt xem 7 ngày"
          :value="overview.viewsLast7Days"
        />
        <AdminStatCard
          label="Đánh giá"
          :value="overview.reviewCount"
          :hint="`Điểm trung bình ${overview.averageRating}/10`"
        />
        <AdminStatCard
          label="Điểm trung bình"
          :value="`${overview.averageRating}/10`"
        />
      </section>

      <section class="grid gap-6 xl:grid-cols-2">
        <div class="rounded-2xl border border-white/10 bg-surface-container">
          <h2
            class="border-b border-white/10 px-5 py-4 font-display text-lg font-bold"
          >
            Xem nhiều nhất 7 ngày
          </h2>
          <ul v-if="overview.topTitles.length" class="divide-y divide-white/5">
            <li
              v-for="item in overview.topTitles"
              :key="item.slug"
              class="flex items-center gap-4 px-5 py-3"
            >
              <img
                :src="item.posterUrl"
                :alt="item.title"
                loading="lazy"
                class="h-14 w-10 shrink-0 rounded-lg object-cover"
              />
              <NuxtLink
                :to="`/movies/${item.slug}`"
                class="min-w-0 flex-1 truncate text-sm font-semibold transition hover:text-primary"
              >
                {{ item.title }}
              </NuxtLink>
              <span class="text-sm font-bold text-primary">
                {{ item.views.toLocaleString("vi-VN") }}
              </span>
            </li>
          </ul>
          <p v-else class="px-5 py-6 text-sm text-muted-foreground">
            Chưa có lượt xem nào trong 7 ngày qua.
          </p>
        </div>

        <div class="rounded-2xl border border-white/10 bg-surface-container">
          <h2
            class="border-b border-white/10 px-5 py-4 font-display text-lg font-bold"
          >
            Người dùng mới
          </h2>
          <ul
            v-if="overview.recentUsers.length"
            class="divide-y divide-white/5"
          >
            <li
              v-for="item in overview.recentUsers"
              :key="item.id"
              class="flex items-center gap-3 px-5 py-3"
            >
              <img
                v-if="item.avatarUrl"
                :src="item.avatarUrl"
                :alt="item.displayName"
                loading="lazy"
                referrerpolicy="no-referrer"
                class="size-9 shrink-0 rounded-full object-cover"
              />
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-semibold">
                  {{ item.displayName }}
                </p>
                <p class="truncate text-xs text-muted-foreground">
                  {{ item.email }}
                </p>
              </div>
              <Badge :variant="item.role === 'admin' ? 'default' : 'outline'">
                {{ item.role === "admin" ? "Admin" : "Thành viên" }}
              </Badge>
              <span class="hidden text-xs text-muted-foreground sm:block">
                {{ formatDate(item.createdAt) }}
              </span>
            </li>
          </ul>
          <p v-else class="px-5 py-6 text-sm text-muted-foreground">
            Chưa có người dùng nào.
          </p>
        </div>
      </section>
    </template>
  </div>
</template>
