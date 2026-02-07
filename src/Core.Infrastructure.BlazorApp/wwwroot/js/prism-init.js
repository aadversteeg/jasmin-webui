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
