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
    expect(audioId('apple')).toBe('7441e112be2685a40ab86b37fe161ea1');
    expect(audioId('I can buy bread.', true)).toBe('1f707f8299a056b9d1585a536cb45669');
  });

  it('ignores extra spaces, as the API does', () => {
    expect(audioId('  I want   an apple. ')).toBe(audioId('I want an apple.'));
  });

  it('tells the slow recording from the normal one', () => {
    expect(audioId('apple', true)).not.toBe(audioId('apple'));
  });
});
