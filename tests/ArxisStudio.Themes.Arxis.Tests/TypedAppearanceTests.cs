using System.Text.RegularExpressions;
using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Вид контрола — свойство, и тема видит его псевдоклассом.
/// </summary>
/// <remarks>
/// Прежде вид задавался классом стиля, и опечатка в классе молча давала контрол по
/// умолчанию. Перечисление проверяет компилятор разметки, а тема видит значение
/// псевдоклассом: значение по умолчанию псевдокласса не несёт, другое ставит ровно свой,
/// и смена значения снимает прежний — иначе кнопка, побывавшая акцентной, осталась бы ею.
/// </remarks>
public class TypedAppearanceTests
{
    public static TheoryData<AxButtonAppearance, string?> Appearances => new()
    {
        { AxButtonAppearance.Default, null },
        { AxButtonAppearance.Primary, ":primary" },
        { AxButtonAppearance.Subtle, ":subtle" },
        { AxButtonAppearance.Danger, ":danger" },
        { AxButtonAppearance.Toolbar, ":toolbar" },
    };

    private static readonly string[] AppearanceClasses = [":primary", ":subtle", ":danger", ":toolbar"];

    /// <summary>Вид кнопки и переключателя ставит ровно свой псевдокласс и снимает прежний.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Appearances))]
    public void An_appearance_marks_exactly_its_own_pseudo_class(AxButtonAppearance appearance, string? pseudo)
    {
        foreach (var (control, classes) in new (Control, Classes)[]
                 {
                     Pair(new AxButton { Appearance = AxButtonAppearance.Primary }),
                     Pair(new AxToggleButton { Appearance = AxButtonAppearance.Primary }),
                 })
        {
            control.SetValue(AxButton.AppearanceProperty, appearance);

            foreach (var name in AppearanceClasses)
                Assert.True(classes.Contains(name) == (name == pseudo), $"{control.GetType().Name}, {appearance}: {name} стоит не так");
        }
    }

    /// <summary>Тесный размер — псевдокласс <c>:compact</c> у кнопки, переключателя, поля и списка.</summary>
    [AvaloniaFact]
    public void A_compact_size_is_marked_on_every_control_that_has_it()
    {
        Control[] controls = [new AxButton(), new AxToggleButton(), new AxTextBox(), new AxSearchField(), new AxComboBox()];

        foreach (var control in controls)
        {
            control.SetValue(AxButton.SizeProperty, AxControlSize.Compact);
            Assert.True(control.Classes.Contains(":compact"), $"{control.GetType().Name}: тесный размер не отмечен");

            control.SetValue(AxButton.SizeProperty, AxControlSize.Normal);
            Assert.False(control.Classes.Contains(":compact"), $"{control.GetType().Name}: обычный размер остался тесным");
        }
    }

    /// <summary>Роль и тон текста меняют псевдокласс, не оставляя прежнего.</summary>
    [AvaloniaFact]
    public void Role_and_tone_of_a_text_replace_their_pseudo_classes()
    {
        var text = new TextBlock();

        AxText.SetRole(text, AxTextRole.Small);
        AxText.SetTone(text, AxTextTone.Tertiary);

        Assert.Contains(":role-small", text.Classes);
        Assert.Contains(":tone-tertiary", text.Classes);

        AxText.SetRole(text, AxTextRole.Code);
        AxText.SetTone(text, AxTextTone.Primary);

        Assert.Contains(":role-code", text.Classes);
        Assert.DoesNotContain(":role-small", text.Classes);
        Assert.DoesNotContain(text.Classes, name => name.StartsWith(":tone-", StringComparison.Ordinal));
    }

    /// <summary>Тон текста красит его своей кистью, а роль — своим кеглем.</summary>
    [AvaloniaTheory]
    [InlineData(AxTextTone.Secondary, "AxTextSecondaryBrush")]
    [InlineData(AxTextTone.Tertiary, "AxTextTertiaryBrush")]
    [InlineData(AxTextTone.Disabled, "AxTextDisabledBrush")]
    [InlineData(AxTextTone.Error, "AxErrorTextBrush")]
    [InlineData(AxTextTone.Warning, "AxWarningTextBrush")]
    [InlineData(AxTextTone.Success, "AxSuccessTextBrush")]
    public void A_tone_paints_the_text_with_its_brush(AxTextTone tone, string brush)
    {
        var text = new TextBlock { Text = "Текст" };

        AxText.SetTone(text, tone);
        AxText.SetRole(text, AxTextRole.Small);

        var window = Shown(text);

        Assert.Same(Resource(window, brush), text.Foreground);
        Assert.Equal(Resource(window, "AxFontSizeSmall"), text.FontSize);

        window.Close();
    }

