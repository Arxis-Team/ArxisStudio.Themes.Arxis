# -*- coding: utf-8 -*-
"""Извлекает из дизайн-проекта все объявленные значения в JSON.

Источники:
  design/10 Foundations.dc.html — шкалы (раздел 2) и семантические токены (раздел 3);
  spec/foundations.md            — метрики и типографика (раздел 5).
"""
import io, json, re, sys, os

HANDOFF = r'C:\Users\Maxim\Desktop\ArxisStudio\design_handoff_arxis'

html = io.open(os.path.join(HANDOFF, 'design', '10 Foundations.dc.html'), encoding='utf-8').read()

# ---- шкалы: title="AxGray1 · #000000" в блоках Light / Dark ----
scales = {}
pat = re.compile(r'>(Light|Dark)</span>|title="(Ax[A-Za-z]+\d+) · (#[0-9A-Fa-f]{6,8})"')
half = None
for m in pat.finditer(html):
    if m.group(1):
        half = m.group(1)
    else:
        scales.setdefault(m.group(2), {})[half] = m.group(3).upper()

# ---- семантические токены: раздел 3, тройками «имя, Light, Dark» ----
start = html.find('3. Семантические токены')
end = html.find('4. Значения макетов')
cells = re.findall(r"Fira Code[^>]*>([^<]+)</span>", html[start:end])

semantic = {}
i = 0
while i + 2 < len(cells):
    name, light, dark = cells[i], cells[i + 1], cells[i + 2]
    if re.fullmatch(r'Ax[A-Za-z]+', name) and light.startswith('#') and dark.startswith('#'):
        semantic[name] = {'Light': light.upper(), 'Dark': dark.upper()}
        i += 3
    else:
        i += 1

# ---- метрики и типографика: таблица раздела 5 ----
spec = io.open(os.path.join(HANDOFF, 'spec', 'foundations.md'), encoding='utf-8').read()
metrics = {}
for line in spec.splitlines():
    m = re.match(r'\|\s*(Ax[A-Za-z]+)\s*\|\s*([^|]+?)\s*\|', line)
    if m:
        metrics[m.group(1)] = m.group(2).strip()

out = {'scales': scales, 'semantic': semantic, 'metrics': metrics}
print('шкал:', len(scales), '· семантических:', len(semantic), '· метрик:', len(metrics))

dest = sys.argv[1] if len(sys.argv) > 1 else 'design-tokens.json'
io.open(dest, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False, indent=2, sort_keys=True))
print('записано в', dest)
