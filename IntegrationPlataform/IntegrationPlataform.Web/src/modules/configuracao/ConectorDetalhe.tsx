import { useState, useEffect, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { Link2, Save, Eye, EyeOff } from 'lucide-react';
import { PageLayout, Badge, Button, Input, Checkbox, useApi, toast } from 'archon-ui';
import { conectorService } from '../../services/conectorService';
import { integracaoAtributoService } from '../../services/integracaoAtributoService';
import { conectorAtributoValorService } from '../../services/conectorAtributoValorService';
import type { Conector } from '../../types/conector';
import type { IntegracaoAtributo } from '../../types/integracaoAtributo';
import { TipoCampo } from '../../types/integracaoAtributo';
import type { ConectorAtributoValor } from '../../types/conectorAtributoValor';

export default function ConectorDetalhe() {
  const { id } = useParams<{ id: string }>();
  const [conector, setConector] = useState<Conector | null>(null);
  const [atributos, setAtributos] = useState<IntegracaoAtributo[]>([]);
  const [valores, setValores] = useState<ConectorAtributoValor[]>([]);
  const [formValues, setFormValues] = useState<Record<number, string>>({});
  const [visibleSensitiveFields, setVisibleSensitiveFields] = useState<Record<number, boolean>>({});

  const { execute: fetchConector } = useApi<Conector>({
    showErrorMessage: true,
  });

  const { execute: fetchAtributos } = useApi<IntegracaoAtributo[]>({
    showErrorMessage: true,
  });

  const { execute: fetchValores } = useApi<ConectorAtributoValor[]>({
    showErrorMessage: true,
  });

  const { execute: saveValor, loading: saving } = useApi({
    showErrorMessage: true,
  });

  const conectorId = parseInt(id || '0');

  const loadConector = async () => {
    const result = await fetchConector(() => conectorService.getById(conectorId));
    if (result) {
      setConector(result);
    }
  };

  const loadAtributos = async (integracaoId: number) => {
    const result = await fetchAtributos(() => integracaoAtributoService.getByIntegracao(integracaoId));
    if (result) {
      const sorted = [...result].sort((a, b) => a.ordem - b.ordem);
      setAtributos(sorted);
    }
  };

  const loadValores = async () => {
    const result = await fetchValores(() => conectorAtributoValorService.getByConector(conectorId));
    if (result) {
      setValores(result);
    }
  };

  useEffect(() => {
    if (conectorId) {
      loadConector();
      loadValores();
    }
  }, [conectorId]);

  useEffect(() => {
    if (conector?.integracao?.id) {
      loadAtributos(conector.integracao.id);
    }
  }, [conector?.integracao?.id]);

  useEffect(() => {
    const initial: Record<number, string> = {};
    for (const atributo of atributos) {
      const existing = valores.find(v => v.integracaoAtributoId === atributo.id);
      if (existing) {
        initial[atributo.id] = existing.valor || '';
      } else {
        initial[atributo.id] = atributo.valorPadrao || '';
      }
    }
    setFormValues(initial);
  }, [atributos, valores]);

  const valorMap = useMemo(() => {
    const map: Record<number, ConectorAtributoValor> = {};
    for (const v of valores) {
      if (v.integracaoAtributoId) {
        map[v.integracaoAtributoId] = v;
      }
    }
    return map;
  }, [valores]);

  const groupedAtributos = useMemo(() => {
    const groups: Record<string, IntegracaoAtributo[]> = {};
    for (const atributo of atributos) {
      const grupo = atributo.grupo || 'Geral';
      if (!groups[grupo]) {
        groups[grupo] = [];
      }
      groups[grupo].push(atributo);
    }
    return groups;
  }, [atributos]);

  const toggleSensitiveVisibility = (atributoId: number) => {
    setVisibleSensitiveFields(prev => ({ ...prev, [atributoId]: !prev[atributoId] }));
  };

  const handleValueChange = (atributoId: number, value: string) => {
    setFormValues(prev => ({ ...prev, [atributoId]: value }));
  };

  const handleSave = async () => {
    let hasError = false;

    for (const atributo of atributos) {
      const value = formValues[atributo.id] || '';
      const existing = valorMap[atributo.id];

      if (atributo.obrigatorio && !value.trim()) {
        toast({ title: 'Erro', description: `O campo "${atributo.label}" e obrigatorio`, variant: 'destructive' });
        hasError = true;
        break;
      }

      if (existing) {
        if (value !== existing.valor) {
          const result = await saveValor(() =>
            conectorAtributoValorService.update(existing.id, {
              integracaoAtributoId: atributo.id,
              valor: value,
            })
          );
          if (!result) {
            hasError = true;
            break;
          }
        }
      } else if (value.trim()) {
        const result = await saveValor(() =>
          conectorAtributoValorService.create({
            conectorId,
            integracaoAtributoId: atributo.id,
            valor: value,
          })
        );
        if (!result) {
          hasError = true;
          break;
        }
      }
    }

    if (!hasError) {
      toast({ title: 'Sucesso', description: 'Atributos salvos com sucesso', variant: 'success' });
      loadValores();
    }
  };

  const renderInput = (atributo: IntegracaoAtributo) => {
    const value = formValues[atributo.id] || '';

    switch (atributo.tipo) {
      case TipoCampo.TextoLongo:
        return (
          <textarea
            className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
            placeholder={atributo.placeholder || ''}
            rows={3}
          />
        );

      case TipoCampo.Numero:
        return (
          <Input
            type="number"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
            placeholder={atributo.placeholder || ''}
          />
        );

      case TipoCampo.Decimal:
        return (
          <Input
            type="number"
            step="0.01"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
            placeholder={atributo.placeholder || ''}
          />
        );

      case TipoCampo.Booleano:
        return (
          <div className="flex items-center space-x-2 pt-2">
            <Checkbox
              id={`atributo-${atributo.id}`}
              checked={value === 'true'}
              onCheckedChange={(checked) => handleValueChange(atributo.id, checked ? 'true' : 'false')}
            />
            <label htmlFor={`atributo-${atributo.id}`} className="text-sm">
              {atributo.placeholder || 'Sim'}
            </label>
          </div>
        );

      case TipoCampo.Data:
        return (
          <Input
            type="date"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
          />
        );

      case TipoCampo.DataHora:
        return (
          <Input
            type="datetime-local"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
          />
        );

      case TipoCampo.Lista:
        return (
          <Input
            type="text"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
            placeholder={atributo.placeholder || ''}
          />
        );

      default:
        if (atributo.sensivel) {
          const isVisible = visibleSensitiveFields[atributo.id] || false;
          return (
            <div className="relative">
              <Input
                type={isVisible ? 'text' : 'password'}
                value={value}
                onChange={(e) => handleValueChange(atributo.id, e.target.value)}
                placeholder={atributo.placeholder || ''}
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => toggleSensitiveVisibility(atributo.id)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {isVisible ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          );
        }
        return (
          <Input
            type="text"
            value={value}
            onChange={(e) => handleValueChange(atributo.id, e.target.value)}
            placeholder={atributo.placeholder || ''}
          />
        );
    }
  };

  return (
    <PageLayout
      title={conector?.nome || 'Conector'}
      onRefresh={() => { loadConector(); loadValores(); }}
    >
      <div className="space-y-6">

        {conector && (
          <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-4">
            <div>
              <span className="text-xs text-muted-foreground">Nome</span>
              <p className="font-medium">{conector.nome}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Integração</span>
              <p className="font-medium">{conector.integracao?.nome || '-'}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Status</span>
              <div className="mt-1">
                <Badge variant={conector.ativo ? 'success' : 'destructive'}>
                  {conector.ativo ? 'Ativo' : 'Inativo'}
                </Badge>
              </div>
            </div>
          </div>
        )}

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">Atributos</h2>
            {atributos.length > 0 && (
              <Button size="sm" onClick={handleSave} disabled={saving}>
                <Save size={16} className="mr-2" />
                {saving ? 'Salvando...' : 'Salvar Atributos'}
              </Button>
            )}
          </div>

          {atributos.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-12 text-muted-foreground">
              <Link2 size={48} className="mb-4 opacity-50" />
              <p>Nenhum atributo configurado</p>
              <p className="text-sm">Esta integração não possui atributos definidos</p>
            </div>
          ) : (
            Object.entries(groupedAtributos).map(([grupo, grupoAtributos]) => (
              <div key={grupo} className="space-y-4">
                {Object.keys(groupedAtributos).length > 1 && (
                  <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">{grupo}</h3>
                )}
                <div className="rounded-lg border bg-card p-4 space-y-4">
                  {grupoAtributos.map((atributo) => (
                    <div key={atributo.id} className="space-y-2">
                      <label className="text-sm font-medium flex items-baseline gap-1">
                        {atributo.label}
                        {atributo.obrigatorio && <span className="text-destructive">*</span>}
                      </label>
                      {atributo.descricao && (
                        <p className="text-sm text-muted-foreground/80 leading-relaxed italic">
                          {atributo.descricao}
                        </p>
                      )}
                      {renderInput(atributo)}
                    </div>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </PageLayout>
  );
}
