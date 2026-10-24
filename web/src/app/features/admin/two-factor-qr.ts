import QRCode from 'qrcode';

/** The address authenticator apps read from a QR code (or from a link opened on the phone itself). */
export function buildOtpAuthUri(email: string, key: string): string {
  const label = encodeURIComponent(`Wordrise:${email}`);
  return `otpauth://totp/${label}?secret=${encodeURIComponent(key)}&issuer=Wordrise&digits=6`;
}

/**
 * The QR code as an image address, drawn in the browser: the key is a secret, so it never goes to an
 * online QR service.
 */
export function qrCodeFor(text: string): Promise<string> {
  return QRCode.toDataURL(text, { width: 240, margin: 1, errorCorrectionLevel: 'M' });
}
