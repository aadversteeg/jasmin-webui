window.scrollHelper = {
    isScrolledToBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) return true;
        const threshold = 50; // pixels from bottom
        return element.scrollHeight - element.scrollTop - element.clientHeight < threshold;
    },

    scrollToBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    getScrollInfo: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) return { scrollTop: 0, scrollHeight: 0, clientHeight: 0 };
        return {
            scrollTop: element.scrollTop,
            scrollHeight: element.scrollHeight,
            clientHeight: element.clientHeight
        };
    }
};
