import { Pipe, PipeTransform } from '@angular/core';

const ARABIC_DIGITS = '٠١٢٣٤٥٦٧٨٩';

export function toArabicDigits(value: number | string): string {
  return String(value).replace(/\d/g, (digit) => ARABIC_DIGITS[Number(digit)]);
}

@Pipe({ name: 'arabicDigits' })
export class ArabicDigitsPipe implements PipeTransform {
  transform(value: number | string): string {
    return toArabicDigits(value);
  }
}
