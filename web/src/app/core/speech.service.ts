import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { SpeechStyle, audioId } from './audio-id';

/** The device voice needs a locale; recordings only need the language. */
const DEVICE_LOCALES: Record<string, string> = { en: 'en-US' };

/**
 * How the device voice says each letter's name. Read alone, "A" comes out as the word "a", so the
 * device is given a spelling it can say. Recordings do not need this: the server spells letters out.
 */
const LETTER_NAMES_FOR_DEVICE: Record<string, string> = {
  A: 'ay', B: 'bee', C: 'see', D: 'dee', E: 'ee', F: 'eff', G: 'jee', H: 'aitch', I: 'eye', J: 'jay', K: 'kay',
  L: 'el', M: 'em', N: 'en', O: 'oh', P: 'pee', Q: 'cue', R: 'ar', S: 'ess', T: 'tee', U: 'you', V: 'vee',
  W: 'double you', X: 'ex', Y: 'why', Z: 'zee',
};

/**
 * Speaks text in the language being taught. Plays the recording the server made when the lesson was
 * published; when there is none (no speech provider yet, or one recording failed) it uses the device's
 * own voice, so a learner always hears something.
 */
@Injectable({ providedIn: 'root' })
export class SpeechService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /** The language of the lesson being studied, as an ISO code; the lesson screen sets it. */
  language = 'en';

  private player: HTMLAudioElement | null = null;

  /** Recordings the server does not have: not asked for again until the page reloads. */
  private readonly missing = new Set<string>();

  /**
   * @param style `letter` speaks a letter's name ("A" is "ay"), `sound` the sound it makes ("a" as in apple).
   */
  speak(text: string, slow = false, style: SpeechStyle = ''): void {
    if (!this.isBrowser) {
      return;
    }

    this.stop();

    const id = audioId(this.language, text, slow, style);
    if (this.missing.has(id)) {
      this.speakWithDevice(text, slow, style);
      return;
    }

    const player = new Audio(`/api/v1/media/audio/${id}`);
    this.player = player;
    player.addEventListener(
      'error',
      () => {
        this.missing.add(id);
        if (this.player === player) {
          this.speakWithDevice(text, slow, style);
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

  private speakWithDevice(text: string, slow: boolean, style: SpeechStyle): void {
    if (!('speechSynthesis' in window)) {
      return;
    }
    const utterance = new SpeechSynthesisUtterance(this.deviceText(text, style));
    utterance.lang = DEVICE_LOCALES[this.language] ?? this.language;
    utterance.rate = slow ? 0.55 : 0.85;
    window.speechSynthesis.speak(utterance);
  }

  /** A letter's name is spelled for the device; a spelled-out word (a name) is said letter by letter. */
  private deviceText(text: string, style: SpeechStyle): string {
    if (style !== 'letter') {
      return text;
    }
    return [...text.toUpperCase()].map((letter) => LETTER_NAMES_FOR_DEVICE[letter] ?? letter).join(' ');
  }
}
