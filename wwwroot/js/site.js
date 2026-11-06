// Carrito de prácticas para la calculadora (se guarda en el navegador, por usuario).
const Carrito = (() => {
    const clave = () => 'carrito:' + (document.body.dataset.usuario || 'anon');
    const leer = () => { try { return JSON.parse(localStorage.getItem(clave())) || []; } catch { return []; } };
    const guardar = items => { localStorage.setItem(clave(), JSON.stringify(items)); actualizarContador(); };

    function agregar(codigo) {
        const items = leer();
        items.push({ codigo, porcentaje: 100, cantidad: 1, ayudantes: true, anestesia: true, gastos: true });
        guardar(items);
    }

    function actualizarContador() {
        const el = document.getElementById('contador-carrito');
        if (!el) return;
        const n = leer().length;
        el.textContent = n > 0 ? n : '';
    }

    document.addEventListener('DOMContentLoaded', actualizarContador);
    return { leer, guardar, agregar, actualizarContador };
})();

// Botones "Agregar a la calculadora" (data-agregar="codigo").
document.addEventListener('click', e => {
    const btn = e.target.closest('[data-agregar]');
    if (!btn) return;
    e.preventDefault();
    Carrito.agregar(btn.dataset.agregar);
    const original = btn.textContent;
    btn.textContent = 'Agregada ✓';
    btn.disabled = true;
    setTimeout(() => { btn.textContent = original; btn.disabled = false; }, 1200);
});

const formatoPesos = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', minimumFractionDigits: 2 });
const formatoNumero = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 4 });
