# ArxisStudio.Themes.Arxis

Тема «Arxis» — внешний вид ArxisStudio. Аналог `Avalonia.Themes.Fluent` для пары
[`ArxisStudio.Controls`](../ArxisStudio.Controls/): ControlTheme-шаблоны всех
Ax\*-контролов и палитры **Dark / Light**. Роли, правила и цели дизайн-системы
записаны в студии, в [`docs/design-system.md`](../../docs/design-system.md).

## Подключение

В M0 тема подключается поверх `FluentTheme` — базового слоя для не-Ax примитивов
(окно, попапы, тултипы); приложение регистрирует шрифт Inter:

```csharp
AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont();
```

```xml
<Application.Styles>
    <FluentTheme/>
    <themes:ArxisTheme/>
</Application.Styles>
```

Вариант темы переключается штатно: `Application.RequestedThemeVariant =
ThemeVariant.Dark / Light` — все токены объявлены в theme dictionaries и
меняются динамически.

## Откуда значения

Роли, пороги контраста, сетку и плотность задаёт дизайн-система студии, а тесты
темы проверяют правила — пороги, кратность, наличие состояний, — а не переписанные
числа. Ключевое сегодня: строка списка 24, контур фокуса 2px, скругление 4, кнопка
28 высотой с отступами 12 по горизонтали. Правило одно: значение цвета или размера объявляется
здесь и больше нигде — в шаблонах, разметке и коде панелей их быть не должно.

Высота контрола с подписью — наименьшая, а не прибитая: 28 у кнопки, 32 у
вкладки, 40 у полосы заголовка — это их высоты при кегле темы и обычной плотности. Вырастет кегль —
вырастет и контрол, а не срежет подпись. Прибитой высота остаётся только там,
где текста нет: у иконочной кнопки, флажка, тумблера, ползунка.

## Содержимое

- `Palette.axaml` — роли, а не ступени шкал, × 2 варианта: поверхности
  `AxSurfaceSunken/Base/Panel/Raised/Overlay`, взаимодействие
  `AxHover/AxPressed` — полупрозрачные, одной ступенью на любой поверхности — и
  непрозрачные `AxTrack/AxFillDisabled`, линии `AxStrokeSubtle/Control/Strong`,
  текст `AxTextPrimary/Secondary/Tertiary/Disabled` и `AxTextOnAccent`, акцент
  `AxAccent/AxAccentHover` для графики и `AxAccentFill/AxAccentFillHover/AxAccentFillPressed`
  для залитых поверхностей с текстом, `AxFocusRing`, выделение
  `AxSelectionActive/AxSelectionInactive`, ссылки `AxLink*` вместе с `AxLinkOnPlate`,
  состояния `AxError/AxWarning/AxSuccess` с текстовыми парами `*Text`, контуром поля
  `*Outline` и заливкой с рамкой сообщения `*Fill/*Stroke` (у `AxInfo` — только
  последние), оттенки `AxTint*` и `AxMonogram*`, подсказка `AxToolTipFill/Stroke`,
  ползунок прокрутки `AxScrollThumb*`, подсветка кода `AxCode*`, тени
  `AxShadowPopup/AxShadowModal`. Каждая роль — как `*Color` числом и `*Brush`;
  кисти объявлены внутри словарей вариантов, поэтому вложенный
  `ThemeVariantScope` перекрашивается вместе с ними.
- `Typography.axaml` — `AxFontFamily` (Inter), `AxFontFamilyMono` (Cascadia Code,
  едет в сборке темы; системные — запасными), целые кегли 13 / 12 / 11, заголовок 15,
  крупный 20, вордмарк заставки 26 и доля высоты строки `AxLineHeightRatio`.
- `Metrics.axaml` — размеры: `AxControlHeight` и `AxControlHeightCompact`,
  `AxRowHeight`, высоты хрома `AxTabHeight`, `AxTitleBarHeight`, `AxStatusBarHeight`,
  `AxMenuRowHeight`, `AxToolbarButtonSize`, `AxWindowButtonWidth` и шаг лестницы дерева
  `AxTreeIndent` — всё кратно четырём и идёт за плотностью, — `AxFocusOutlineWidth`,
  радиусы 3 / 4 / 6 / 8 / 10 / 12 и пилюля
  `AxCornerRadiusPill`, радиус кольца строки `AxCornerRadiusFocusRow`, отступы кнопки, поля и
  комбобокса, размер флажка и тумблера, размер и обводка иконки.
- `Spacing.axaml` — шкала расстояний: ступени `AxSpaceHair 2`, `AxSpaceTight 4`,
  `AxSpaceSnug 6`, `AxSpace 8`, `AxSpaceWide 12`, `AxSpaceLoose 16`,
  `AxSpaceSection 24`, `AxSpaceScreen 40` — каждая в двух формах (`x:Double` для
  `Spacing=` и `Thickness` с суффиксом для `Margin=`/`Padding=`), — и смысловые
  имена поверх них: `AxGapIconText`, `AxGapControls`, `AxGapFormRow`,
  `AxGapGroup`. Шкала мерит расстояние **между** вещами; отступ внутрь контрола
  — это `Ax*Padding` в `Metrics.axaml`, и на шкалу он не садится.
- `ControlThemes/` — темы всех Ax\*-контролов, меню и контекстных меню, тултипа,
  тонких скроллбаров (8px) ключом `{x:Type ScrollBar}` для всего приложения.
- `Strings.axaml` — запасные имена иконочных кнопок шаблонов для средств доступности;
  приложение кладёт перевод тем же ключом в свои ресурсы.
- Глобальные стили: база окна (фон, цвет, шрифт), задержка тултипа, роли и тоны
  текста по псевдоклассам `AxText.Role` и `AxText.Tone` (`TextBlock:role-small`,
  `TextBlock:tone-secondary`) и цвет глифа иконочной кнопки по её состоянию.

## Шрифты в поставке

Cascadia Code едет отдельной библиотекой `ArxisStudio.Fonts.Cascadia` и
включается в сборку ресурсом: моноширинный шрифт студии не зависит от того, что
установлено в системе. Лицензия SIL OFL 1.1 — рядом с файлом шрифта.

## Тесты

`tests/ArxisStudio.Themes.Arxis.Tests` — headless-контракт темы: каждый
Ax\*-контрол получает шаблон в обоих вариантах и не теряет его при переключении
`RequestedThemeVariant` у живого окна; токены ролей существуют в обеих темах, а
палитра держит пороги контраста дизайн-системы — текст на поверхностях и плашках,
граница контрола и кольцо фокуса 3:1, акцент, состояния, код; метрики тумблера и иконки
отдаёт тема, а не шаблон; моноширинный стек начинается с Cascadia Code; длины
раскладки чётные, а нечётные исключения названы поимённо; подпись каждого
контрола цела при тройной шкале кеглей, а в обычной контрол стоит на своей
высоте; отступы и скругления числом сверяются храповиками и только убывают.

```bash
dotnet test
```

Репозиторий ожидает `ArxisStudio.Controls` рядом с собой (sibling checkout);
сборка честно скажет об этом, если его нет.
