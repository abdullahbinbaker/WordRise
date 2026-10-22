import { describe, expect, it } from 'vitest';
import { audioId, sha256 } from './audio-id';

const hex = (bytes: Uint8Array) => [...bytes].map((b) => b.toString(16).padStart(2, '0')).join('');

describe('audioId', () => {
  it('computes SHA-256 correctly, including inputs that need extra padding blocks', () => {
    const encode = (text: string) => new TextEncoder().encode(text);
    expect(hex(sha256(encode('')))).toBe('e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855');
    expect(hex(sha256(encode('abc')))).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad');
    expect(hex(sha256(encode('abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq')))).toBe(
      '248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1',
    );
  });

  // The same values come out of the API (`AudioId.For`), which is how the app finds recordings.
  it('matches the names the API gives recordings', () => {
    expect(audioId('en', 'apple')).toBe('f41e0a4134d0d7aec2945806eebb7370');
    expect(audioId('en', 'I can buy bread.', true)).toBe('cedab8a21e7469cae5d949e73a29038c');
  });

  it('ignores extra spaces, as the API does', () => {
    expect(audioId('en', '  I want   an apple. ')).toBe(audioId('en', 'I want an apple.'));
  });

  it('tells the slow recording from the normal one', () => {
    expect(audioId('en', 'apple', true)).not.toBe(audioId('en', 'apple'));
    expect(audioId('fr', 'apple')).not.toBe(audioId('en', 'apple'));
  });
});
