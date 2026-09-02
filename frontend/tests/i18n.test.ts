import { describe, expect, it } from "bun:test";
import { en } from "../app/i18n/en";
import type { Messages } from "../app/i18n/types";
import { vi } from "../app/i18n/vi";

describe("i18n contract integrity", () => {
  it("vi and en dictionaries satisfy Messages contract", () => {
    const viMessages: Messages = vi;
    const enMessages: Messages = en;
    expect(viMessages).toBeDefined();
    expect(enMessages).toBeDefined();
  });

  it("vi and en have identical key structures at top level", () => {
    const viKeys = Object.keys(vi).sort();
    const enKeys = Object.keys(en).sort();
    expect(viKeys).toEqual(enKeys);
  });

  it("vi and en have identical home keys", () => {
    const viHome = Object.keys(vi.home).sort();
    const enHome = Object.keys(en.home).sort();
    expect(viHome).toEqual(enHome);
  });

  it("vi and en have identical browse keys", () => {
    const viBrowse = Object.keys(vi.browse).sort();
    const enBrowse = Object.keys(en.browse).sort();
    expect(viBrowse).toEqual(enBrowse);
  });

  it("vi and en have identical myList keys", () => {
    const viMyList = Object.keys(vi.myList).sort();
    const enMyList = Object.keys(en.myList).sort();
    expect(viMyList).toEqual(enMyList);
  });

  it("vi and en have identical assistant keys", () => {
    const viAssistant = Object.keys(vi.assistant).sort();
    const enAssistant = Object.keys(en.assistant).sort();
    expect(viAssistant).toEqual(enAssistant);
  });
});
