(() => {
    const formatarTelefone = valor => {
        const digitos = (valor ?? '').replace(/\D/g, '').slice(0, 11);

        if (digitos.length <= 2) return digitos ? `(${digitos}` : '';
        if (digitos.length <= 6) return `(${digitos.slice(0, 2)}) ${digitos.slice(2)}`;
        if (digitos.length <= 10) return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 6)}-${digitos.slice(6)}`;
        return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 7)}-${digitos.slice(7)}`;
    };

    document.addEventListener('input', evento => {
        const campo = evento.target;
        if (!(campo instanceof HTMLInputElement)) return;

        if (campo.matches('[data-phone-mask]')) {
            campo.value = formatarTelefone(campo.value);
            return;
        }

        if (campo.matches('[data-percent-limit]') && Number(campo.value) > 100) {
            campo.value = '100';
        }
    }, true);
})();
