// Prism.js initialization and interop helpers

window.prismInterop = {
    /**
     * Highlights all code blocks within a container element.
     * @param {string} containerId - The ID of the container element
     */
    highlightAll: function(containerId) {
        const container = document.getElementById(containerId);
        if (container && typeof Prism !== 'undefined') {
            Prism.highlightAllUnder(container);
        }
    },

    /**
     * Highlights a specific code element.
     * @param {string} elementId - The ID of the code element
     */
    highlightElement: function(elementId) {
        const element = document.getElementById(elementId);
        if (element && typeof Prism !== 'undefined') {
            Prism.highlightElement(element);
        }
    },

    /**
     * Gets the Prism language class for a given MIME type.
     * @param {string} mimeType - The MIME type
     * @returns {string} The Prism language class
     */
    getLanguageClass: function(mimeType) {
        if (!mimeType) return 'language-plaintext';

        const mimeToLanguage = {
            'application/json': 'language-json',
            'text/javascript': 'language-javascript',
            'application/javascript': 'language-javascript',
            'text/css': 'language-css',
            'text/html': 'language-markup',
            'text/xml': 'language-markup',
            'application/xml': 'language-markup',
            'text/markdown': 'language-markdown',
            'text/x-csharp': 'language-csharp',
            'text/x-python': 'language-python',
            'text/plain': 'language-plaintext'
        };

        // Check for exact match
        if (mimeToLanguage[mimeType]) {
            return mimeToLanguage[mimeType];
        }

        // Check for text/* patterns
        if (mimeType.startsWith('text/x-')) {
            const lang = mimeType.substring(7); // Remove 'text/x-'
            if (Prism.languages[lang]) {
                return 'language-' + lang;
            }
        }

        // Default to plaintext
        return 'language-plaintext';
    }
};

// Resource link interception helpers
window.resourceLinkInterop = {
    /**
     * Sets up click interception for resource links within a container.
     * Links with data-resource-link="true" will trigger a callback to Blazor.
     * @param {string} containerId - The ID of the container element
     * @param {object} dotNetRef - The DotNetObjectReference for callbacks
     */
    setupLinkInterception: function(containerId, dotNetRef) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // Find all resource links
        const resourceLinks = container.querySelectorAll('a[data-resource-link="true"]');

        resourceLinks.forEach(link => {
            // Avoid adding multiple listeners
            if (link.dataset.listenerAttached) return;
            link.dataset.listenerAttached = 'true';

            link.addEventListener('click', async function(e) {
                // Allow modifier keys to work (open in new tab, etc.)
                if (e.ctrlKey || e.metaKey || e.shiftKey) {
                    return; // Let the browser handle it
                }

                e.preventDefault();

                // Extract the resource URI from the href
                const href = link.getAttribute('href');
                if (!href) return;

                try {
                    const url = new URL(href, window.location.origin);
                    const resourceParam = url.searchParams.get('resource');

                    if (resourceParam) {
                        // Decode the resource URI and call back to Blazor
                        const resourceUri = decodeURIComponent(resourceParam);
                        await dotNetRef.invokeMethodAsync('OnResourceLinkClick', resourceUri);
                    }
                } catch (error) {
                    console.error('Error handling resource link click:', error);
                }
            });
        });
    }
};
