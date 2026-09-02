<script setup lang="ts">
import {
  Activity,
  BarChart3,
  Clock,
  Eye,
  Flame,
  Monitor,
  Smartphone,
  Tablet,
  TrendingUp,
  Tv,
} from "@lucide/vue";
import { onMounted, ref } from "vue";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Phân tích & Thống kê — ZMovie admin" });

const isLoading = ref(true);

const dailyViews = ref([
  { date: "25/08", views: 2450, height: "45%" },
  { date: "26/08", views: 2890, height: "55%" },
  { date: "27/08", views: 3200, height: "62%" },
  { date: "28/08", views: 2780, height: "52%" },
  { date: "29/08", views: 4120, height: "82%" },
  { date: "30/08", views: 4890, height: "95%" },
  { date: "31/08", views: 5120, height: "100%" },
]);

const peakHours = ref([
  { hour: "18:00", percent: 35 },
  { hour: "19:00", percent: 60 },
  { hour: "20:00", percent: 85 },
  { hour: "21:00", percent: 100 },
  { hour: "22:00", percent: 90 },
  { hour: "23:00", percent: 65 },
]);

const devices = ref([
  {
    name: "Máy tính bàn / Laptop",
    share: "54.2%",
    icon: Monitor,
    color: "bg-primary",
  },
  {
    name: "Điện thoại thông minh (iOS & Android)",
    share: "38.5%",
    icon: Smartphone,
    color: "bg-sky-400",
  },
  {
    name: "Máy tính bảng & Smart TV",
    share: "7.3%",
    icon: Tv,
    color: "bg-amber-400",
  },
]);

const topTitles = ref([
  {
    rank: 1,
    title: "Sintel (Bản dựng 4K R2)",
    views: "14.280",
    category: "Phim demo R2",
    score: "9.8",
  },
  {
    rank: 2,
    title: "Big Buck Bunny (Ultra HD)",
    views: "11.450",
    category: "Phim demo R2",
    score: "9.5",
  },
  {
    rank: 3,
    title: "Tears of Steel (VFX Edition)",
    views: "9.820",
    category: "Phim demo R2",
    score: "9.4",
  },
  {
    rank: 4,
    title: "Dune: Hành Tinh Cát - Phần 2",
    views: "8.640",
    category: "Kho NguonC",
    score: "9.1",
  },
  {
    rank: 5,
    title: "Oppenheimer",
    views: "7.310",
    category: "Kho NguonC",
    score: "9.0",
  },
]);

onMounted(() => {
  setTimeout(() => {
    isLoading.value = false;
  }, 300);
});
</script>

