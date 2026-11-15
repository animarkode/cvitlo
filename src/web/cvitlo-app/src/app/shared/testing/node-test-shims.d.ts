// Ambient Node declarations for the contrast spec reading `styles.css`; the project has no `@types/node`.
declare module 'node:fs' {
  export function readFileSync(path: string, encoding: 'utf8'): string;
}

declare const process: { cwd(): string };
