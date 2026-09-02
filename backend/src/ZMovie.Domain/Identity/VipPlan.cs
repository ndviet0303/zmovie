namespace ZMovie.Domain.Identity;

public sealed record VipPlan(string Code, string Name, decimal PriceVnd, int DurationMonths, string Description)
{
    public static readonly VipPlan Monthly = new("VIP1", "VIP 1 Tháng", 49000m, 1, "Trải nghiệm chuẩn 4K HDR, không quảng cáo và phòng chiếu VIP.");
    public static readonly VipPlan Quarterly = new("VIP3", "VIP 3 Tháng", 129000m, 3, "Tiết kiệm 15% cùng huy hiệu VIP Bilibili độc quyền.");
    public static readonly VipPlan Yearly = new("VIP12", "VIP 1 Năm", 499000m, 12, "Gói tối ưu nhất, tặng 2 tháng miễn phí và quyền bỏ phiếu phim.");

    public static IReadOnlyList<VipPlan> All => [Monthly, Quarterly, Yearly];

    public static VipPlan? FromCode(string code)
    {
        return All.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}
