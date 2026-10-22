import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { audioId } from './audio-id';

/**
 * Speaks English text. Plays the recording the server made when the lesson was published; when
 * there is none (no speech provider yet, or one recording failed) it uses the device's own voice,
 * so a learner always hears something.
 */
@Injectable({ providedIn: 'root' })
export class SpeechService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private player: HTMLAudioElement | null = null;

  /** Recordings the server does not have: not asked for again until the page reloads. */
  private readonly missing = new Set<string>();

  speak(text: string, slow = false): void {
    if (!this.isBrowser) {
      return;
    }

    this.stop();

    const id = audioId(text, slow);
    if (this.missing.has(id)) {
      this.speakWithDevice(text, slow);
      return;
    }

    const player = new Audio(`/api/v1/media/audio/${id}`);
    this.player = player;
    player.addEventListener(
      'error',
      () => {
        this.missing.add(id);
        if (this.player === player) {
          this.speakWithDevice(text, slow);
        }
      },
      { once: true },
    );
    // A refused play() (for example when the tap was not recent) is not a missing recording.
    void player.play().catch(() => undefined);
  }

  private stop(): void {
    this.player?.pause();
    this.player = null;
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  }

  private speakWithDevice(text: string, slow: boolean): void {
    if (!('speechSynthesis' in window)) {
      return;
    }
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = slow ? 0.55 : 0.85;
    window.speechSynthesis.speak(utterance);
  }
}
