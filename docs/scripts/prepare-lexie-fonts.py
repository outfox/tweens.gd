"""Enable small-text smoothing in the bundled Lexie Readable webfonts.

Run from the repository root with:
    uv run --no-project --with 'fonttools[woff]' python docs/scripts/prepare-lexie-fonts.py

The supplied fonts omit the OpenType gasp table, leaving rasterization to
browser defaults. Explicit smoothing avoids rough small text on Windows Chrome.
See https://learn.microsoft.com/en-us/typography/opentype/spec/gasp.
"""

from pathlib import Path

from fontTools.ttLib import TTFont, newTable


fonts = Path(__file__).resolve().parents[1] / "fonts"
for name in ("LexieReadable-Regular.woff2", "LexieReadable-Bold.woff2"):
    path = fonts / name
    font = TTFont(path, recalcTimestamp=False)
    gasp = newTable("gasp")
    gasp.version = 1
    # Grid fitting, grayscale, and symmetric ClearType smoothing at every size.
    gasp.gaspRange = {0xFFFF: 0x000F}
    font["gasp"] = gasp
    font.save(path)
    print(f"Enabled smoothing: {name}")
