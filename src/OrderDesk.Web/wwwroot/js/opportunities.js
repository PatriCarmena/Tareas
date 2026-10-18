(() => {
    let dragged = null;
    document.querySelectorAll('.opportunity-card[draggable="true"]').forEach(card => {
        card.addEventListener('dragstart', event => {
            dragged = card;
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', card.dataset.opportunityId);
            card.classList.add('dragging');
        });
        card.addEventListener('dragend', () => {
            card.classList.remove('dragging');
            dragged = null;
            document.querySelectorAll('.kanban-column').forEach(column => column.classList.remove('drop-target'));
        });
    });
    document.querySelectorAll('.kanban-column').forEach(column => {
        column.addEventListener('dragover', event => {
            if (!dragged || dragged.closest('.kanban-column') === column) return;
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
            column.classList.add('drop-target');
        });
        column.addEventListener('dragleave', event => {
            if (!column.contains(event.relatedTarget)) column.classList.remove('drop-target');
        });
        column.addEventListener('drop', event => {
            event.preventDefault();
            column.classList.remove('drop-target');
            if (!dragged || dragged.closest('.kanban-column') === column) return;
            const form = dragged.querySelector('.opportunity-stage-form');
            const select = form?.querySelector('select[name="stage"]');
            if (!form || !select) return;
            select.value = column.dataset.stage;
            form.requestSubmit();
        });
    });
})();
