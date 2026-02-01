window.sidePanelResize = {
    dotNetRef: null,
    startX: 0,
    startWidth: 0,

    start: function(dotNetRef, startX, startWidth) {
        this.dotNetRef = dotNetRef;
        this.startX = startX;
        this.startWidth = startWidth;

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
    },

    onMouseMove: function(e) {
        const deltaX = e.clientX - window.sidePanelResize.startX;
        window.sidePanelResize.dotNetRef.invokeMethodAsync('UpdateWidth', deltaX);
    },

    onMouseUp: function() {
        document.removeEventListener('mousemove', window.sidePanelResize.onMouseMove);
        document.removeEventListener('mouseup', window.sidePanelResize.onMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        window.sidePanelResize.dotNetRef.invokeMethodAsync('EndResize');
    }
};
