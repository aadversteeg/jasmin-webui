window.slidePanelResize = {
    dotNetRef: null,
    startX: 0,
    startWidth: 0,
    position: 'right',

    start: function(dotNetRef, startX, startWidth, position) {
        this.dotNetRef = dotNetRef;
        this.startX = startX;
        this.startWidth = startWidth;
        this.position = position;

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
    },

    onMouseMove: function(e) {
        const deltaX = e.clientX - window.slidePanelResize.startX;
        window.slidePanelResize.dotNetRef.invokeMethodAsync('UpdateWidth', deltaX);
    },

    onMouseUp: function() {
        document.removeEventListener('mousemove', window.slidePanelResize.onMouseMove);
        document.removeEventListener('mouseup', window.slidePanelResize.onMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        window.slidePanelResize.dotNetRef.invokeMethodAsync('EndResize');
    }
};
