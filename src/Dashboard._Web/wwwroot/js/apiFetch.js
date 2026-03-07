async function apiFetch(url, options) {
    const response = await fetch(url, options);
    if (!response.ok) {
        const detail = await response.text().catch(() => '');
        console.error(`apiFetch error: ${response.status} ${response.statusText} — ${url}`, detail);
        throw new Error(`Request failed: ${response.status} ${response.statusText}`);
    }
    return response;
}
