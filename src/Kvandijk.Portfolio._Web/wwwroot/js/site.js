const styles = getComputedStyle(document.documentElement);

const accentColors = [
    styles.getPropertyValue('--accent-1').trim(),
    styles.getPropertyValue('--accent-2').trim(),
    styles.getPropertyValue('--accent-3').trim(),
    styles.getPropertyValue('--accent-4').trim()
];
