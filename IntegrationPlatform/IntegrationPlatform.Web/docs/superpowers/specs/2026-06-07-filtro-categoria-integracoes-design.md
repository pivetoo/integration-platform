# Design: Filtro por Categoria na Tela de Integrações

**Data:** 2026-06-07
**Escopo:** `src/modules/Configuration/Integrations/index.tsx`

## Objetivo

Adicionar filtro por categoria à tela de listagem de integrações, seguindo o mesmo padrão do filtro de status já existente.

## Decisões

- Filtro client-side (mesmo padrão do status)
- Single-select dropdown via `FilterPanel` do archon-ui
- Categorias carregadas via `integrationCategoryService.getActive()`

## Mudanças em `Integrations/index.tsx`

**Estado novo:**
```ts
const [categoryFilter, setCategoryFilter] = useState<string>('');
const [categories, setCategories] = useState<IntegrationCategory[]>([]);
```

**Carregamento de categorias no mount:**
```ts
useEffect(() => {
  integrationCategoryService.getActive().then(setCategories);
}, []);
```

**Filtragem local** — adicionar condição ao lado do status em `loadIntegracoes()`:
```ts
const filtered = result.data.filter((i: Integration) => {
  const statusOk = statusFilter === 'active' ? i.isActive : statusFilter === 'inactive' ? !i.isActive : true;
  const categoryOk = categoryFilter ? String(i.integrationCategoryId) === categoryFilter : true;
  return statusOk && categoryOk;
});
```

**Seção no FilterPanel:**
```ts
{
  key: 'category',
  label: t('common.column.category'),
  value: categoryFilter,
  onChange: setCategoryFilter,
  options: categories.map(c => ({ value: String(c.id), label: c.name })),
  allLabel: t('common.filter.all'),
}
```

**Reset de página** — incluir `categoryFilter`:
```ts
useEffect(() => { setPage(1); }, [debouncedSearch, statusFilter, categoryFilter]);
useEffect(() => { void loadIntegracoes(); }, [page, pageSize, debouncedSearch, statusFilter, categoryFilter]);
```

**Clear filters:**
```ts
const clearFilters = () => { setStatusFilter(''); setCategoryFilter(''); };
```

## Imports adicionais

- `IntegrationCategory` já está importado
- `integrationCategoryService` precisa ser importado

## Fora de escopo

- Filtro server-side
- Multi-select de categorias
- Mudanças no backend
