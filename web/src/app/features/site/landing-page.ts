import { Component, afterNextRender, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Learner } from '../learner/learner';

interface FaqItem {
  q: string;
  a: string;
}

@Component({
  selector: 'app-landing-page',
  imports: [Learner, RouterLink],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage {
  protected readonly auth = inject(AuthService);

  protected readonly initial = computed(() => this.auth.displayName().charAt(0));

  constructor() {
    // The page is prerendered for everyone; who is signed in is only known in the browser.
    afterNextRender(() => void this.auth.ensureUser());
  }

  protected readonly features: readonly string[] = [
    'نطق صوتي لكل كلمة وجملة',
    'شرح بالعربية، بلا مصطلحات صعبة',
    'تذكير بالكلمات في الوقت المناسب قبل أن تنساها',
  ];

  protected readonly steps: readonly { n: string; title: string; body: string }[] = [
    { n: '١', title: 'اختر هدفك', body: 'حدّد عدد الكلمات التي تريدها كل يوم: ٥ أو ١٠ أو ١٥. يمكنك تغييره متى شئت.' },
    { n: '٢', title: 'ادرس ١٠ دقائق', body: 'كل يوم درس قصير فيه مراجعة وكلمات جديدة ومحادثة من الحياة اليومية.' },
    { n: '٣', title: 'راجع في الوقت المناسب', body: 'نعيد لك الكلمة قبل أن تنساها: الصعبة غداً، والسهلة بعد أسبوع.' },
  ];

  private readonly faqItems: readonly FaqItem[] = [
    { q: 'هل المنصة مجانية فعلاً؟', a: 'نعم، مجانية بالكامل وبلا إعلانات. نعمل بدعم من المتبرعين والمتطوعين.' },
    {
      q: 'لا أعرف أي كلمة إنجليزية. هل أستطيع البدء؟',
      a: 'نعم. الدروس مصمّمة لمن يبدأ من الصفر، وكل شرح مكتوب بالعربية مع نطق صوتي لكل كلمة.',
    },
    { q: 'كم من الوقت أحتاج كل يوم؟', a: 'بين ٥ و١٥ دقيقة حسب الهدف الذي تختاره.' },
    { q: 'ماذا لو نسيت كلمة؟', a: 'لا بأس. المنصة تعيد لك الكلمات الصعبة أكثر حتى تثبت في ذاكرتك.' },
    { q: 'هل أستطيع الدراسة من الجوال والكمبيوتر؟', a: 'نعم، تقدّمك محفوظ في حسابك على كل الأجهزة.' },
  ];

  /** Index of the open FAQ answer, or -1 when all are closed. */
  private readonly openFaq = signal(0);

  protected readonly faq = computed(() =>
    this.faqItems.map((item, index) => ({ ...item, index, open: this.openFaq() === index })),
  );

  protected toggleFaq(index: number): void {
    this.openFaq.update((open) => (open === index ? -1 : index));
  }
}
