import { describe, expect, it } from 'vitest';
import { buildOtpAuthUri, qrCodeFor } from './two-factor-qr';

describe('two-factor QR code', () => {
  it('builds the address authenticator apps expect', () => {
    const uri = buildOtpAuthUri('me@example.com', 'ABCD EFGH'.replace(' ', ''));

    expect(uri).toBe('otpauth://totp/Wordrise%3Ame%40example.com?secret=ABCDEFGH&issuer=Wordrise&digits=6');
  });

  it('keeps unusual characters in the email from breaking the address', () => {
    const uri = buildOtpAuthUri('a b&c@example.com', 'KEY');

    expect(uri).not.toContain(' ');
    expect(uri.match(/\?/g)).toHaveLength(1);
    expect(new URL(uri).searchParams.get('secret')).toBe('KEY');
  });

  it('draws the code in the browser as a PNG image address', async () => {
    const image = await qrCodeFor(buildOtpAuthUri('me@example.com', 'ABCDEFGH'));

    expect(image).toMatch(/^data:image\/png;base64,/);
    expect(image.length).toBeGreaterThan(500);
  });
});
