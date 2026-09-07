namespace ZMovie.Domain.Engagement;

public static class UserLevelPolicy
{
    public static (int Level, string Title, int CurrentExp, int NextLevelExp) Calculate(int totalExp)
    {
        var exp = Math.Max(0, totalExp);
        return exp switch
        {
            < 100 => (1, "Tân thủ", exp, 100),
            < 500 => (2, "Mọt phim", exp, 500),
            < 1500 => (3, "Ghiền phim", exp, 1500),
            < 4000 => (4, "Đại sư điện ảnh", exp, 4000),
            < 10000 => (5, "Huyền thoại ZMovie", exp, 10000),
            _ => (6, "Chúa tể Rạp chiếu", exp, 10000)
        };
    }
}

public static class BilibiliLevelCalculator
{
    public static (int Level, string Title, int CurrentExp, int NextLevelExp) Calculate(int totalExp) =>
        UserLevelPolicy.Calculate(totalExp);
}
