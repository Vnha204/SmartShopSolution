const CAT_API = '/Admin/api/categories';
let _maxDepth = 0;

function generateSlug(name) {
    const s = name.toLowerCase()
        .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd').replace(/Đ/g, 'd')
        .replace(/[^a-z0-9\s-]/g, '').trim().replace(/\s+/g, '-');
    document.getElementById('Slug').value = s;
}

function removeDepthsFrom(fromDepth) {
    for (let d = fromDepth; d <= _maxDepth + 1; d++) {
        const el = document.getElementById('cat-level-' + d);
        if (el) el.remove();
    }
    _maxDepth = Math.max(fromDepth - 1, 0);
}

async function renderLevel(parentId, depth) {
    removeDepthsFrom(depth);
    clearLeaf();

    const url = parentId != null
        ? `${CAT_API}/children?parentId=${parentId}`
        : `${CAT_API}/children`;

    const resp = await fetch(url);
    const categories = await resp.json();
    if (!categories.length) return;

    const container = document.getElementById('categoryChainContainer');
    const wrapper = document.createElement('div');
    wrapper.id = 'cat-level-' + depth;
    wrapper.className = 'mb-2';

    const label = document.createElement('small');
    label.className = 'form-text text-muted mb-1 d-block';
    label.textContent = depth === 0 ? 'Danh mục gốc' : 'Danh mục con';
    wrapper.appendChild(label);

    const sel = document.createElement('select');
    sel.className = 'form-select form-select-sm';
    sel.dataset.depth = depth;
    sel.dataset.parentId = parentId ?? '';
    sel.addEventListener('change', function () { onLevelChange(this); });

    const placeholder = document.createElement('option');
    placeholder.value = '';
    placeholder.textContent = '-- Chọn --';
    sel.appendChild(placeholder);

    categories.forEach(c => {
        const opt = document.createElement('option');
        opt.value = c.id;
        opt.textContent = c.name;
        opt.dataset.hasChildren = c.hasChildren ? '1' : '0';
        sel.appendChild(opt);
    });

    wrapper.appendChild(sel);
    const stale = document.getElementById(wrapper.id);
    if (stale) stale.remove();
    container.appendChild(wrapper);
    _maxDepth = Math.max(_maxDepth, depth);
}

async function onLevelChange(sel) {
    const depth = parseInt(sel.dataset.depth);
    const categoryId = sel.value;

    removeDepthsFrom(depth + 1);
    clearLeaf();

    if (!categoryId) return;

    const opt = sel.querySelector(`option[value="${categoryId}"]`);
    const hasChildren = opt && opt.dataset.hasChildren === '1';

    if (hasChildren) {
        await renderLevel(parseInt(categoryId), depth + 1);
    } else {
        setLeaf(parseInt(categoryId), await fetchBreadcrumbText(categoryId));
    }
}

function setLeaf(categoryId, breadcrumbText) {
    document.getElementById('CategoryId').value = categoryId;
    document.getElementById('leafBreadcrumb').textContent = breadcrumbText;
    document.getElementById('leafDisplay').style.display = 'block';
    loadAttributes(categoryId, []);
}

function clearLeaf() {
    document.getElementById('CategoryId').value = '';
    document.getElementById('leafDisplay').style.display = 'none';
    document.getElementById('attributeCard').style.display = 'none';
    document.getElementById('attributeSection').innerHTML = '';
}

async function fetchBreadcrumbText(categoryId) {
    const resp = await fetch(`${CAT_API}/breadcrumb?categoryId=${categoryId}`);
    const bc = await resp.json();
    return bc.map(b => b.name).join(' › ');
}

async function preloadCategoryChain(leafCategoryId) {
    const resp = await fetch(`${CAT_API}/breadcrumb?categoryId=${leafCategoryId}`);
    const breadcrumb = await resp.json();
    if (!breadcrumb.length) return;

    await renderLevel(null, 0);

    for (let i = 0; i < breadcrumb.length; i++) {
        const item = breadcrumb[i];
        const sel = document.querySelector(`#cat-level-${i} select`);
        if (sel) sel.value = item.id;

        if (i < breadcrumb.length - 1) {
            await renderLevel(item.id, i + 1);
        } else {
            setLeaf(item.id, breadcrumb.map(b => b.name).join(' › '));
        }
    }
}

async function loadAttributes(categoryId, selectedIds) {
    const card = document.getElementById('attributeCard');
    const section = document.getElementById('attributeSection');
    try {
        const resp = await fetch(`${CAT_API}/${categoryId}/attributes`);
        const attrs = await resp.json();
        renderAttributeCheckboxes(section, attrs, selectedIds);
        card.style.display = attrs.length > 0 ? 'block' : 'none';
    } catch (e) {
        console.error('Failed to load attributes', e);
    }
}

function renderAttributeCheckboxes(container, attrs, selectedIds) {
    if (!attrs || !attrs.length) {
        container.innerHTML =
            '<p class="text-muted small mb-0">Danh mục này chưa có thuộc tính nào được cấu hình.</p>';
        return;
    }
    let html = '';
    attrs.forEach(attr => {
        html += `<div class="mb-3" data-attribute-id="${attr.attributeId}">
                <p class="fw-semibold mb-1">${escHtml(attr.displayName)}</p>
                <div class="d-flex flex-wrap gap-2">`;
        attr.values.forEach(v => {
            const chk = (selectedIds.includes(v.id) || v.selected) ? ' checked' : '';
            html += `<div class="form-check form-check-inline">
                    <input class="form-check-input" type="checkbox"
                           name="SelectedAttributeValueIds" value="${v.id}"
                           id="av_${v.id}" data-attribute-id="${attr.attributeId}"
                           data-value-id="${v.id}"${chk} />
                    <label class="form-check-label" for="av_${v.id}">
                        ${escHtml(v.displayName)}
                    </label>
                </div>`;
        });
        html += `</div></div>`;
    });
    container.innerHTML = html;
}

function escHtml(str) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(str));
    return d.innerHTML;
}

document.addEventListener('DOMContentLoaded', function () {
    renderLevel(null, 0);
});