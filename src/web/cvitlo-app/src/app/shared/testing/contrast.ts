// jsdom cannot compute colour, so axe's color-contrast rule is off; these helpers let a plain
// spec check the `src/styles.css` tokens against WCAG AA instead.

export type Rgb = readonly [number, number, number];

export function oklchToSrgb(lPct: number, c: number, hDeg: number): Rgb {
  const l = lPct / 100;
  const h = (hDeg * Math.PI) / 180;
  const a = c * Math.cos(h);
  const b = c * Math.sin(h);

  const l_ = l + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = l - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = l - 0.0894841775 * a - 1.291485548 * b;

  const lCubed = l_ ** 3;
  const mCubed = m_ ** 3;
  const sCubed = s_ ** 3;

  const r = 4.0767416621 * lCubed - 3.3077115913 * mCubed + 0.2309699292 * sCubed;
  const g = -1.2684380046 * lCubed + 2.6097574011 * mCubed - 0.3413193965 * sCubed;
  const bl = -0.0041960863 * lCubed - 0.7034186147 * mCubed + 1.707614701 * sCubed;

  return [gammaEncode(r), gammaEncode(g), gammaEncode(bl)];
}

function gammaEncode(channel: number): number {
  const clamped = Math.max(0, Math.min(1, channel));
  const encoded = clamped <= 0.0031308 ? 12.92 * clamped : 1.055 * clamped ** (1 / 2.4) - 0.055;
  return encoded * 255;
}

export function hexToSrgb(hex: string): Rgb {
  const value = hex.replace('#', '');
  return [
    parseInt(value.slice(0, 2), 16),
    parseInt(value.slice(2, 4), 16),
    parseInt(value.slice(4, 6), 16),
  ];
}

// Alpha is ignored (callers composite explicitly). Throws on anything else so a renamed token fails loudly.
export function cssColorToSrgb(value: string): Rgb {
  const oklch = /oklch\(\s*([\d.]+)%\s+([\d.]+)\s+([\d.]+)\s*(?:\/\s*[\d.]+)?\s*\)/i.exec(value);
  if (oklch) {
    return oklchToSrgb(Number(oklch[1]), Number(oklch[2]), Number(oklch[3]));
  }
  const hex = /^#([0-9a-f]{6})$/i.exec(value.trim());
  if (hex) {
    return hexToSrgb(value.trim());
  }
  throw new Error(`Unsupported CSS colour value: "${value}"`);
}

// Blends in gamma space, as browsers do for a translucent fill.
export function composite(fg: Rgb, alpha: number, bg: Rgb): Rgb {
  return [
    fg[0] * alpha + bg[0] * (1 - alpha),
    fg[1] * alpha + bg[1] * (1 - alpha),
    fg[2] * alpha + bg[2] * (1 - alpha),
  ];
}

export function relativeLuminance([r, g, b]: Rgb): number {
  const linear = (channel: number) => {
    const c = channel / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

export function contrastRatio(a: Rgb, b: Rgb): number {
  const la = relativeLuminance(a);
  const lb = relativeLuminance(b);
  const lighter = Math.max(la, lb);
  const darker = Math.min(la, lb);
  return (lighter + 0.05) / (darker + 0.05);
}

export type TokenMap = Record<string, string>;

// `dark` is `@theme` overlaid with `.dark`, so each map is the complete palette as rendered.
export function parseColorTokens(css: string): { light: TokenMap; dark: TokenMap } {
  const theme = extractBlock(css, '@theme');
  const dark = extractBlock(css, '.dark');
  const light = readColorVars(theme);
  return { light, dark: { ...light, ...readColorVars(dark) } };
}

function extractBlock(css: string, selector: string): string {
  // Anchor to the rule start so `.dark` inside `@custom-variant dark (...)` cannot match and
  // return the light palette.
  const rule = new RegExp(`^${escapeRegExp(selector)}\\s*\\{`, 'm');
  const match = rule.exec(css);
  if (!match) {
    throw new Error(`Block "${selector}" not found in styles.css`);
  }
  const open = css.indexOf('{', match.index);
  let depth = 0;
  for (let i = open; i < css.length; i++) {
    if (css[i] === '{') depth++;
    if (css[i] === '}') {
      depth--;
      if (depth === 0) return css.slice(open + 1, i);
    }
  }
  throw new Error(`Unbalanced braces after "${selector}" in styles.css`);
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function readColorVars(block: string): TokenMap {
  const map: TokenMap = {};
  const re = /(--color-[\w-]+)\s*:\s*([^;]+);/g;
  let match: RegExpExecArray | null;
  while ((match = re.exec(block)) !== null) {
    map[match[1]] = match[2].trim();
  }
  return map;
}
