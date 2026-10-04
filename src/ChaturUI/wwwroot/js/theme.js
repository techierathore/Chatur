// Chatur — sets the theme attributes the four files under wwwroot/themes read (ThemeInterop.cs).
// A theme is data (Settings ▸ Appearance, cluster L); this is the one DOM write that applies it.
export function setTheme(themeName, isDark) {
    document.documentElement.setAttribute('data-theme', themeName);
    document.documentElement.classList.toggle('dark', isDark);
    clearCustomTheme();
}

// A theme added from a file (REQ-UI-038) has no static wwwroot/themes/*.css of its own — "no new
// build of Chatur" rules that out. Its named tokens (the same compact shape the four built-in
// themes hold in the Theme table) are mapped onto the real TrBlazeUI variables here and written as
// one <style> element, so it repaints every open window exactly like a built-in theme would.
const CUSTOM_STYLE_ID = 'chatur-custom-theme';

const TOKEN_MAP = {
    bg: ['--background'],
    card: ['--card', '--popover'],
    fg: ['--foreground', '--card-foreground', '--popover-foreground'],
    dim: ['--muted-foreground'],
    line: ['--border', '--input'],
    line2: ['--sidebar-border'],
    soft: ['--secondary', '--muted', '--sidebar-accent'],
    hover: ['--sidebar'],
    accent: ['--primary', '--ring', '--sidebar-primary'],
    accentFg: ['--primary-foreground', '--sidebar-primary-foreground'],
    accentSoft: ['--accent'],
    faint: ['--sidebar-foreground'],
};

export function applyCustomTheme(themeName, lightValues, darkValues, isDark) {
    document.documentElement.setAttribute('data-theme', themeName);
    document.documentElement.classList.toggle('dark', isDark);

    let vStyle = document.getElementById(CUSTOM_STYLE_ID);
    if (!vStyle) {
        vStyle = document.createElement('style');
        vStyle.id = CUSTOM_STYLE_ID;
        document.head.appendChild(vStyle);
    }

    const vLightBlock = toDeclarationBlock(lightValues);
    const vDarkBlock = toDeclarationBlock(darkValues);
    vStyle.textContent =
        `html[data-theme="${themeName}"]{${vLightBlock}}\n` +
        `html.dark[data-theme="${themeName}"]{${vDarkBlock}}`;
}

export function clearCustomTheme() {
    const vStyle = document.getElementById(CUSTOM_STYLE_ID);
    if (vStyle) {
        vStyle.remove();
    }
}

function toDeclarationBlock(values) {
    let vCss = '';
    for (const vToken in values) {
        const vTargets = TOKEN_MAP[vToken];
        if (!vTargets) {
            continue;
        }
        for (const vTarget of vTargets) {
            vCss += `${vTarget}:${values[vToken]};`;
        }
    }
    // accent-foreground pairs with the accentSoft background, not with accent itself.
    if (values.accent) {
        vCss += `--accent-foreground:${values.accent};`;
    }
    return vCss;
}
