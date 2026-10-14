document.querySelectorAll('.toast').forEach((toast) => setTimeout(() => toast.classList.add('toast-hide'), 3500));

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
