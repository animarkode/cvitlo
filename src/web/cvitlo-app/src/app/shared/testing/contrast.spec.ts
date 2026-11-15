import { readFileSync } from 'node:fs';
import {
  composite,
  contrastRatio,
  cssColorToSrgb,
  parseColorTokens,
  type TokenMap,
} from './contrast';

// The status fill tokens stay tuned for white-on-fill; the `-text` tokens for AA as text on neutral
// surfaces. Both must hold in light and dark mode.

const stylesCss = readFileSync(`${process.cwd()}/src/styles.css`, 'utf8');
const { light, dark } = parseColorTokens(stylesCss);

const AA_NORMAL = 4.5;
const STATUSES = ['destructive', 'success', 'warning', 'info'] as const;
const MODES = [
  { name: 'light', tokens: light },
  { name: 'dark', tokens: dark },
] as const;

function color(tokens: TokenMap, name: string): ReturnType<typeof cssColorToSrgb> {
  const value = tokens[name];
  expect(value, `${name} should be defined in styles.css`).toBeDefined();
  return cssColorToSrgb(value);
}

describe('parseColorTokens resolves the real .dark block', () => {
  // Guards against the `.dark` lookup matching the `.dark` substring inside
  // `@custom-variant dark (...)` and silently returning the light palette.
  it('reads the dark overrides, not the light palette', () => {
    expect(dark['--color-background']).toBe('oklch(22% 0 0)');
    expect(dark['--color-card']).not.toBe(light['--color-card']);
  });
});

describe('status colour contrast (WCAG AA)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    describe.each(STATUSES)('%s-text on its real surfaces', status => {
      const text = () => color(tokens, `--color-${status}-text`);
      const fill = () => color(tokens, `--color-${status}`);
      const page = () => color(tokens, '--color-bg');

      // Every neutral surface a status text token can land on. Asserting the
      // full set (a superset of real usage) keeps future placements safe.
      const surfaces = () => [
        { label: 'page', bg: page() },
        { label: 'card', bg: color(tokens, '--color-card') },
        { label: 'input', bg: color(tokens, '--color-input') },
        { label: 'tint /10 (toast)', bg: composite(fill(), 0.1, page()) },
        { label: 'tint /15 (verify-email, toolbar)', bg: composite(fill(), 0.15, page()) },
      ];

      it.each(surfaces())('meets AA on $label', ({ bg }) => {
        expect(contrastRatio(text(), bg)).toBeGreaterThanOrEqual(AA_NORMAL);
      });
    });

    it('keeps the solid destructive button (white-on-fill) at AA', () => {
      // Guards against "fixing" text contrast by darkening the fill token.
      const ratio = contrastRatio(
        color(tokens, '--color-destructive-foreground'),
        color(tokens, '--color-destructive'),
      );
      expect(ratio).toBeGreaterThanOrEqual(AA_NORMAL);
    });
  });
});

// Adjacent surface levels must be distinct without shadows; 1.15 is the measured floor of the ramp.
const SURFACE_SEPARATION_MIN = 1.15;

describe('gray surface ramp separation (#369)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    const pairs = [
      { label: 'card vs background', a: '--color-card', b: '--color-background' },
      { label: 'input vs card', a: '--color-input', b: '--color-card' },
      { label: 'muted vs card', a: '--color-muted', b: '--color-card' },
      { label: 'surface vs card', a: '--color-surface', b: '--color-card' },
      { label: 'line vs card', a: '--color-line', b: '--color-card' },
    ];

    it.each(pairs)('$label separation is at least 1.15', ({ a, b }) => {
      expect(contrastRatio(color(tokens, a), color(tokens, b))).toBeGreaterThanOrEqual(
        SURFACE_SEPARATION_MIN,
      );
    });
  });
});

describe('foreground text on gray surfaces (WCAG AA, #369)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    const surfaces = [
      '--color-background',
      '--color-card',
      '--color-input',
      '--color-muted',
      '--color-surface',
    ];

    it.each(surfaces)('foreground meets AA on %s', surface => {
      expect(
        contrastRatio(color(tokens, '--color-foreground'), color(tokens, surface)),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    // muted-foreground is only guarded here, on card and input. Any text on
    // bg-muted/bg-surface in dark mode must use foreground instead — see
    // verify-email's "Verifying…" pill, which overrides with dark:text-foreground.
    it.each(['--color-card', '--color-input'])('muted-foreground meets AA on %s', surface => {
      expect(
        contrastRatio(color(tokens, '--color-muted-foreground'), color(tokens, surface)),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });
  });
});

