import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';

/**
 * Speaks English text with the browser's speech engine.
 * Placeholder until lessons ship with pre-generated Azure TTS audio.
 */
@Injectable({ providedIn: 'root' })
export class SpeechService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  speak(text: string, slow = false): void {
    if (!this.isBrowser || !('speechSynthesis' in window)) {
      return;
    }
    const synth = window.speechSynthesis;
    synth.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = slow ? 0.55 : 0.85;
    synth.speak(utterance);
  }
}
