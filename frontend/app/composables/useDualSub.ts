import { ref } from "vue";

export type SubtitleCue = {
  start: number;
  end: number;
  text: string;
};

export type VocabularyDefinition = {
  word: string;
  phonetic?: string;
  partOfSpeech?: string;
  translation: string;
};

// Common built-in vocabulary definitions for cinema/everyday learning
const LOCAL_DICTIONARY: Record<string, VocabularyDefinition> = {
  hello: {
    word: "hello",
    phonetic: "/həˈləʊ/",
    partOfSpeech: "exclamation",
    translation: "xin chào",
  },
  goodbye: {
    word: "goodbye",
    phonetic: "/ɡʊdˈbaɪ/",
    partOfSpeech: "exclamation",
    translation: "tạm biệt",
  },
  movie: {
    word: "movie",
    phonetic: "/ˈmuːvi/",
    partOfSpeech: "noun",
    translation: "bộ phim",
  },
  hero: {
    word: "hero",
    phonetic: "/ˈhɪərəʊ/",
    partOfSpeech: "noun",
    translation: "người hùng, anh hùng",
  },
  danger: {
    word: "danger",
    phonetic: "/ˈdeɪndʒə/",
    partOfSpeech: "noun",
    translation: "mối nguy hiểm",
  },
  secret: {
    word: "secret",
    phonetic: "/ˈsiːkrət/",
    partOfSpeech: "noun/adj",
    translation: "bí mật",
  },
  time: {
    word: "time",
    phonetic: "/taɪm/",
    partOfSpeech: "noun",
    translation: "thời gian",
  },
  world: {
    word: "world",
    phonetic: "/wɜːld/",
    partOfSpeech: "noun",
    translation: "thế giới",
  },
  friend: {
    word: "friend",
    phonetic: "/frend/",
    partOfSpeech: "noun",
    translation: "bạn bè",
  },
  love: {
    word: "love",
    phonetic: "/lʌv/",
    partOfSpeech: "noun/verb",
    translation: "tình yêu, yêu",
  },
  truth: {
    word: "truth",
    phonetic: "/truːθ/",
    partOfSpeech: "noun",
    translation: "sự thật",
  },
  destiny: {
    word: "destiny",
    phonetic: "/ˈdestɪni/",
    partOfSpeech: "noun",
    translation: "định mệnh, số phận",
  },
  power: {
    word: "power",
    phonetic: "/ˈpaʊə/",
    partOfSpeech: "noun",
    translation: "sức mạnh, quyền năng",
  },
  fight: {
    word: "fight",
    phonetic: "/faɪt/",
    partOfSpeech: "verb/noun",
    translation: "chiến đấu",
  },
  victory: {
    word: "victory",
    phonetic: "/ˈvɪktəri/",
    partOfSpeech: "noun",
    translation: "chiến thắng",
  },
};

export function parseWebVtt(vttText: string): SubtitleCue[] {
  const lines = vttText.split(/\r?\n/);
  const cues: SubtitleCue[] = [];
  let currentStart = -1;
  let currentEnd = -1;
  let currentTextLines: string[] = [];

  const timeRegex =
    /(?:(\d{2}):)?(\d{2}):(\d{2})\.(\d{3})\s*-->\s*(?:(\d{2}):)?(\d{2}):(\d{2})\.(\d{3})/;

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line || line.startsWith("WEBVTT") || line.startsWith("NOTE")) {
      if (currentStart >= 0 && currentEnd >= 0 && currentTextLines.length > 0) {
        cues.push({
          start: currentStart,
          end: currentEnd,
          text: currentTextLines.join(" ").replace(/<[^>]+>/g, ""),
        });
        currentStart = -1;
        currentEnd = -1;
        currentTextLines = [];
      }
      continue;
    }

    const match = line.match(timeRegex);
    if (match) {
      const h1 = parseInt(match[1] || "0", 10);
      const m1 = parseInt(match[2], 10);
      const s1 = parseInt(match[3], 10);
      const ms1 = parseInt(match[4], 10);

      const h2 = parseInt(match[5] || "0", 10);
      const m2 = parseInt(match[6], 10);
      const s2 = parseInt(match[7], 10);
      const ms2 = parseInt(match[8], 10);

      currentStart = h1 * 3600 + m1 * 60 + s1 + ms1 / 1000;
      currentEnd = h2 * 3600 + m2 * 60 + s2 + ms2 / 1000;
    } else if (currentStart >= 0) {
      currentTextLines.push(line);
    }
  }

  if (currentStart >= 0 && currentEnd >= 0 && currentTextLines.length > 0) {
    cues.push({
      start: currentStart,
      end: currentEnd,
      text: currentTextLines.join(" ").replace(/<[^>]+>/g, ""),
    });
  }

  return cues;
}

export function useDualSub() {
  const isDualSubEnabled = ref(false);
  const primaryCues = ref<SubtitleCue[]>([]);
  const secondaryCues = ref<SubtitleCue[]>([]);

  const selectedWord = ref("");
  const activeDefinition = ref<VocabularyDefinition | null>(null);
  const isDictionaryOpen = ref(false);

  function getActiveCue(
    cues: SubtitleCue[],
    currentTime: number,
  ): SubtitleCue | null {
    if (cues.length === 0) return null;
    return (
      cues.find((c) => currentTime >= c.start && currentTime <= c.end) ?? null
    );
  }

  function lookupWord(rawWord: string) {
    const cleanWord = rawWord.toLowerCase().replace(/[^a-z0-9]/g, "");
    if (!cleanWord) return;

    selectedWord.value = cleanWord;
    const def = LOCAL_DICTIONARY[cleanWord];
    if (def) {
      activeDefinition.value = def;
    } else {
      activeDefinition.value = {
        word: cleanWord,
        translation: `Từ vựng "${cleanWord}"`,
      };
    }
    isDictionaryOpen.value = true;
  }

  function repeatCurrentCue(
    currentTime: number,
    seekFn: (sec: number) => void,
  ) {
    const cue =
      getActiveCue(primaryCues.value, currentTime) ||
      getActiveCue(secondaryCues.value, currentTime);
    if (cue) {
      seekFn(cue.start);
    }
  }

  function loadSampleDemoTracks() {
    primaryCues.value = [
      {
        start: 0,
        end: 15,
        text: "In a world of chaos, heroes must rise to find their destiny.",
      },
      {
        start: 16,
        end: 35,
        text: "The secret power cannot fall into the hands of our enemy.",
      },
      {
        start: 36,
        end: 60,
        text: "Fight for the truth, for victory and the ones you love!",
      },
    ];
    secondaryCues.value = [
      {
        start: 0,
        end: 15,
        text: "Trong một thế giới hỗn loạn, những anh hùng phải đứng dậy tìm định mệnh của mình.",
      },
      {
        start: 16,
        end: 35,
        text: "Nguồn sức mạnh bí mật không thể rơi vào tay kẻ thù.",
      },
      {
        start: 36,
        end: 60,
        text: "Hãy chiến đấu vì sự thật, vì chiến thắng và những người bạn yêu quý!",
      },
    ];
  }

  return {
    isDualSubEnabled,
    primaryCues,
    secondaryCues,
    selectedWord,
    activeDefinition,
    isDictionaryOpen,
    getActiveCue,
    lookupWord,
    repeatCurrentCue,
    loadSampleDemoTracks,
  };
}
