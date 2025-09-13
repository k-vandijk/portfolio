// Replaces a skeleton section with content fetched from a given endpoint.

async function loadSkeletonSection(sectionEndpoint) {
    const skeleton = document.getElementById('skeleton-section');

    try {
        const response = await fetch(sectionEndpoint);

        const html = await response.text();

        // Create a wrapper div to parse the HTML
        const wrapper = document.createElement('div');
        wrapper.innerHTML = html;

        // Extract <script> tags BEFORE replacing the DOM
        const scripts = wrapper.querySelectorAll('script');

        // Replace skeleton with the fetched HTML
        skeleton.replaceWith(...wrapper.childNodes);

        // Re-inject <script> tags to make them execute
        scripts.forEach((oldScript) => {
            const newScript = document.createElement('script');

            // Copy attributes
            for (const attr of oldScript.attributes) {
                newScript.setAttribute(attr.name, attr.value);
            }

            newScript.textContent = oldScript.textContent;
            document.body.appendChild(newScript);
        });

    } catch (err) {

    }
}