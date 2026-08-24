# ArxisStudio.Themes.Arxis

Тема «Arxis» — внешний вид ArxisStudio. Аналог `Avalonia.Themes.Fluent` для пары
[`ArxisStudio.Controls`](../ArxisStudio.Controls/): ControlTheme-шаблоны всех
Ax\*-контролов и палитры **Dark / Light** по дизайн-токенам студии
(см. `docs/design-spec.md` главного репозитория ArxisStudio).

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

## Содержимое

- `Palette.axaml` — 25 токенов × 2 варианта: `AxBg1…AxBg4`, `AxBrd/AxBrd2`,
  `AxFg/AxFg2/AxFg3`, `AxAcc/AxAccHover/AxOnAcc`, `AxSel`, `AxCanvas/AxDot`, `AxInp`,
  семантические `AxGrn/AxRed/AxYel/AxOrg/AxPur`, ссылки и тени; каждый — как
  `*Color` и `*Brush`.
- `Typography.axaml` — `AxFontFamily` (Inter), `AxFontFamilyMono` (JetBrains Mono с
  fallback), базовые размеры.
- `ControlThemes/` — темы всех Ax\*-контролов + тонкие скроллбары (8px) ключом
  `{x:Type ScrollBar}` для всего приложения.
- Глобальные стили: база окна (фон, цвет, шрифт) и текстовые роли
  `TextBlock.mono / .dim / .dimmer / .section`.

## Тесты

`tests/ArxisStudio.Themes.Arxis.Tests` — headless-контракт темы: каждый Ax\*-контрол
получает шаблон в обоих вариантах, палитра переключается с вариантом, контейнеры
списков создаются своих Ax-типов.

```bash
dotnet test
```

Репозиторий ожидает `ArxisStudio.Controls` рядом с собой (sibling checkout);
сборка честно скажет об этом, если его нет.
