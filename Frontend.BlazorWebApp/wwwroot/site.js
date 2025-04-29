console.log("asdadasdas");

window.fadeOutAndRemove = (el) => {
    if (!el) return;
    el.style.opacity = '0';
    setTimeout(() => {
        el.remove();
    }, 500);
};
