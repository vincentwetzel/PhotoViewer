# GUI Design Standards

This document defines the visual design standards for the PhotoViewer application. All UI elements must follow these guidelines to ensure a modern, polished, and visually appealing user experience.

## Core Principle: No Plain Boring Text

**All user-facing GUI elements MUST incorporate visual enhancements** such as icons, images, colors, or other graphical elements. Plain text-only UI is not acceptable.

---

## 0. Theme Compliance (MANDATORY - READ FIRST)

### Rule: Zero Theme Overrides Allowed

**All colors MUST be defined through the theme system.** No hardcoded colors, no inline brushes, no DWM overrides.

### The Theme System Is The Final Say

1. **ThemeManager.ApplyTheme()** is the **only** place where theme colors are set.
2. **ThemeManager.IsSystemDarkMode()** is the **only** method that detects system theme (reads DWM registry).
3. **App.xaml** defines the default light-mode resource brushes.
4. **All XAML elements** MUST use `{DynamicResource ResourceKey}` to reference theme colors.

### Valid Theme Resource Keys

| Resource Key | Purpose |
|---|---|
| `WindowBackground` | Main window/page backgrounds |
| `ControlBackground` | Button, panel, and control backgrounds |
| `TextForeground` | Text, icons, and foreground elements |
| `BorderBrush` | Borders and dividers |
| `MenuBackground` | Menu bar backgrounds |
| `ListItemHover` | List item hover/selection highlight |

### Strictly Forbidden

```xaml
<!-- ❌ BANNED: Hardcoded hex color -->
<Border Background="#1a1a1a" />

<!-- ❌ BANNED: Named color override -->
<TextBlock Foreground="White" />

<!-- ❌ BANNED: Inline SolidColorBrush -->
<Button Background="{Binding MyCustomBrush}" />

<!-- ❌ BANNED: DWM force in code-behind -->
<int useDark = 1; DwmSetWindowAttribute(...); />
```

### Required Pattern

```xaml
<!-- ✅ CORRECT: DynamicResource for all colors -->
<Border Background="{DynamicResource WindowBackground}" />
<TextBlock Foreground="{DynamicResource TextForeground}" />
<Button Background="{DynamicResource ControlBackground}"
        Foreground="{DynamicResource TextForeground}">
    <Button.Style>
        <Style TargetType="Button">
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Background" Value="{DynamicResource ListItemHover}" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

### Checklist for Theme Compliance

- [ ] Does every `Background`, `Foreground`, `Fill`, `Stroke`, `BorderBrush` use `{DynamicResource}`?
- [ ] Are there zero hardcoded color values (`#...`, `White`, `Black`, `Gray`, etc.) in XAML?
- [ ] Does code-behind contain zero `SolidColorBrush` color assignments that bypass the theme system?
- [ ] Does DWM title bar code respect the app theme (not force dark/light)?
- [ ] Do all Style.Triggers use `{DynamicResource}` for color changes?

---

## 1. Icons and Visual Symbols

### Requirement
All buttons, menu items, navigation items, and actions MUST be accompanied by appropriate icons.

### Examples
- **File operations**: Use folder, document, or save icons
- **Navigation**: Use arrows, home, or location icons
- **Actions**: Use action-specific symbols (e.g., star for favorites, trash for delete)
- **Sources**: Use distinct icons for different source types (Local Folder, OneDrive, Google Drive, etc.)

### Implementation
```xaml
<!-- ✅ Good: Button with icon -->
<Button>
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource IconFolder}" Width="16" Height="16" Margin="0,0,8,0" />
        <TextBlock Text="Add Folder" />
    </StackPanel>
</Button>

<!-- ❌ Bad: Plain text button -->
<Button Content="Add Folder" />
```

---

## 2. Color and Theming

### Requirement
Use meaningful colors to enhance visual hierarchy and convey state, not just for decoration.

### Guidelines
- **Primary actions**: Use accent colors (e.g., `#0078D4` for Microsoft-style apps)
- **Status indicators**: Use semantic colors (green for success, red for errors, gold for favorites)
- **Backgrounds**: Use subtle shades to create depth and separation
- **Hover/Active states**: Always provide visual feedback

