import { computed, ref } from "vue";
import { useAuth } from "~/composables/useAuth";

export type VipPlanTier = "VIP1" | "VIP3" | "VIP12";

export interface VipPlanInfo {
  tier: VipPlanTier;
  name: string;
  price: string;
  amount: number;
  months: number;
  badge?: string;
  description: string;
}

export const VIP_PLANS: VipPlanInfo[] = [
  {
    tier: "VIP1",
    name: "VIP 1 Tháng",
    price: "49.000đ",
    amount: 49000,
    months: 1,
    description:
      "Trải nghiệm chuẩn 4K HDR, không quảng cáo và phòng chiếu VIP.",
  },
  {
    tier: "VIP3",
    name: "VIP 3 Tháng",
    price: "129.000đ",
    amount: 129000,
    months: 3,
    badge: "Tiết kiệm 15%",
    description:
      "Tặng huy hiệu VIP Bilibili và ưu tiên băng thông giờ cao điểm.",
  },
  {
    tier: "VIP12",
    name: "VIP 1 Năm",
    price: "499.000đ",
    amount: 499000,
    months: 12,
    badge: "Tiết kiệm 25%",
    description:
      "Gói tối ưu nhất, tặng 2 tháng miễn phí và quyền bình chọn phim mới.",
  },
];

export function useVipCheckout() {
  const { user } = useAuth();
  const selectedTier = ref<VipPlanTier>("VIP3");
  const isCheckingOut = ref(false);
  const checkoutError = ref("");
  const isPaid = ref(false);

  const selectedPlan = computed(() => {
    return VIP_PLANS.find((p) => p.tier === selectedTier.value) || VIP_PLANS[1];
  });

  const transferContent = computed(() => {
    const uid = user.value?.id || "guest";
    return `ZMVIP_${uid}_${selectedTier.value}`;
  });

  const vietQrUrl = computed(() => {
    const plan = selectedPlan.value;
    const content = encodeURIComponent(transferContent.value);
    const accountName = encodeURIComponent("CONG TY ZMOVIE VIETNAM");
    return `https://img.vietqr.io/image/MB-0988888888-compact2.png?amount=${plan.amount}&addInfo=${content}&accountName=${accountName}`;
  });

  function selectTier(tier: VipPlanTier) {
    selectedTier.value = tier;
    isPaid.value = false;
    checkoutError.value = "";
  }

  return {
    selectedTier,
    selectedPlan,
    transferContent,
    vietQrUrl,
    isCheckingOut,
    checkoutError,
    isPaid,
    selectTier,
  };
}
