import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { audioId } from './audio-id';
import { SpeechService } from './speech.service';

/** A stand-in for the browser's audio element that records what it was asked to do. */
class FakeAudio {
  static instances: FakeAudio[] = [];
  private readonly listeners = new Map<string, () => void>();
  paused = false;

  constructor(readonly src: string) {
    FakeAudio.instances.push(this);
  }

  addEventListener(name: string, listener: () => void): void {
    this.listeners.set(name, listener);
  }

  play(): Promise<void> {
    return Promise.resolve();
  }

  pause(): void {
    this.paused = true;
  }

  fail(): void {
    this.listeners.get('error')?.();
  }
}

describe('SpeechService', () => {
  const speak = vi.fn();
  const cancel = vi.fn();

  beforeEach(() => {
    FakeAudio.instances = [];
    speak.mockClear();
    cancel.mockClear();
    vi.stubGlobal('Audio', FakeAudio);
    vi.stubGlobal('speechSynthesis', { speak, cancel });
    vi.stubGlobal('SpeechSynthesisUtterance', class { constructor(readonly text: string) {} });
  });

  afterEach(() => vi.unstubAllGlobals());

  const service = () => TestBed.inject(SpeechService);

  it('plays the recording the server made, not the device voice', () => {
    service().speak('apple');

    expect(FakeAudio.instances.map((a) => a.src)).toEqual([`/api/v1/media/audio/${audioId('apple')}`]);
    expect(speak).not.toHaveBeenCalled();
  });

  it('asks for the slow recording when slow is requested', () => {
    service().speak('I can buy bread.', true);

    expect(FakeAudio.instances[0].src).toBe(`/api/v1/media/audio/${audioId('I can buy bread.', true)}`);
  });

  it('falls back to the device voice when there is no recording, and remembers that', () => {
    const speech = service();

    speech.speak('apple');
    FakeAudio.instances[0].fail();
    expect(speak).toHaveBeenCalledTimes(1);

    speech.speak('apple');
    expect(FakeAudio.instances).toHaveLength(1);
    expect(speak).toHaveBeenCalledTimes(2);
  });

  it('stops what is playing before the next sound starts', () => {
    const speech = service();

    speech.speak('apple');
    speech.speak('milk');

    expect(FakeAudio.instances[0].paused).toBe(true);
    expect(cancel).toHaveBeenCalled();
  });
});