describe('surface icon-button contrast (#479)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    it('foreground meets AA on card', () => {
      expect(
        contrastRatio(color(tokens, '--color-foreground'), color(tokens, '--color-card')),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });
  });
});

// Alpha fills are banned in cv-button, so the opaque per-theme tokens must not collapse into one gray.
// The chroma floor keeps Delete red next to Edit's gray, which a luminance ratio cannot capture.
function oklchChroma(value: string): number {
  const match = /oklch\(\s*[\d.]+%\s+([\d.]+)\s+[\d.]+\s*\)/i.exec(value);
  expect(match, `"${value}" should be an opaque oklch() value`).not.toBeNull();
  return Number(match![1]);
}

describe('destructive button fill tokens (#370)', () => {
  describe.each(MODES)('$name mode', ({ name, tokens }) => {
    const softFills = ['--color-destructive-soft', '--color-destructive-soft-hover'];

    it.each(softFills)('destructive-text meets AA on %s', fill => {
      expect(
        contrastRatio(color(tokens, '--color-destructive-text'), color(tokens, fill)),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    it('destructive-foreground meets AA on the solid hover fill', () => {
      expect(
        contrastRatio(
          color(tokens, '--color-destructive-foreground'),
          color(tokens, '--color-destructive-hover'),
        ),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    // bg-muted (Edit) is achromatic in both themes; the soft fill (Delete)
    // must carry enough chroma to read as red at a glance.
    const CHROMA_FLOOR = name === 'dark' ? 0.08 : 0.02;

    it.each(softFills)(`%s chroma is at least ${name === 'dark' ? 0.08 : 0.02}`, fill => {
      const value = tokens[fill];
      expect(value, `${fill} should be defined in styles.css`).toBeDefined();
      expect(oklchChroma(value)).toBeGreaterThanOrEqual(CHROMA_FLOOR);
    });
  });
});

describe('status chip fills (account page Verified / This device)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    it.each([
      ['--color-success-text', '--color-success-soft'],
      ['--color-info-text', '--color-info-soft'],
    ])('%s meets AA on %s', (text, fill) => {
      expect(contrastRatio(color(tokens, text), color(tokens, fill))).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    it.each(['--color-success-soft', '--color-info-soft'])('%s is opaque', fill => {
      expect(tokens[fill], `${fill} should be defined in styles.css`).toBeDefined();
      expect(tokens[fill]).not.toMatch(/\//);
    });
  });
});

describe('solid success fill (verify-email pill)', () => {
  describe.each(MODES)('$name mode', ({ tokens }) => {
    it('success-foreground meets AA on success-solid', () => {
      expect(
        contrastRatio(color(tokens, '--color-success-foreground'), color(tokens, '--color-success-solid')),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    it('success-solid is opaque, so the page background cannot show through', () => {
      expect(tokens['--color-success-solid']).not.toMatch(/\//);
    });
  });
});

describe('about-page GitHub button (#370)', () => {
  // bg-foreground / text-background at rest; hover is foreground at 90% over
  // the page background — both must stay AA in both themes.
  describe.each(MODES)('$name mode', ({ tokens }) => {
    it('text-background meets AA on bg-foreground', () => {
      expect(
        contrastRatio(color(tokens, '--color-background'), color(tokens, '--color-foreground')),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    it('text-background meets AA on the hover fill (foreground/90 over page)', () => {
      const hoverFill = composite(
        color(tokens, '--color-foreground'),
        0.9,
        color(tokens, '--color-background'),
      );
      expect(contrastRatio(color(tokens, '--color-background'), hoverFill)).toBeGreaterThanOrEqual(
        AA_NORMAL,
      );
    });
  });
});

// The hover fill is theme-split: light stays translucent (/38), dark goes solid, because
// `--color-primary` (oklch 78.4%) sits between the light card (98.5%) and the dark card (31%).
describe('Add button hover fill (#407)', () => {
  // Keep in step with the class list in `orderable-list.html`.
  const LIGHT_WASH_ALPHA = 0.38;
  // The --color-surface benchmark (1.29 light / 1.31 dark) for "clearly
  // perceptible" against --color-card. The light wash is tuned to sit on it;
  // the solid dark fill clears it many times over at 6.55.
  const SEPARATION_FLOOR = 1.2;

  const lightWash = () =>
    composite(color(light, '--color-primary'), LIGHT_WASH_ALPHA, color(light, '--color-card'));

  it('light: the wash separates from the card', () => {
    expect(contrastRatio(lightWash(), color(light, '--color-card'))).toBeGreaterThanOrEqual(
      SEPARATION_FLOOR,
    );
  });

  it('light: muted-foreground stays AA on the wash, so no label override', () => {
    // This is what lets the light theme keep its resting label class on hover.
    expect(
      contrastRatio(color(light, '--color-muted-foreground'), lightWash()),
    ).toBeGreaterThanOrEqual(AA_NORMAL);
  });

  it('dark: the solid accent separates from the card', () => {
    expect(
      contrastRatio(color(dark, '--color-primary'), color(dark, '--color-card')),
    ).toBeGreaterThanOrEqual(SEPARATION_FLOOR);
  });

  it('dark: primary-foreground meets AA on the solid accent', () => {
    expect(
      contrastRatio(color(dark, '--color-primary-foreground'), color(dark, '--color-primary')),
    ).toBeGreaterThanOrEqual(AA_NORMAL);
  });

  it('dark: muted-foreground would fail, which is why the label is overridden', () => {
    // 1.33 on the solid yellow. Pairs with the light assertion above: together
    // they pin *why* the override is dark-only.
    expect(
      contrastRatio(color(dark, '--color-muted-foreground'), color(dark, '--color-primary')),
    ).toBeLessThan(AA_NORMAL);
  });

  it('dark: foreground would fail too, which is why it is primary-foreground', () => {
    // --color-foreground flips to near-white in dark and measures 1.90 here —
    // a failure light mode hides, since there it would pass.
    expect(
      contrastRatio(color(dark, '--color-foreground'), color(dark, '--color-primary')),
    ).toBeLessThan(AA_NORMAL);
  });

  it('the accent keeps enough chroma to read as yellow, not grey', () => {
    // Same argument as the #370 chroma floor — a luminance ratio cannot express
    // "yellow" next to the chevron's achromatic bg-muted.
    expect(oklchChroma(light['--color-primary'])).toBeGreaterThanOrEqual(0.1);
  });
});

// The chip is a neutral surface, not an accent: it shares the fill of the chevron hover beside it.
// The dark ramp is strictly achromatic, so a high-chroma value reads as a foreign blue tile.
describe('section-rail icon chip (#405)', () => {
  describe.each(MODES)('$name mode', ({ name, tokens }) => {
    it('foreground (the lucide icon) meets AA on lavender', () => {
      expect(
        contrastRatio(color(tokens, '--color-foreground'), color(tokens, '--color-lavender')),
      ).toBeGreaterThanOrEqual(AA_NORMAL);
    });

    // The chip carries the same fill as the chevron hover state beside it —
    // dark matches `--color-muted` exactly (1.00), light measures 1.03.
    it('lavender is the same fill as the muted hover state', () => {
      expect(
        contrastRatio(color(tokens, '--color-lavender'), color(tokens, '--color-muted')),
      ).toBeLessThanOrEqual(1.05);
    });

    // Chroma is asserted dark-mode only: light mode's lavender is a hex tint
    // that `oklchChroma` cannot parse.
    if (name !== 'dark') {
      return;
    }

    // A ceiling, not a floor. The dark neutrals are chroma 0; the chip may carry
    // the same whisper of hue the light neutrals do (0.01), but anything at
    // accent level (the #370 fills sit at 0.08+) breaks the gray ramp.
    const NEUTRAL_CHROMA_MAX = 0.02;

    it(`lavender chroma stays at or below ${NEUTRAL_CHROMA_MAX}`, () => {
      const value = tokens['--color-lavender'];
      expect(value, '--color-lavender should be defined in the .dark block').toBeDefined();
      expect(oklchChroma(value)).toBeLessThanOrEqual(NEUTRAL_CHROMA_MAX);
    });

    // The chip must stay visibly raised off the editor pane it sits on.
    it(`lavender separates from card by at least ${SURFACE_SEPARATION_MIN}`, () => {
      expect(
        contrastRatio(color(tokens, '--color-lavender'), color(tokens, '--color-card')),
      ).toBeGreaterThanOrEqual(SURFACE_SEPARATION_MIN);
    });
  });
});