<template>
  <div class="space-y-8">
    <AdminPageHeader
      title="Phân tích & Thống kê"
      description="Biểu đồ chuyên sâu về lưu lượng xem phim, phân bố thiết bị và khung giờ cao điểm."
    />

    <!-- Highlight Metrics -->
    <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <AdminStatCard
        label="Tổng giờ xem tuần qua"
        value="18.940 giờ"
        hint="+14.5% so với tuần trước"
      />
      <AdminStatCard
        label="Lượt xem trung bình / ngày"
        value="3.648"
        hint="Cao nhất vào Thứ 7 và Chủ nhật"
      />
      <AdminStatCard
        label="Thời gian xem trung bình"
        value="42 phút / phiên"
        hint="Tỉ lệ hoàn thành tập đạt 78%"
      />
      <AdminStatCard
        label="Tỉ lệ sử dụng máy chủ R2"
        value="82.4%"
        hint="Trải nghiệm phát tức thì, 0 buffering"
      />
    </section>

    <!-- Daily Views Chart -->
    <div
      class="rounded-3xl border border-white/10 bg-surface-container-lowest p-6"
    >
      <div
        class="flex items-center justify-between border-b border-white/8 pb-4"
      >
        <div>
          <h3
            class="font-display text-base font-semibold text-foreground flex items-center gap-2"
          >
            <TrendingUp class="size-4 text-primary" />
            <span>Xu hướng lượt xem 7 ngày qua</span>
          </h3>
          <p class="text-xs text-muted-foreground mt-0.5">
            Thống kê lưu lượng streaming trên toàn hệ thống
          </p>
        </div>
        <span
          class="rounded-full bg-primary/20 px-3 py-1 text-xs font-bold text-primary border border-primary/30"
        >
          +22.8% Tăng trưởng
        </span>
      </div>

      <!-- Custom CSS Bar Chart -->
      <div
        class="mt-6 flex h-60 items-end justify-between gap-2 sm:gap-6 pt-8 pb-2"
      >
        <div
          v-for="item in dailyViews"
          :key="item.date"
          class="group flex flex-1 flex-col items-center gap-2 h-full justify-end"
        >
          <div class="relative w-full flex justify-center">
            <span
              class="absolute -top-7 text-[11px] font-bold text-primary opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap"
            >
              {{ item.views.toLocaleString("vi-VN") }}
            </span>
          </div>
          <div
            class="w-full max-w-[48px] rounded-t-xl bg-gradient-to-t from-primary/40 to-primary transition-all duration-500 group-hover:brightness-125"
            :style="{ height: item.height }"
          />
          <span
            class="text-xs text-muted-foreground group-hover:text-foreground transition-colors"
          >
            {{ item.date }}
          </span>
        </div>
      </div>
    </div>

    <!-- Secondary Charts Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Peak Hours -->
      <div
        class="rounded-3xl border border-white/10 bg-surface-container-lowest p-6"
      >
        <h3
          class="font-display text-base font-semibold text-foreground flex items-center gap-2"
        >
          <Clock class="size-4 text-amber-400" />
          <span>Khung giờ cao điểm xem phim (Peak Hours)</span>
        </h3>
        <p class="text-xs text-muted-foreground mt-0.5">
          Khung giờ 20:00 - 22:00 ghi nhận lưu lượng truy cập cao nhất
        </p>

        <div class="mt-6 space-y-3.5">
          <div v-for="peak in peakHours" :key="peak.hour" class="space-y-1.5">
            <div class="flex justify-between text-xs">
              <span class="font-medium text-foreground">{{ peak.hour }}</span>
              <span class="text-muted-foreground"
                >{{ peak.percent }}% công suất</span
              >
            </div>
            <div
              class="h-2 w-full overflow-hidden rounded-full bg-surface-container"
            >
              <div
                class="h-full rounded-full bg-gradient-to-r from-amber-500 to-amber-300 transition-all duration-700"
                :style="{ width: `${peak.percent}%` }"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Device Distribution -->
      <div
        class="rounded-3xl border border-white/10 bg-surface-container-lowest p-6"
      >
        <h3
          class="font-display text-base font-semibold text-foreground flex items-center gap-2"
        >
          <Activity class="size-4 text-sky-400" />
          <span>Phân bố thiết bị người dùng</span>
        </h3>
        <p class="text-xs text-muted-foreground mt-0.5">
          Tỉ lệ người xem qua nền tảng Web Desktop và Mobile
        </p>

        <div class="mt-6 space-y-4">
          <div
            v-for="d in devices"
            :key="d.name"
            class="flex items-center justify-between rounded-2xl bg-surface-container p-3.5 border border-white/5"
          >
            <div class="flex items-center gap-3">
              <div
                class="grid size-9 place-items-center rounded-xl bg-white/5 text-foreground"
              >
                <component :is="d.icon" class="size-4.5" />
              </div>
              <span class="text-xs font-medium text-foreground">{{
                d.name
              }}</span>
            </div>
            <span class="font-display text-sm font-bold text-foreground">{{
              d.share
            }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Top Movies Table -->
    <div
      class="rounded-3xl border border-white/10 bg-surface-container-lowest p-6"
    >
      <h3
        class="font-display text-base font-semibold text-foreground flex items-center gap-2"
      >
        <Flame class="size-4 text-rose-500" />
        <span>Top 5 phim có lượt xem cao nhất tuần</span>
      </h3>
      <div class="mt-4 overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead>
            <tr class="border-b border-white/10 text-muted-foreground">
              <th class="pb-3 font-semibold w-16">Hạng</th>
              <th class="pb-3 font-semibold">Tựa phim</th>
              <th class="pb-3 font-semibold">Nguồn máy chủ</th>
              <th class="pb-3 font-semibold">Lượt xem</th>
              <th class="pb-3 font-semibold">Điểm đánh giá</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/5">
            <tr
              v-for="t in topTitles"
              :key="t.rank"
              class="hover:bg-white/2 transition"
            >
              <td
                class="py-3.5 font-bold font-display"
                :class="t.rank === 1 ? 'text-amber-400' : 'text-foreground'"
              >
                #{{ t.rank }}
              </td>
              <td class="py-3.5 font-semibold text-foreground">
                {{ t.title }}
              </td>
              <td class="py-3.5">
                <span
                  class="rounded-full px-2.5 py-0.5 text-[10px] font-semibold"
                  :class="
                    t.category.includes('R2')
                      ? 'bg-primary/20 text-primary border border-primary/30'
                      : 'bg-surface-container text-muted-foreground'
                  "
                >
                  {{ t.category }}
                </span>
              </td>
              <td class="py-3.5 font-mono text-foreground">{{ t.views }}</td>
              <td class="py-3.5 font-semibold text-amber-400">
                {{ t.score }} / 10
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
