# GUI Design Standards

This document defines the visual design standards for the PhotoViewer application. All UI elements must follow these guidelines to ensure a modern, polished, and visually appealing user experience.

## Core Principle: No Plain Boring Text

**All user-facing GUI elements MUST incorporate visual enhancements** such as icons, images, colors, or other graphical elements. Plain text-only UI is not acceptable.

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
        Background="#40000000" Foreground="White"
        HorizontalAlignment="Right" VerticalAlignment="Center">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Opacity" Value="0.5" />
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Opacity" Value="1.0" />
                    <Setter Property="Background" Value="#60000000" />
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
