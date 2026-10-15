document.querySelectorAll('.toast').forEach((toast) => setTimeout(() => toast.classList.add('toast-hide'), 3500));

// Las validaciones se muestran dentro de Nexo, no con globos nativos del navegador.
document.querySelectorAll('form').forEach((form) => { form.noValidate = true; });

document.querySelectorAll('.password-toggle').forEach((button) => {
    button.addEventListener('click', () => {
        const input = button.closest('.password-field')?.querySelector('input');
        if (!input) return;
        const showPassword = input.type === 'password';
        input.type = showPassword ? 'text' : 'password';
        button.textContent = showPassword ? '🙈' : '👁';
        button.setAttribute('aria-label', showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña');
        button.title = showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña';
    });
});

const confirmDialog = document.querySelector('#confirm-dialog');
const confirmMessage = document.querySelector('#confirm-message');

const requestConfirmation = (message) => new Promise((resolve) => {
    if (!(confirmDialog instanceof HTMLDialogElement) || !confirmMessage) {
        resolve(false);
        return;
    }
    confirmMessage.textContent = message;
    confirmDialog.addEventListener('close', () => resolve(confirmDialog.returnValue === 'confirm'), { once: true });
    confirmDialog.showModal();
});

document.querySelectorAll('form[data-confirm-submit]').forEach((form) => {
    form.addEventListener('submit', async (event) => {
        if (form.dataset.confirmed === 'true') return;
        event.preventDefault();
        if (await requestConfirmation(form.dataset.confirmSubmit)) {
            form.dataset.confirmed = 'true';
            form.requestSubmit();
        }
    });
});

document.querySelectorAll('select[data-confirm-change]').forEach((select) => {
    select.addEventListener('change', async () => {
        const previousValue = select.dataset.currentValue;
        const selectedText = select.options[select.selectedIndex]?.text ?? select.value;
        const confirmed = await requestConfirmation(`${select.dataset.confirmChange} Nuevo estado: ${selectedText}.`);
        if (!confirmed) {
            select.value = previousValue;
            return;
        }
        select.form?.requestSubmit();
    });
});
