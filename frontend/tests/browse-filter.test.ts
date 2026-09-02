import { describe, expect, it } from "bun:test";
import type { TitleSummary } from "../app/types/catalog";

function filterTitles(
  items: TitleSummary[],
  filters: {
    genre?: string;
    country?: string;
    year?: string;
    format?: string;
  },
) {
  return items.filter((title) => {
    const matchesGenre =
      !filters.genre ||
      filters.genre === "all" ||
      title.genre
        .split(",")
        .map((s) => s.trim())
        .includes(filters.genre);

    const matchesCountry =
      !filters.country ||
      filters.country === "all" ||
      title.country?.trim().toLowerCase() === filters.country.toLowerCase();

    const matchesYear =
      !filters.year ||
      filters.year === "all" ||
      String(title.year) === filters.year;

    const matchesFormat =
      !filters.format ||
      filters.format === "all" ||
      (filters.format === "r2" && title.isR2Hosted) ||
      (filters.format === "movie" && title.type === "movie") ||
      (filters.format === "series" && title.type === "series");

    return matchesGenre && matchesCountry && matchesYear && matchesFormat;
  });
}

describe("Browse multi-criteria filtering", () => {
  const sampleTitles: TitleSummary[] = [
    {
      slug: "big-buck-bunny",
      title: "Big Buck Bunny",
      genre: "Animation, Comedy",
      year: 2024,
      type: "movie",
      posterUrl: "https://example.test/bbb.jpg",
      isR2Hosted: true,
      country: "Âu Mỹ",
    },
    {
      slug: "natra-2",
      title: "Natra 2",
      genre: "Animation, Fantasy",
      year: 2025,
      type: "movie",
      posterUrl: "https://example.test/natra.jpg",
      isR2Hosted: false,
      country: "Trung Quốc",
    },
    {
      slug: "squid-game",
      title: "Squid Game",
      genre: "Drama, Thriller",
      year: 2024,
      type: "series",
      posterUrl: "https://example.test/sg.jpg",
      isR2Hosted: false,
      country: "Hàn Quốc",
    },
  ];

  it("filters correctly by R2 benchmark format", () => {
    const r2Titles = filterTitles(sampleTitles, { format: "r2" });
    expect(r2Titles).toHaveLength(1);
    expect(r2Titles[0].slug).toBe("big-buck-bunny");
  });

  it("filters correctly by country and year", () => {
    const filtered = filterTitles(sampleTitles, {
      country: "Hàn Quốc",
      year: "2024",
    });
    expect(filtered).toHaveLength(1);
    expect(filtered[0].slug).toBe("squid-game");
  });

  it("filters correctly by genre and format", () => {
    const movies = filterTitles(sampleTitles, {
      genre: "Animation",
      format: "movie",
    });
    expect(movies).toHaveLength(2);
  });

  it("returns all when filters are set to all", () => {
    const all = filterTitles(sampleTitles, {
      genre: "all",
      country: "all",
      year: "all",
      format: "all",
    });
    expect(all).toHaveLength(3);
  });
});
