window.dialogHelper = {
    init: (dialogEl, dotNetRef) => {
        dialogEl.addEventListener('cancel', (e) => {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnDialogCancel');
        });
        dialogEl.addEventListener('click', (e) => {
            if (e.target === dialogEl) {
                dotNetRef.invokeMethodAsync('OnDialogBackdropClick');
            }
        });
    },
    showModal: (dialogEl) => {
        if (!dialogEl.open) dialogEl.showModal();
    },
    close: (dialogEl) => {
        if (dialogEl.open) dialogEl.close();
    },
    getElementWidth: (elementId) => {
        const el = document.getElementById(elementId);
        return el ? el.offsetWidth : 0;
    }
};
