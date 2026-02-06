window.splitPanelResize = {
    dotNetRef: null,
    container: null,
    startX: 0,
    startWidthPercent: 0,

    start: function(dotNetRef, containerId, startX, startWidthPercent) {
        this.dotNetRef = dotNetRef;
        this.container = document.getElementById(containerId);
        this.startX = startX;
        this.startWidthPercent = startWidthPercent;

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
    },

    onMouseMove: function(e) {
        const self = window.splitPanelResize;
        if (!self.container) return;

        const containerWidth = self.container.offsetWidth;
        const deltaX = e.clientX - self.startX;
        const deltaPercent = (deltaX / containerWidth) * 100;
        const newPercent = Math.min(80, Math.max(20, self.startWidthPercent + deltaPercent));

        self.dotNetRef.invokeMethodAsync('UpdateWidthPercent', newPercent);
    },

    onMouseUp: function() {
        document.removeEventListener('mousemove', window.splitPanelResize.onMouseMove);
        document.removeEventListener('mouseup', window.splitPanelResize.onMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        window.splitPanelResize.dotNetRef.invokeMethodAsync('EndResize');
    }
};
