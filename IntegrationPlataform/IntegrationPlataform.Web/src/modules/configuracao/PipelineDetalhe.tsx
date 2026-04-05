import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { GripVertical, Plus, Pencil, Trash2, Workflow, Bug } from 'lucide-react';
import { PageLayout, Badge, Button, Card, CardContent, CardHeader, CardTitle, ConfirmModal, useApi, useI18n, toast } from 'archon-ui';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import { SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { pipelineService } from '../../services/pipelineService';
import { pipelineEtapaService } from '../../services/pipelineEtapaService';
import { PipelineStepTypeLabels, ErrorActionLabels } from '../../types/pipeline';
import type { Pipeline, PipelineStep } from '../../types/pipeline';
import PipelineEtapaFormModal from '../../components/modals/PipelineEtapaFormModal';
import PipelineDebuggerModal from '../../components/modals/PipelineDebuggerModal';

interface SortableEtapaProps {
  step: PipelineStep;
  selected: boolean;
  onSelect: (stepId: number) => void;
  onEdit: (step: PipelineStep) => void;
  onDelete: (step: PipelineStep) => void;
}

function SortableEtapa({ step, selected, onSelect, onEdit, onDelete }: SortableEtapaProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: step.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.7 : 1,
  };

  return (
    <div
      role="button"
      tabIndex={0}
      ref={setNodeRef}
      style={style}
      onClick={() => onSelect(step.id)}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onSelect(step.id);
        }
      }}
      className={[
        'w-full rounded-lg border bg-card p-3 text-left transition-all',
        'cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary/30',
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
          {step.order}
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate text-sm font-semibold text-foreground">{step.name}</span>
            <Badge variant={step.isActive ? 'success' : 'destructive'}>{step.isActive ? 'Ativo' : 'Inativo'}</Badge>
          </div>

          <div className="mt-1.5 flex flex-wrap gap-1.5 text-xs">
            <Badge variant="outline">{PipelineStepTypeLabels[step.type]}</Badge>
            <Badge variant="outline">Ao erro: {ErrorActionLabels[step.errorAction]}</Badge>
            {step.ignoreOnResponse && <Badge variant="outline">Ignorar retorno</Badge>}
          </div>
        </div>

        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              onEdit(step);
            }}
          >
            <Pencil size={14} />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              onDelete(step);
            }}
          >
            <Trash2 size={14} className="text-destructive" />
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function PipelineDetalhe() {
  const { t } = useI18n();
  const { id } = useParams<{ id: string }>();
  const [pipeline, setPipeline] = useState<Pipeline | null>(null);
  const [steps, setSteps] = useState<PipelineStep[]>([]);
  const [selectedStepId, setSelectedStepId] = useState<number | null>(null);
  const [isStepFormOpen, setIsStepFormOpen] = useState(false);
  const [editingStep, setEditingStep] = useState<PipelineStep | null>(null);
  const [deletingStep, setDeletingStep] = useState<PipelineStep | null>(null);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isDebuggerOpen, setIsDebuggerOpen] = useState(false);

  const { execute: fetchPipeline } = useApi<Pipeline>({ showErrorMessage: true });
  const { execute: fetchSteps } = useApi<PipelineStep[]>({ showErrorMessage: true });
  const { execute: reorderSteps } = useApi({ showSuccessMessage: true, showErrorMessage: true });
  const { execute: deleteStep } = useApi({
    showSuccessMessage: false,
    showErrorMessage: true,
    onSuccess: () => {
      toast({ title: t('common.toast.removedTitle'), description: t('pipeline.detail.stepRemoved'), variant: 'success' });
    },
  });

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const pipelineId = parseInt(id || '0');

  const selectedStep = useMemo(
    () => steps.find((step) => step.id === selectedStepId) ?? null,
    [steps, selectedStepId]
  );

  const loadPipeline = async () => {
    const result = await fetchPipeline(() => pipelineService.getById(pipelineId));
    if (result) {
      setPipeline(result);
    }
  };

  const loadSteps = async () => {
    const result = await fetchSteps(() => pipelineEtapaService.getByPipeline(pipelineId));
    if (result) {
      const sorted = [...result].sort((a, b) => a.order - b.order);
      setSteps(sorted);

      if (sorted.length === 0) {
        setSelectedStepId(null);
        return;
      }

      setSelectedStepId((prev) => {
        if (prev && sorted.some((step) => step.id === prev)) {
          return prev;
        }
        return sorted[0].id;
      });
    }
  };

  useEffect(() => {
    if (pipelineId) {
      loadPipeline();
      loadSteps();
    }
  }, [pipelineId]);

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;

    if (!over || active.id === over.id) {
      return;
    }

    const oldIndex = steps.findIndex((step) => step.id === active.id);
    const newIndex = steps.findIndex((step) => step.id === over.id);

    if (oldIndex < 0 || newIndex < 0) {
      return;
    }

    const previousSteps = [...steps];
    const reorderedSteps = [...steps];
    const [removed] = reorderedSteps.splice(oldIndex, 1);
    reorderedSteps.splice(newIndex, 0, removed);

    const normalized = reorderedSteps.map((step, index) => ({ ...step, order: index + 1 }));
    setSteps(normalized);

    try {
      await reorderSteps(() =>
        pipelineEtapaService.reorder(normalized.map((step) => ({ id: step.id, order: step.order })))
      );
    } catch {
      setSteps(previousSteps);
      loadSteps();
    }
  };

  const handleAddStep = () => {
    setEditingStep(null);
    setIsStepFormOpen(true);
  };

  const handleEditStep = (step: PipelineStep) => {
    setEditingStep(step);
    setIsStepFormOpen(true);
  };

  const handleDeleteStep = (step: PipelineStep) => {
    setDeletingStep(step);
    setIsConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingStep) {
      return;
    }

    await deleteStep(() => pipelineEtapaService.delete(deletingStep.id));

    setIsConfirmOpen(false);
    setDeletingStep(null);
    loadSteps();
  };

  const handleStepFormSuccess = () => {
    setIsStepFormOpen(false);
    setEditingStep(null);
    loadSteps();
  };

  return (
    <PageLayout
      title={pipeline?.name || t('pipeline.detail.fallbackTitle')}
      subtitle={t('pipeline.detail.subtitle')}
      onRefresh={() => {
        loadPipeline();
        loadSteps();
      }}
      showDefaultActions={false}
      actions={[
        {
          key: 'debugger',
          label: t('pipeline.detail.debuggerAction'),
          icon: <Bug size={16} />,
          variant: 'outline-danger',
          onClick: () => setIsDebuggerOpen(true),
        },
        {
          key: 'new-step',
          label: t('pipeline.detail.newStepAction'),
          icon: <Plus size={16} />,
          variant: 'secondary',
          onClick: handleAddStep,
        },
      ]}
    >
      <div className="space-y-4">
        {pipeline && (
          <div className="grid grid-cols-2 gap-4 rounded-lg border bg-card p-4 md:grid-cols-3">
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.identifier')}</span>
              <p className="font-medium">{pipeline.identifier}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.integration')}</span>
              <p className="font-medium">{pipeline.integration?.name || '-'}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">{t('common.column.status')}</span>
              <div className="mt-1">
                <Badge variant={pipeline.isActive ? 'success' : 'destructive'}>
                  {pipeline.isActive ? t('common.status.active') : t('common.status.inactive')}
                </Badge>
              </div>
            </div>
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-12">
          <Card className="lg:col-span-5">
            <CardHeader className="pb-2">
              <CardTitle className="text-base">{t('pipeline.detail.stepsTitle')}</CardTitle>
              <p className="text-sm text-muted-foreground">{t('pipeline.detail.stepsDescription')}</p>
            </CardHeader>
            <CardContent>
              {steps.length === 0 ? (
                <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-10 text-muted-foreground">
                  <Workflow size={42} className="mb-3 opacity-50" />
                  <p>{t('pipeline.detail.emptyTitle')}</p>
                </div>
              ) : (
                <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                  <SortableContext items={steps.map((step) => step.id)} strategy={verticalListSortingStrategy}>
                    <div className="space-y-2">
                      {steps.map((step) => (
                        <SortableEtapa
                          key={step.id}
                          step={step}
                          selected={selectedStepId === step.id}
                          onSelect={setSelectedStepId}
                          onEdit={handleEditStep}
                          onDelete={handleDeleteStep}
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
              <CardTitle className="text-base">{t('pipeline.detail.stepDetailsTitle')}</CardTitle>
            </CardHeader>
            <CardContent>
              {!selectedStep ? (
                <div className="flex min-h-[240px] items-center justify-center rounded-lg border border-dashed text-sm text-muted-foreground">
                  {t('pipeline.detail.selectStep')}
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-lg font-semibold">{selectedStep.name}</p>
                      <p className="text-sm text-muted-foreground">{t('pipeline.detail.orderLabel')} {selectedStep.order}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button size="sm" variant="outline" onClick={() => handleEditStep(selectedStep)}>
                        <Pencil size={14} className="mr-1.5" />
                        {t('common.action.edit')}
                      </Button>
                      <Button size="sm" variant="outline-danger" onClick={() => handleDeleteStep(selectedStep)}>
                        <Trash2 size={14} className="mr-1.5" />
                        {t('common.action.delete')}
                      </Button>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <Badge variant={selectedStep.isActive ? 'success' : 'destructive'}>
                      {selectedStep.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                    <Badge variant="outline">{PipelineStepTypeLabels[selectedStep.type]}</Badge>
                    <Badge variant="outline">{t('pipeline.detail.onErrorLabel')}: {ErrorActionLabels[selectedStep.errorAction]}</Badge>
                    {selectedStep.ignoreOnResponse && <Badge variant="outline">{t('pipeline.detail.ignoreReturn')}</Badge>}
                  </div>

                  <div className="grid gap-3 rounded-lg border p-3 md:grid-cols-2">
                    <div>
                      <p className="text-xs text-muted-foreground">{t('pipeline.detail.apiCall')}</p>
                      <p className="text-sm font-medium">{selectedStep.apiCall?.name || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">{t('pipeline.detail.javaScriptFunction')}</p>
                      <p className="text-sm font-medium">{selectedStep.javaScriptFunction?.name || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">{t('pipeline.detail.sqlScript')}</p>
                      <p className="text-sm font-medium">{selectedStep.databaseScript?.name || '-'}</p>
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
        title={t('pipeline.detail.deleteStepTitle')}
        description={t('pipeline.detail.deleteStepDescription').replace('{0}', deletingStep?.name || '')}
        confirmText={t('common.action.delete')}
        cancelText={t('common.action.cancel')}
        variant="danger"
      />

      {pipeline && (
        <PipelineEtapaFormModal
          open={isStepFormOpen}
          onOpenChange={setIsStepFormOpen}
          step={editingStep}
          pipelineId={pipelineId}
          integrationId={pipeline.integration?.id || 0}
          nextOrder={steps.length + 1}
          onSuccess={handleStepFormSuccess}
        />
      )}

      {pipeline && (
        <PipelineDebuggerModal
          open={isDebuggerOpen}
          onOpenChange={setIsDebuggerOpen}
          pipelineId={pipelineId}
          integrationId={pipeline.integration?.id || 0}
          steps={steps}
          initialStepId={selectedStepId}
        />
      )}
    </PageLayout>
  );
}
