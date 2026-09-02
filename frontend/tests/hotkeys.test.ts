import { describe, expect, it } from "bun:test";
import { usePlayerHotkeys } from "../app/composables/usePlayerHotkeys";

describe("usePlayerHotkeys", () => {
  it("initializes hudNotice as null", () => {
    const hotkeys = usePlayerHotkeys({
      togglePlay: () => {},
      toggleFullscreen: () => {},
      toggleMute: () => {},
      seekDelta: () => {},
      adjustVolume: () => {},
    });

    expect(hotkeys.hudNotice.value).toBeNull();
  });

  it("sets hudNotice when showHud is invoked", () => {
    const hotkeys = usePlayerHotkeys({
      togglePlay: () => {},
      toggleFullscreen: () => {},
      toggleMute: () => {},
      seekDelta: () => {},
      adjustVolume: () => {},
    });

    hotkeys.showHud("+10s", "seek-forward");
    expect(hotkeys.hudNotice.value).toEqual({
      text: "+10s",
      type: "seek-forward",
    });
  });
});
