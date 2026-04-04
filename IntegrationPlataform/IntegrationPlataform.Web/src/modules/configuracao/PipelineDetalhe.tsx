import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { GripVertical, Plus, Pencil, Trash2, Workflow, Bug } from 'lucide-react';
import { PageLayout, Badge, Button, Card, CardContent, CardHeader, CardTitle, ConfirmModal, useApi, toast } from 'archon-ui';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import { SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { pipelineService } from '../../services/pipelineService';
import { pipelineEtapaService } from '../../services/pipelineEtapaService';
import { TipoEtapaLabels, AcaoErroLabels } from '../../types/pipeline';
import type { Pipeline, PipelineEtapa } from '../../types/pipeline';
import PipelineEtapaFormModal from '../../components/modals/PipelineEtapaFormModal';
import PipelineDebuggerModal from '../../components/modals/PipelineDebuggerModal';

interface SortableEtapaProps {
  etapa: PipelineEtapa;
  selected: boolean;
  onSelect: (etapaId: number) => void;
  onEdit: (etapa: PipelineEtapa) => void;
  onDelete: (etapa: PipelineEtapa) => void;
}

function SortableEtapa({ etapa, selected, onSelect, onEdit, onDelete }: SortableEtapaProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: etapa.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.7 : 1,
  };

  return (
    <button
      type="button"
      ref={setNodeRef}
      style={style}
      onClick={() => onSelect(etapa.id)}
      className={[
        'w-full rounded-lg border bg-card p-3 text-left transition-all',
        selected ? 'border-primary ring-1 ring-primary/20' : 'border-border hover:border-primary/40 hover:bg-accent/30',
        isDragging ? 'shadow-lg ring-2 ring-primary/25' : '',
      ].join(' ')}
    >
      <div className="flex items-start gap-3">
        <span
          className="self-center cursor-grab touch-none text-muted-foreground hover:text-foreground"
          {...attributes}
          {...listeners}
          onClick={(event) => event.stopPropagation()}
        >
          <GripVertical size={18} />
        </span>

        <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
          {etapa.ordem}
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate text-sm font-semibold text-foreground">{etapa.nome}</span>
            <Badge variant={etapa.ativo ? 'success' : 'destructive'}>{etapa.ativo ? 'Ativo' : 'Inativo'}</Badge>
          </div>

          <div className="mt-1.5 flex flex-wrap gap-1.5 text-xs">
            <Badge variant="outline">{TipoEtapaLabels[etapa.tipo]}</Badge>
            <Badge variant="outline">Ao erro: {AcaoErroLabels[etapa.aoErro]}</Badge>
            {etapa.ignorarNoRetorno && <Badge variant="outline">Ignorar retorno</Badge>}
          </div>
        </div>

        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              onEdit(etapa);
            }}
          >
            <Pencil size={14} />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              onDelete(etapa);
            }}
          >
            <Trash2 size={14} className="text-destructive" />
          </Button>
        </div>
      </div>
    </button>
  );
}

