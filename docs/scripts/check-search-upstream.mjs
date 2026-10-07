import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

// Search.astro from Starlight 0.42.4, reviewed when creating our language-filtering override.
// Update only after comparing the upstream component and porting any relevant changes.
const reviewedHash = '30d6549c54409a9e92ec9734901946d409a408635e8876b18940285f21c62062';

export async function checkSearchUpstream() {
  const upstream = new URL(import.meta.resolve('@astrojs/starlight/components/Search.astro'));
  const source = await readFile(upstream, 'utf8');
  // Ignore checkout/platform line endings, but detect every other upstream source change.
  const hash = createHash('sha256').update(source.replace(/\r\n/g, '\n')).digest('hex');
  if (hash !== reviewedHash) {
    throw new Error(
      "Starlight's Search.astro has changed since our search override was reviewed.\n" +
        'Compare it with src/components/overrides/Search.astro, port relevant changes, and verify both language filters.\n' +
        'Only then update reviewedHash in scripts/check-search-upstream.mjs. See README.md: Search override maintenance.\n' +
        `Upstream component: ${upstream}\nCurrent SHA-256: ${hash}`,
    );
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  await checkSearchUpstream();
  console.log('Starlight search component matches the reviewed upstream source.');
}