### Examples
```xaml
<!-- Favorite button with state-based coloring -->
<Button Background="#80000000" Foreground="Gold">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Content" Value="&#xE106;"/> <!-- Empty Star -->
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsFavorite}" Value="True">
                    <Setter Property="Content" Value="&#xE105;"/> <!-- Filled Star -->
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

---

## 3. Images and Thumbnails

### Requirement
Wherever applicable, display image previews or thumbnails instead of text representations.

### Examples
- **Photo gallery**: Show thumbnail grids, not file paths
- **Source items**: Show preview images or representative icons
- **Recently viewed**: Show actual image thumbnails

---

## 4. Typography and Spacing

### Requirement
Use typography and spacing to create visual rhythm and readability.

### Guidelines
- **Font families**: Use modern, clean fonts (e.g., Segoe UI, Segoe UI Symbol for icons)
- **Font sizes**: Establish clear hierarchy (larger for titles, smaller for secondary info)
- **Spacing**: Use consistent margins and padding (e.g., `Margin="5,0,5,0"`)
- **Alignment**: Align related elements vertically and horizontally

---

## 5. Interactive Elements

### Requirement
All interactive elements MUST provide visual feedback.

### Guidelines
- **Hover effects**: Change color, opacity, or scale on hover
- **Pressed states**: Show depression or color change when clicked
- **Disabled states**: Use reduced opacity or grayed-out appearance
- **Focus indicators**: Show clear focus rings for keyboard navigation

---

## 6. Layout and Composition

### Requirement
Create visually balanced layouts with clear visual hierarchy.

### Guidelines
- **Grouping**: Use containers, borders, or background colors to group related controls
- **Separation**: Use splitters, separators, or spacing to divide sections
- **Alignment**: Align controls along common edges
- **Proportions**: Use golden ratio or rule of thirds for pleasing proportions

---

## 7. Tooltips and Help Text

### Requirement
Enhance tooltips with icons or additional context where helpful.

### Examples
```xaml
<!-- Tooltip with file path preview -->
<Border ToolTip="{Binding Photo.FilePath}">
    <Image Source="{Binding Photo.Thumbnail}" />
</Border>
```

---

## 8. Animations and Transitions

### Requirement
Use subtle animations to enhance user experience (when performance allows).

### Examples
- Smooth transitions when switching views
- Fade-in effects for loading content
- Scale animations for hover effects
- Slide transitions for panels

---

## 9. Navigation Controls

### Requirement
Navigation controls should be visible but not distracting. Use subtle styling that appears when needed.

### Guidelines
- **Opacity**: Use low opacity (e.g., 0.5) for idle state, increase to 1.0 on hover
- **Positioning**: Place navigation elements at natural edge positions (left/right for horizontal navigation)
- **Size**: Make navigation targets large enough to click comfortably but not overwhelming
- **Visual style**: Use semi-transparent backgrounds to blend with content

### Examples
```xaml
<!-- Navigation arrow button - faint until hovered -->
<Button Content="&#8250;"
        Opacity="0.5"
        Background="{DynamicResource ControlBackground}"
        Foreground="{DynamicResource TextForeground}"
        HorizontalAlignment="Right" VerticalAlignment="Center">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Opacity" Value="0.5" />
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Opacity" Value="1.0" />
                    <Setter Property="Background" Value="{DynamicResource ListItemHover}" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

---

## Checklist for New UI Elements

Before committing any new UI feature, verify:

- [ ] Does this element have an appropriate icon?
- [ ] Is the color scheme meaningful and consistent?
- [ ] Are there visual feedback states (hover, pressed, disabled)?
- [ ] Is the spacing and alignment consistent with the rest of the app?
- [ ] Could this benefit from an image or thumbnail?
- [ ] Is the typography clear and hierarchical?
- [ ] Does it look modern and polished?

---

## 10. Folder Tree Navigation

### Requirement
Local folder sources display as an expandable tree hierarchy (similar to Windows Explorer) with clear expand/collapse indicators.

### Guidelines
- **Chevron placement**: Right-aligned in each folder row, separate from the folder icon and name.
- **Chevron shape**: Filled `Path` using `{DynamicResource TextForeground}` — no stroked lines.
- **Chevron direction**:
  - `∨` pointing **down** when collapsed (indicates folder can be expanded).
  - `∧` pointing **up** when expanded (indicates folder can be collapsed).
- **Chevron visibility**: Hidden for leaf folders (no subfolders).
- **Interaction**: Clicking the chevron toggles expand/collapse only. Clicking the folder row selects it and loads its photos.
- **Indentation**: Nested subfolders indented by 18px per level; the chevron stays right-aligned regardless of depth.
- **Hover**: Row background highlights via `{DynamicResource ListItemHover}` on hover/selection.
- **No cursor override**: Do not change the cursor style on chevrons — use the default system cursor.

### Examples
```xaml
<!-- Folder node with right-aligned chevron -->
<DockPanel LastChildFill="True">
    <Path x:Name="Chevron" DockPanel.Dock="Right"
          Width="14" Height="10"
          Data="M 0 0 L 7 7 L 14 0 L 10 0 L 7 3 L 4 0 Z"
          Fill="{DynamicResource TextForeground}"
          VerticalAlignment="Center" />
    <StackPanel Orientation="Horizontal">
        <ContentPresenter Content="{StaticResource IconFolder}" />
        <TextBlock Text="{Binding Name}" />
        <TextBlock Text="{Binding PhotoCount}" Opacity="0.5" />
    </StackPanel>
</DockPanel>
<!-- When expanded: Data="M 0 7 L 7 0 L 14 7 L 10 7 L 7 4 L 4 7 Z" -->
```

---

## Resources

### Icon Libraries
- **Segoe UI Symbol**: Built-in Windows icon font (e.g., `&#xE105;` for filled star)
- **Material Design Icons**: https://materialdesignicons.com/
- **FontAwesome**: https://fontawesome.com/
- **Custom Path icons**: Define vector paths in XAML resources

### Color Palettes
- **Fluent Design**: Microsoft's design system colors
- **Material Design**: Google's color palette guidelines

---

## Enforcement

All code reviews and PRs MUST verify compliance with these standards. UI elements that violate these guidelines will be rejected.
