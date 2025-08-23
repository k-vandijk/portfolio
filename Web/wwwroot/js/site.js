const styles = getComputedStyle(document.documentElement);

const accentColors = [
    styles.getPropertyValue('--accent-1').trim(),
    styles.getPropertyValue('--accent-2').trim(),
    styles.getPropertyValue('--accent-3').trim(),
    styles.getPropertyValue('--accent-4').trim()
];

const overlay = document.getElementById('page-loading');
const show = () => overlay.classList.remove('d-none');
const hide = () => overlay.classList.add('d-none');

// Show when clicking same-tab links
document.addEventListener('click', function (e) {
    const a = e.target.closest('a[href]');
    if (!a) return;

    // Ignore new-tab / download / anchors / cross-origin / JS links
    if (a.target === '_blank' || a.hasAttribute('download')) return;
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    if (a.getAttribute('href').startsWith('#')) return;
    const url = new URL(a.href, window.location.href);
    if (url.origin !== window.location.origin) return;
    if (a.getAttribute('href').startsWith('javascript:')) return;

    show();
}, { capture: true });

// Show on form submit
document.addEventListener('submit', function (e) {
    const form = e.target;
    if (form.target && form.target !== '_self') return; // submitting to new tab/frame
    show();
}, { capture: true });

// Fallback: if navigation is triggered programmatically
window.addEventListener('beforeunload', show);

// Hide when page is shown from BFCache (back/forward)
window.addEventListener('pageshow', function (e) {
    if (e.persisted) hide();
});