# -*- coding: utf-8 -*-
"""Извлекает из дизайн-проекта все объявленные значения в JSON.

Источники:
  design/10 Foundations.dc.html — шкалы (раздел 2) и семантические токены (раздел 3);
  spec/foundations.md            — метрики и типографика (раздел 5);
  design/ArxisStudio.dc.html     — значения CSS-переменных обоих вариантов;
  design/Ax*.dc.html             — какой переменной красится каждое состояние.
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


# ---- значения CSS-переменных: блоки :root (тёмная) и .axL (светлая) ----
studio = io.open(os.path.join(HANDOFF, 'design', 'ArxisStudio.dc.html'), encoding='utf-8').read()

variables = {}
for selector, half in ((r':root\{([^}]*)\}', 'Dark'), (r'\.axL\{([^}]*)\}', 'Light')):
    block = re.search(selector, studio)
    for name, value in re.findall(r'--([A-Za-z0-9]+):\s*([^;]+)', block.group(1)):
        variables.setdefault(name, {})[half] = value.strip().upper()

# ---- состояния контролов: style / style-hover / style-active / style-focus ----
#
# Компонент пишет цвет как var(--bg3, #393B40) — берём имя переменной, а не
# запасной литерал: тогда значение придёт из блоков выше и останется одно на
# весь проект.
def declared(style):
    out = {}
    for prop in ('background', 'color', 'border-color'):
        m = re.search(r'(?:^|;)\s*' + prop + r':\s*([^;]+)', style)
        if m:
            out[prop] = m.group(1).strip()
    border = re.search(r'(?:^|;)\s*border:\s*[^;]*?(var\(--[A-Za-z0-9]+[^)]*\)|transparent)', style)
    if border and 'border-color' not in out:
        out['border-color'] = border.group(1)
    return {prop: (re.match(r'var\(--([A-Za-z0-9]+)', value).group(1) if value.startswith('var(') else value)
            for prop, value in out.items()}

states = {}
for component in ('AxButton', 'AxTextBox', 'AxComboBox'):
    markup = io.open(os.path.join(HANDOFF, 'design', component + '.dc.html'), encoding='utf-8').read()
    for m in re.finditer(r'<sc-if value="\{\{ (\w+) \}\}"[^>]*>\s*<div([^>]*)>', markup):
        variant, attrs = m.group(1), m.group(2)
        base = re.search(r'\sstyle="([^"]*)"', attrs)
        if base:
            states[f'{component}/{variant}/base'] = declared(base.group(1))
        for state in ('hover', 'active', 'focus'):
            extra_style = re.search(r'style-' + state + r'="([^"]*)"', attrs)
            if extra_style:
                states[f'{component}/{variant}/{state}'] = declared(extra_style.group(1))

out = {'scales': scales, 'semantic': semantic, 'metrics': metrics,
       'variables': variables, 'states': states}
print('шкал:', len(scales), '· семантических:', len(semantic), '· метрик:', len(metrics),
      '· переменных:', len(variables), '· состояний:', len(states))

dest = sys.argv[1] if len(sys.argv) > 1 else 'design-tokens.json'
io.open(dest, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False, indent=2, sort_keys=True))
print('записано в', dest)
