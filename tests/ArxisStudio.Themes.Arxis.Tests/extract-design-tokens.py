# -*- coding: utf-8 -*-
"""Извлекает из дизайн-проекта все объявленные значения в JSON.

Источники:
  design/10 Foundations.dc.html — шкалы (раздел 2) и семантические токены (раздел 3);
  spec/foundations.md            — метрики и типографика (раздел 5);
  design/ArxisStudio.dc.html     — значения CSS-переменных обоих вариантов;
  design/Ax*.dc.html             — какой переменной красится каждое состояние;
  spec/controls.md               — состав набора: разделы 8 и 9.
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

    # Кольцо фокуса компонент рисует тенью, тема — отдельным слоем: на экране
    # это один и тот же край, поэтому и сверяется как край.
    shadow = re.search(r'box-shadow:[^;]*?(var\(--[A-Za-z0-9]+[^)]*\))', style)
    if shadow and 'border-color' not in out:
        out['border-color'] = shadow.group(1)
    return {prop: (re.match(r'var\(--([A-Za-z0-9]+)', value).group(1) if value.startswith('var(') else value)
            for prop, value in out.items()}

def resolve(layer, choices):
    """Разворачивает {{ имя }} в обычный и выключенный вид."""
    if not any(value.startswith('{{') for value in layer.values()):
        return {'': layer}

    out = {'': {}, 'Disabled': {}}
    for prop, value in layer.items():
        name = value[2:-2].strip() if value.startswith('{{') else None
        if name is None:
            out[''][prop] = value
            out['Disabled'][prop] = value
        elif name in choices:
            out[''][prop] = strip(choices[name]['enabled'])
            out['Disabled'][prop] = strip(choices[name]['disabled'])

    # Ветки совпали — значит выключенный вид ничем не объявлен, и лишнего
    # состояния заводить не за что.
    return out if out[''] != out['Disabled'] else {'': out['']}


def strip(value):
    """Оставляет от var(--bg4, #43454A) имя переменной."""
    m = re.match(r'var\(--([A-Za-z0-9]+)', value)
    return m.group(1) if m else value.upper()


states = {}
for component in ('AxButton', 'AxTextBox', 'AxComboBox', 'AxCheckBox', 'AxToggleSwitch'):
    markup = io.open(os.path.join(HANDOFF, 'design', component + '.dc.html'), encoding='utf-8').read()

    # Часть значений компонент считает в скрипте: offBg: disabled ? 'A' : 'B'.
    # Обе ветки — объявленные значения проекта, и обе нужны: вторая описывает
    # обычный вид, первая — выключенный.
    choices = {name: {'disabled': off, 'enabled': on}
               for name, off, on in re.findall(r"(\w+):\s*disabled \? '([^']*)' : '([^']*)'", markup)}

    # Контур фокуса у флажка и тумблера объявлен только тенью на общей обёртке.
    root = re.search(r'<x-dc>\s*<div([^>]*)>', markup)
    if root:
        focus = re.search(r'style-focus="([^"]*)"', root.group(1))
        if focus and 'style="' not in root.group(1) or (focus and 'background' not in root.group(1)):
            declared_focus = declared(focus.group(1))
            if declared_focus:
                states[f'{component}/root/focus'] = declared_focus

    for m in re.finditer(r'<sc-if value="\{\{ (\w+) \}\}"[^>]*>(.*?)</sc-if>', markup, re.S):
        variant, segment = m.group(1), m.group(2)
        opening = re.match(r'\s*<\w+([^>]*)>', segment)
        if not opening:
            continue

        # Внутри вида бывает второй окрашенный слой: бегунок тумблера,
        # подсказка поля и выпадающего списка.
        layers = [declared(style) for style in re.findall(r'\sstyle="([^"]*)"', segment)]
        layers = [layer for layer in layers if layer]

        for slot, layer in zip(('base', 'inner'), layers):
            for target, resolved in resolve(layer, choices).items():
                if resolved:
                    states[f'{component}/{variant}{target}/{slot}'] = resolved

        for state in ('hover', 'active', 'focus'):
            extra_style = re.search(r'style-' + state + r'="([^"]*)"', opening.group(1))
            if extra_style:
                states[f'{component}/{variant}/{state}'] = declared(extra_style.group(1))

# ---- состав набора: разделы 8 и 9 спецификации контролов ----
#
# Раздел 8 называет контрол под каждую карточку макетов, раздел 9 — то, что
# предлагалось дописать. Имена стоят во второй колонке, иногда парой через
# косую черту: «AxComboBox / AxComboBoxItem».
spec_controls = io.open(os.path.join(HANDOFF, 'spec', 'controls.md'), encoding='utf-8').read()

controls = []
section = spec_controls[spec_controls.find('## 8.'):spec_controls.find('## 10.')]
for line in section.splitlines():
    cells = [cell.strip() for cell in line.split('|')]
    if len(cells) < 4:
        continue
    for name in re.split(r'\s*/\s*', cells[2]):
        if re.fullmatch(r'Ax[A-Za-z]+', name) and name not in controls:
            controls.append(name)

controls.sort()


out = {'scales': scales, 'semantic': semantic, 'metrics': metrics,
       'variables': variables, 'states': states, 'controls': controls}
print('шкал:', len(scales), '· семантических:', len(semantic), '· метрик:', len(metrics),
      '· переменных:', len(variables), '· состояний:', len(states),
      '· контролов:', len(controls))

dest = sys.argv[1] if len(sys.argv) > 1 else 'design-tokens.json'
io.open(dest, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False, indent=2, sort_keys=True))
print('записано в', dest)