export default function PipelineDetalhe() {
  const { id } = useParams<{ id: string }>();
  const [pipeline, setPipeline] = useState<Pipeline | null>(null);
  const [etapas, setEtapas] = useState<PipelineEtapa[]>([]);
  const [selectedEtapaId, setSelectedEtapaId] = useState<number | null>(null);
  const [isEtapaFormOpen, setIsEtapaFormOpen] = useState(false);
  const [editingEtapa, setEditingEtapa] = useState<PipelineEtapa | null>(null);
  const [deletingEtapa, setDeletingEtapa] = useState<PipelineEtapa | null>(null);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isDebuggerOpen, setIsDebuggerOpen] = useState(false);

  const { execute: fetchPipeline } = useApi<Pipeline>({ showErrorMessage: true });
  const { execute: fetchEtapas } = useApi<PipelineEtapa[]>({ showErrorMessage: true });
  const { execute: reorderEtapas } = useApi({ showSuccessMessage: true, showErrorMessage: true });
  const { execute: deleteEtapa } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: 'Removido', description: 'Etapa removida com sucesso', variant: 'success' });
    },
  });

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const pipelineId = parseInt(id || '0');

  const selectedEtapa = useMemo(
    () => etapas.find((etapa) => etapa.id === selectedEtapaId) ?? null,
    [etapas, selectedEtapaId]
  );

  const loadPipeline = async () => {
    const result = await fetchPipeline(() => pipelineService.getById(pipelineId));
    if (result) {
      setPipeline(result);
    }
  };

  const loadEtapas = async () => {
    const result = await fetchEtapas(() => pipelineEtapaService.getByPipeline(pipelineId));
    if (result) {
      const sorted = [...result].sort((a, b) => a.ordem - b.ordem);
      setEtapas(sorted);

      if (sorted.length === 0) {
        setSelectedEtapaId(null);
        return;
      }

      setSelectedEtapaId((prev) => {
        if (prev && sorted.some((etapa) => etapa.id === prev)) {
          return prev;
        }
        return sorted[0].id;
      });
    }
  };

  useEffect(() => {
    if (pipelineId) {
      loadPipeline();
      loadEtapas();
    }
  }, [pipelineId]);

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;

    if (!over || active.id === over.id) {
      return;
    }

    const oldIndex = etapas.findIndex((etapa) => etapa.id === active.id);
    const newIndex = etapas.findIndex((etapa) => etapa.id === over.id);

    if (oldIndex < 0 || newIndex < 0) {
      return;
    }

    const previousEtapas = [...etapas];
    const reorderedEtapas = [...etapas];
    const [removed] = reorderedEtapas.splice(oldIndex, 1);
    reorderedEtapas.splice(newIndex, 0, removed);

    const normalized = reorderedEtapas.map((etapa, index) => ({ ...etapa, ordem: index + 1 }));
    setEtapas(normalized);

    try {
      await reorderEtapas(() =>
        pipelineEtapaService.reorder(normalized.map((etapa) => ({ id: etapa.id, ordem: etapa.ordem })))
      );
    } catch {
      setEtapas(previousEtapas);
      loadEtapas();
    }
  };

  const handleAddEtapa = () => {
    setEditingEtapa(null);
    setIsEtapaFormOpen(true);
  };

  const handleEditEtapa = (etapa: PipelineEtapa) => {
    setEditingEtapa(etapa);
    setIsEtapaFormOpen(true);
  };

  const handleDeleteEtapa = (etapa: PipelineEtapa) => {
    setDeletingEtapa(etapa);
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingEtapa) {
      return;
    }

    await deleteEtapa(() => pipelineEtapaService.delete(deletingEtapa.id));

    setIsConfirmOpen(false);
    setDeletingEtapa(null);
    loadEtapas();
  };

  const handleEtapaFormSuccess = () => {
    setIsEtapaFormOpen(false);
    setEditingEtapa(null);
    loadEtapas();
  };

  return (
    <PageLayout
      title={pipeline?.nome || 'Pipeline'}
      subtitle="Builder linear de etapas"
      onRefresh={() => {
        loadPipeline();
        loadEtapas();
      }}
      showDefaultActions={false}
      actions={[
        {
          key: 'debugger',
          label: 'Debugger',
          icon: <Bug size={16} />,
          variant: 'outline-danger',
          onClick: () => setIsDebuggerOpen(true),
        },
        {
          key: 'new-step',
          label: 'Nova Etapa',
          icon: <Plus size={16} />,
          variant: 'secondary',
          onClick: handleAddEtapa,
        },
      ]}
    >
      <div className="space-y-4">
        {pipeline && (
          <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-3">
            <div>
              <span className="text-xs text-muted-foreground">Identificador</span>
              <p className="font-medium">{pipeline.identificador}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Integração</span>
              <p className="font-medium">{pipeline.integracao?.nome || '-'}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Status</span>
              <div className="mt-1">
                <Badge variant={pipeline.ativo ? 'success' : 'destructive'}>
                  {pipeline.ativo ? 'Ativo' : 'Inativo'}
                </Badge>
              </div>
            </div>
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-12">
          <Card className="lg:col-span-5">
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Etapas</CardTitle>
              <p className="text-sm text-muted-foreground">Arraste para reordenar a sequência do pipeline.</p>
            </CardHeader>
            <CardContent>
              {etapas.length === 0 ? (
                <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-10 text-muted-foreground">
                  <Workflow size={42} className="mb-3 opacity-50" />
                  <p>Nenhuma etapa configurada</p>
                </div>
              ) : (
                <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                  <SortableContext items={etapas.map((etapa) => etapa.id)} strategy={verticalListSortingStrategy}>
                    <div className="space-y-2">
                      {etapas.map((etapa) => (
                        <SortableEtapa
                          key={etapa.id}
                          etapa={etapa}
                          selected={selectedEtapaId === etapa.id}
                          onSelect={setSelectedEtapaId}
                          onEdit={handleEditEtapa}
                          onDelete={handleDeleteEtapa}
                        />
                      ))}
                    </div>
                  </SortableContext>
                </DndContext>
              )}
            </CardContent>
          </Card>

          <Card className="lg:col-span-7">
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Detalhes da Etapa</CardTitle>
            </CardHeader>
            <CardContent>
              {!selectedEtapa ? (
                <div className="flex min-h-[240px] items-center justify-center rounded-lg border border-dashed text-sm text-muted-foreground">
                  Selecione uma etapa para visualizar os detalhes.
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-lg font-semibold">{selectedEtapa.nome}</p>
                      <p className="text-sm text-muted-foreground">Ordem {selectedEtapa.ordem}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button size="sm" variant="outline" onClick={() => handleEditEtapa(selectedEtapa)}>
                        <Pencil size={14} className="mr-1.5" />
                        Editar
                      </Button>
                      <Button size="sm" variant="outline-danger" onClick={() => handleDeleteEtapa(selectedEtapa)}>
                        <Trash2 size={14} className="mr-1.5" />
                        Excluir
                      </Button>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <Badge variant={selectedEtapa.ativo ? 'success' : 'destructive'}>
                      {selectedEtapa.ativo ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <Badge variant="outline">{TipoEtapaLabels[selectedEtapa.tipo]}</Badge>
                    <Badge variant="outline">Ao erro: {AcaoErroLabels[selectedEtapa.aoErro]}</Badge>
                    {selectedEtapa.ignorarNoRetorno && <Badge variant="outline">Ignorar retorno</Badge>}
                  </div>

                  <div className="grid gap-3 rounded-lg border p-3 md:grid-cols-2">
                    <div>
                      <p className="text-xs text-muted-foreground">Chamada API</p>
                      <p className="text-sm font-medium">{selectedEtapa.chamadaApi?.nome || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">Função JavaScript</p>
                      <p className="text-sm font-medium">{selectedEtapa.funcaoJavaScript?.nome || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">Script SQL</p>
                      <p className="text-sm font-medium">{selectedEtapa.scriptBancoDados?.nome || '-'}</p>
                    </div>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      <ConfirmModal
        open={isConfirmOpen}
        onOpenChange={setIsConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Excluir Etapa"
        description={`Tem certeza que deseja excluir a etapa "${deletingEtapa?.nome}"?`}
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />

      {pipeline && (
        <PipelineEtapaFormModal
          open={isEtapaFormOpen}
          onOpenChange={setIsEtapaFormOpen}
          etapa={editingEtapa}
          pipelineId={pipelineId}
          integracaoId={pipeline.integracao?.id || 0}
          nextOrdem={etapas.length + 1}
          onSuccess={handleEtapaFormSuccess}
        />
      )}

      {pipeline && (
        <PipelineDebuggerModal
          open={isDebuggerOpen}
          onOpenChange={setIsDebuggerOpen}
          pipelineId={pipelineId}
          integracaoId={pipeline.integracao?.id || 0}
          etapas={etapas}
          initialEtapaId={selectedEtapaId}
        />
      )}
    </PageLayout>
  );
}
