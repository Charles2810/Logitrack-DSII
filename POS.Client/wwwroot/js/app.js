window.logitrack = {
    descargarTexto: function (nombre, contenido, tipoMime) {
        const blob = new Blob(["\uFEFF" + contenido], { type: (tipoMime || "text/csv") + ";charset=utf-8;" });
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement("a");
        enlace.href = url;
        enlace.download = nombre;
        document.body.appendChild(enlace);
        enlace.click();
        document.body.removeChild(enlace);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1500);
    },
    imprimir: function () {
        window.print();
    }
};