    /// <summary>
    /// Состояние проверки — псевдоклассы <c>:validation-warning</c> и <c>:validation-error</c>, и
    /// поле с ошибкой видно по контуру без фокуса.
    /// </summary>
    [AvaloniaFact]
    public void A_validation_state_marks_the_field_and_its_outline()
    {
        var field = new AxTextBox();
        var window = Shown(field);

        AxValidation.SetState(field, AxValidationState.Warning);
        Assert.Contains(":validation-warning", field.Classes);

        AxValidation.SetState(field, AxValidationState.Error);
        window.UpdateLayout();

        Assert.DoesNotContain(":validation-warning", field.Classes);
        Assert.Contains(":validation-error", field.Classes);

        var border = field.GetVisualDescendants().OfType<Avalonia.Controls.Border>().Single(part => part.Name == "PART_BorderElement");

        Assert.Same(Resource(window, "AxErrorOutlineBrush"), border.BorderBrush);

        window.Close();
    }

    /// <summary>Вид чипа, форма и цвет аватара, размер лоадера и иконки, пункт удаления — псевдоклассами.</summary>
    [AvaloniaFact]
    public void The_other_typed_looks_are_pseudo_classes_too()
    {
        var chip = new AxChip { Kind = AxChipKind.Key };
        var avatar = new AxAvatar { Shape = AxAvatarShape.Circle, Tint = AxAvatarTint.Purple };
        var spinner = new AxSpinner { Size = AxSpinnerSize.Large };
        var icon = new AxIcon { Size = AxIconSize.Small };
        var item = new AxMenuItem { IsDestructive = true };

        Assert.Contains(":key", chip.Classes);
        Assert.Contains(":circle", avatar.Classes);
        Assert.Contains(":purple", avatar.Classes);
        Assert.Contains(":large", spinner.Classes);
        Assert.Contains(":small", icon.Classes);
        Assert.Contains(":destructive", item.Classes);

        avatar.Tint = AxAvatarTint.Green;

        Assert.DoesNotContain(":purple", avatar.Classes);
        Assert.Contains(":green", avatar.Classes);
    }

    /// <summary>
    /// Вкладки полосы панели отмечены её видом — и те, что пришли готовыми, и те, что пришли потом.
    /// </summary>
    [AvaloniaFact]
    public void A_tool_window_strip_marks_its_tabs()
    {
        var first = new AxTabItem { Content = "Консоль" };
        var strip = new AxTabStrip { Kind = AxTabStripKind.ToolWindow };

        strip.Items.Add(first);

        var window = Shown(strip);
        var second = new AxTabItem { Content = "Терминал" };

        strip.Items.Add(second);
        window.UpdateLayout();

        Assert.Contains(":tool-window", first.Classes);
        Assert.Contains(":tool-window", second.Classes);

        strip.Kind = AxTabStripKind.Document;

        Assert.DoesNotContain(":tool-window", first.Classes);
        Assert.DoesNotContain(":tool-window", second.Classes);

        window.Close();
    }

    /// <summary>
    /// Шаблон, переприменённый сменой темы, не оставляет подписок на прежние части.
    /// </summary>
    /// <remarks>
    /// Баннер, поле поиска и диалог берут части шаблона по ссылке. Подписка на крестик прежнего
    /// шаблона, не снятая при новом, закрывала бы баннер дважды — и держала бы прежнее дерево в
    /// памяти вместе с контролом.
    /// </remarks>
    [AvaloniaFact]
    public void A_reapplied_template_keeps_one_subscription_per_part()
    {
        var banner = new AxBanner { Content = "Сообщение" };
        var window = Shown(banner);
        var closed = 0;

        banner.Closed += (_, _) => closed++;

        var firstClose = Close(banner);

        // Тот же шаблон, поставленный заново, разворачивается новым деревом — так его
        // переприменяет смена темы.
        var template = banner.Template;

        banner.Template = null;
        window.UpdateLayout();
        banner.Template = template;
        window.UpdateLayout();

        var secondClose = Close(banner);

        Assert.NotSame(firstClose, secondClose);

        firstClose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(0, closed);

        secondClose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, closed);

        window.Close();

        static Button Close(AxBanner banner) =>
            banner.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Close");
    }

    /// <summary>
    /// Имя кнопки для средств доступности тема берёт ресурсом, а не пишет в шаблоне.
    /// </summary>
    /// <remarks>
    /// Тема не знает языка студии, и имя, написанное в шаблоне словами, не переводится: так
    /// стояли «Очистить поиск» и «Закрыть сообщение» по-русски у человека с английской студией.
    /// </remarks>
    [Fact]
    public void Automation_names_in_the_theme_are_resources_not_words()
    {
        var literal = new Regex("""AutomationProperties\.Name="(?!\{)[^"]+(?=")""");

        var found = ThemeSources.All()
            .SelectMany(source => literal.Matches(source.Text).Select(match => $"{source.Name}: {match.Value}"))
            .ToList();

        Assert.True(found.Count == 0, "имена словами в шаблонах темы: " + string.Join("; ", found));
    }

    private static (Control, Classes) Pair(Control control) => (control, control.Classes);

    private static Window Shown(Control control)
    {
        var window = new Window { RequestedThemeVariant = ThemeVariant.Dark, Content = control };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static object? Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return value;
    }
}
