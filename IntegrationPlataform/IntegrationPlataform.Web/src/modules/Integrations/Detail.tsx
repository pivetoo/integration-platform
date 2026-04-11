import { useEffect, useMemo, useRef, useState, type ComponentType, type MouseEvent as ReactMouseEvent } from 'react'
import { useParams } from 'react-router-dom'
import { PageLayout, useApi } from 'archon-ui'
import { Database, Globe, Play, SquareCode, Workflow } from 'lucide-react'
import { integrationService } from '../../services/integrationService'
import { pipelineService } from '../../services/pipelineService'
import { pipelineStepService } from '../../services/pipelineStepService'
import type { Integration } from '../../types/integration'
import type { Pipeline, PipelineStep, PipelineStepType } from '../../types/pipeline'
import { PipelineStepType as PipelineStepTypeEnum } from '../../types/pipeline'

type AnchorSide = 'top' | 'right' | 'bottom' | 'left'

type NodePosition = {
  x: number
  y: number
}

type ConnectionEdge = {
  id: string
  sourceKey: string
  targetKey: string
  sourceAnchor: AnchorSide
  targetAnchor: AnchorSide
}

type CanvasNodeModel = {
  key: string
  title: string
  x: number
  y: number
  size: number
  selected: boolean
  icon: ComponentType<{ size?: number; className?: string }>
  iconClassName: string
  borderClassName: string
  onClick?: () => void
  onMouseDown?: (event: ReactMouseEvent<HTMLButtonElement>) => void
}

const NODE_SIZE = 72
const LABEL_HEIGHT = 14
const LABEL_GAP = 6

function getStepAppearance(type?: PipelineStepType) {
  switch (type) {
    case PipelineStepTypeEnum.HttpRequest:
      return {
        icon: Globe,
        iconClassName: 'text-sky-600',
        borderClassName: 'border-slate-300',
      }
    case PipelineStepTypeEnum.ExecuteScript:
      return {
        icon: Database,
        iconClassName: 'text-emerald-600',
        borderClassName: 'border-slate-300',
      }
    case PipelineStepTypeEnum.JavaScriptFunction:
      return {
        icon: SquareCode,
        iconClassName: 'text-amber-600',
        borderClassName: 'border-slate-300',
      }
    default:
      return {
        icon: Workflow,
        iconClassName: 'text-primary',
        borderClassName: 'border-slate-300',
      }
  }
}

function chooseAnchors(from: NodePosition, to: NodePosition): { sourceAnchor: AnchorSide; targetAnchor: AnchorSide } {
  const dx = to.x - from.x
  const dy = to.y - from.y

  if (Math.abs(dx) >= Math.abs(dy)) {
    return dx >= 0
      ? { sourceAnchor: 'right', targetAnchor: 'left' }
      : { sourceAnchor: 'left', targetAnchor: 'right' }
  }

  return dy >= 0
    ? { sourceAnchor: 'bottom', targetAnchor: 'top' }
    : { sourceAnchor: 'top', targetAnchor: 'bottom' }
}

function getButtonRect(node: CanvasNodeModel) {
  return {
    x: node.x,
    y: node.y + LABEL_HEIGHT + LABEL_GAP,
    width: node.size,
    height: node.size,
  }
}

function getAnchorPoint(node: CanvasNodeModel, anchor: AnchorSide) {
  const rect = getButtonRect(node)

  switch (anchor) {
    case 'top':
      return { x: rect.x + rect.width / 2, y: rect.y }
    case 'right':
      return { x: rect.x + rect.width, y: rect.y + rect.height / 2 }
    case 'bottom':
      return { x: rect.x + rect.width / 2, y: rect.y + rect.height }
    case 'left':
      return { x: rect.x, y: rect.y + rect.height / 2 }
  }
}

function CanvasCurve({ start, end }: { start: { x: number; y: number }; end: { x: number; y: number } }) {
  const dx = end.x - start.x
  const dy = end.y - start.y
  const horizontalControl = Math.max(Math.abs(dx) * 0.45, 45)
  const verticalControl = Math.max(Math.abs(dy) * 0.2, 20)
  const c1x = start.x + (dx >= 0 ? horizontalControl : -horizontalControl)
  const c1y = start.y + (Math.abs(dx) >= Math.abs(dy) ? 0 : verticalControl)
  const c2x = end.x - (dx >= 0 ? horizontalControl : -horizontalControl)
  const c2y = end.y - (Math.abs(dx) >= Math.abs(dy) ? 0 : verticalControl)

  return (
    <path
      d={`M ${start.x} ${start.y} C ${c1x} ${c1y}, ${c2x} ${c2y}, ${end.x} ${end.y}`}
      fill="none"
      stroke="rgb(148 163 184)"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeDasharray="5 5"
    />
  )
}

function CanvasNode({
  node,
  onHandleMouseDown,
  onHandleMouseUp,
}: {
  node: CanvasNodeModel
  onHandleMouseDown: (nodeKey: string, anchor: AnchorSide, event: ReactMouseEvent<HTMLButtonElement>) => void
  onHandleMouseUp: (nodeKey: string, anchor: AnchorSide, event: ReactMouseEvent<HTMLButtonElement>) => void
}) {
  const Icon = node.icon
  const handleClassName =
    'absolute z-10 h-2.5 w-2.5 rounded-full border border-slate-400 bg-white shadow-sm transition-colors hover:bg-slate-100'

  return (
    <div
      className="absolute flex flex-col items-center gap-1 text-center"
      style={{ left: node.x, top: node.y, width: node.size }}
    >
      <div className="max-w-full truncate text-[9px] font-medium text-foreground">
        {node.title}
      </div>

      <div className="relative" style={{ width: node.size, height: node.size }}>
        <button
          type="button"
          onClick={node.onClick}
          onMouseDown={node.onMouseDown}
          className={`absolute inset-0 flex cursor-move items-center justify-center rounded-2xl border bg-white shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md ${node.borderClassName} ${node.selected ? 'ring-2 ring-primary shadow-md' : ''}`}
        >
          <Icon size={22} className={node.iconClassName} />
        </button>

        <button
          type="button"
          className={`${handleClassName} left-1/2 top-0 -translate-x-1/2 -translate-y-1/2`}
          onMouseDown={(event) => onHandleMouseDown(node.key, 'top', event)}
          onMouseUp={(event) => onHandleMouseUp(node.key, 'top', event)}
        />
        <button
          type="button"
          className={`${handleClassName} right-0 top-1/2 translate-x-1/2 -translate-y-1/2`}
          onMouseDown={(event) => onHandleMouseDown(node.key, 'right', event)}
          onMouseUp={(event) => onHandleMouseUp(node.key, 'right', event)}
        />
        <button
          type="button"
          className={`${handleClassName} bottom-0 left-1/2 -translate-x-1/2 translate-y-1/2`}
          onMouseDown={(event) => onHandleMouseDown(node.key, 'bottom', event)}
          onMouseUp={(event) => onHandleMouseUp(node.key, 'bottom', event)}
        />
        <button
          type="button"
          className={`${handleClassName} left-0 top-1/2 -translate-x-1/2 -translate-y-1/2`}
          onMouseDown={(event) => onHandleMouseDown(node.key, 'left', event)}
          onMouseUp={(event) => onHandleMouseUp(node.key, 'left', event)}
        />
      </div>
    </div>
  )
}

function buildAutoLayout(pipelineId: number, steps: PipelineStep[]): Record<string, NodePosition> {
  const baseKey = `pipeline-${pipelineId}`
  const layout: Record<string, NodePosition> = {}

  const columns = 5
  const stepStartX = 40
  const columnGap = 105
  const rowGap = 115
  const topY = 45

  steps.forEach((step, index) => {
    const row = Math.floor(index / columns)
    const rawColumn = index % columns
    const column = row % 2 === 0 ? rawColumn : columns - 1 - rawColumn

    layout[`${baseKey}-step-${step.id}`] = {
      x: stepStartX + column * columnGap,
      y: topY + row * rowGap,
    }
  })

  return layout
}

function buildDefaultEdges(pipelineId: number, steps: PipelineStep[], positions: Record<string, NodePosition>): ConnectionEdge[] {
  const baseKey = `pipeline-${pipelineId}`
  const orderedKeys = steps.map((step) => `${baseKey}-step-${step.id}`)
  const edges: ConnectionEdge[] = []

  for (let index = 0; index < orderedKeys.length - 1; index++) {
    const sourceKey = orderedKeys[index]
    const targetKey = orderedKeys[index + 1]
    const sourcePosition = positions[sourceKey]
    const targetPosition = positions[targetKey]

    if (!sourcePosition || !targetPosition) continue

    const { sourceAnchor, targetAnchor } = chooseAnchors(sourcePosition, targetPosition)

    edges.push({
      id: `${sourceKey}-${targetKey}`,
      sourceKey,
      targetKey,
      sourceAnchor,
      targetAnchor,
    })
  }

  return edges
}

export default function IntegrationWorkspaceDetail() {
  const { id } = useParams<{ id: string }>()
  const integrationId = Number(id)

  const [integration, setIntegration] = useState<Integration | null>(null)
  const [stepsByPipeline, setStepsByPipeline] = useState<Record<number, PipelineStep[]>>({})
  const [selectedPipelineId, setSelectedPipelineId] = useState<number | null>(null)
  const [selectedStep, setSelectedStep] = useState<PipelineStep | null>(null)
  const [nodePositions, setNodePositions] = useState<Record<string, NodePosition>>({})
  const [edgesByPipeline, setEdgesByPipeline] = useState<Record<number, ConnectionEdge[]>>({})
  const [connectionPreview, setConnectionPreview] = useState<{
    sourceKey: string
    sourceAnchor: AnchorSide
    x: number
    y: number
  } | null>(null)

  const canvasRef = useRef<HTMLDivElement | null>(null)
  const dragRef = useRef<{
    key: string
    startX: number
    startY: number
    initialX: number
    initialY: number
  } | null>(null)
  const connectionDragRef = useRef<{
    sourceKey: string
    sourceAnchor: AnchorSide
  } | null>(null)

  const { execute: fetchIntegration } = useApi<Integration>({ showErrorMessage: true })
  const { execute: fetchPipelines } = useApi<Pipeline[]>({ showErrorMessage: true })
  const { execute: fetchSteps } = useApi<PipelineStep[]>({ showErrorMessage: true })

  useEffect(() => {
    if (!integrationId) return

    void fetchIntegration(() => integrationService.getById(integrationId)).then((result) => {
      if (result) setIntegration(result)
    })

    void fetchPipelines(() => pipelineService.getByIntegration(integrationId)).then(async (result) => {
      if (!result) return

      const orderedPipelines = [...result].sort((a, b) => a.name.localeCompare(b.name))
      setSelectedPipelineId((current) => current ?? orderedPipelines[0]?.id ?? null)

      const nextStepsByPipeline: Record<number, PipelineStep[]> = {}

      for (const pipeline of orderedPipelines) {
        const steps = await fetchSteps(() => pipelineStepService.getByPipeline(pipeline.id))
        if (steps) {
          nextStepsByPipeline[pipeline.id] = [...steps].sort((a, b) => a.order - b.order)
        }
      }

      setStepsByPipeline(nextStepsByPipeline)
    })
  }, [integrationId])

  const selectedPipelineSteps = useMemo(
    () => (selectedPipelineId ? stepsByPipeline[selectedPipelineId] || [] : []),
    [stepsByPipeline, selectedPipelineId],
  )

  useEffect(() => {
    if (!selectedPipelineSteps.length) {
      setSelectedStep(null)
      return
    }

    setSelectedStep((current) => current && selectedPipelineSteps.some((step) => step.id === current.id)
      ? current
      : selectedPipelineSteps[0])
  }, [selectedPipelineSteps])

  useEffect(() => {
    if (!selectedPipelineId) return

    const nextPositions = buildAutoLayout(selectedPipelineId, selectedPipelineSteps)
    setNodePositions((current) => ({
      ...current,
      ...nextPositions,
    }))

    setEdgesByPipeline((current) => ({
      ...current,
      [selectedPipelineId]: current[selectedPipelineId] && current[selectedPipelineId].length > 0
        ? current[selectedPipelineId]
        : buildDefaultEdges(selectedPipelineId, selectedPipelineSteps, nextPositions),
    }))
  }, [selectedPipelineId, selectedPipelineSteps])

  const handleAutoArrange = () => {
    if (!selectedPipelineId) return

    const nextPositions = buildAutoLayout(selectedPipelineId, selectedPipelineSteps)
    setNodePositions((current) => ({
      ...current,
      ...nextPositions,
    }))

    setEdgesByPipeline((current) => ({
      ...current,
      [selectedPipelineId]: buildDefaultEdges(selectedPipelineId, selectedPipelineSteps, nextPositions),
    }))
  }

  const handleResetSelection = () => {
    setSelectedStep(null)
  }

  useEffect(() => {
    const handleMouseMove = (event: MouseEvent) => {
      const nodeDrag = dragRef.current
      if (nodeDrag) {
        const deltaX = event.clientX - nodeDrag.startX
        const deltaY = event.clientY - nodeDrag.startY

        setNodePositions((current) => ({
          ...current,
          [nodeDrag.key]: {
            x: Math.max(20, nodeDrag.initialX + deltaX),
            y: Math.max(20, nodeDrag.initialY + deltaY),
          },
        }))

        return
      }

      const connectionDrag = connectionDragRef.current
      if (connectionDrag && canvasRef.current) {
        const bounds = canvasRef.current.getBoundingClientRect()
        setConnectionPreview({
          sourceKey: connectionDrag.sourceKey,
          sourceAnchor: connectionDrag.sourceAnchor,
          x: event.clientX - bounds.left,
          y: event.clientY - bounds.top,
        })
      }
    }

    const handleMouseUp = () => {
      dragRef.current = null
      connectionDragRef.current = null
      setConnectionPreview(null)
    }

    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('mouseup', handleMouseUp)

    return () => {
      window.removeEventListener('mousemove', handleMouseMove)
      window.removeEventListener('mouseup', handleMouseUp)
    }
  }, [])

  const baseKey = selectedPipelineId ? `pipeline-${selectedPipelineId}` : 'pipeline-0'

  const canvasNodes = useMemo<CanvasNodeModel[]>(() => {
    return selectedPipelineSteps.map((step) => {
      const appearance = getStepAppearance(step.type)
      const key = `${baseKey}-step-${step.id}`
      const position = nodePositions[key] ?? { x: 40, y: 45 }

      return {
        key,
        title: step.name,
        x: position.x,
        y: position.y,
        size: NODE_SIZE,
        selected: selectedStep?.id === step.id,
        icon: appearance.icon,
        iconClassName: appearance.iconClassName,
        borderClassName: appearance.borderClassName,
        onClick: () => setSelectedStep(step),
        onMouseDown: (event) => {
          dragRef.current = {
            key,
            startX: event.clientX,
            startY: event.clientY,
            initialX: position.x,
            initialY: position.y,
          }
        },
      }
    })
  }, [baseKey, nodePositions, selectedPipelineSteps, selectedStep])

  const activeEdges = selectedPipelineId ? edgesByPipeline[selectedPipelineId] || [] : []

  const canvasWidth = useMemo(() => {
    const maxRight = canvasNodes.reduce((max, node) => Math.max(max, node.x + node.size), 0)
    return Math.max(960, maxRight + 120)
  }, [canvasNodes])

  const canvasHeight = useMemo(() => {
    const maxBottom = canvasNodes.reduce((max, node) => Math.max(max, node.y + node.size + LABEL_HEIGHT + LABEL_GAP), 0)
    return Math.max(360, maxBottom + 80)
  }, [canvasNodes])

  const handleHandleMouseDown = (nodeKey: string, anchor: AnchorSide, event: ReactMouseEvent<HTMLButtonElement>) => {
    event.stopPropagation()
    event.preventDefault()
    connectionDragRef.current = { sourceKey: nodeKey, sourceAnchor: anchor }

    if (canvasRef.current) {
      const bounds = canvasRef.current.getBoundingClientRect()
      setConnectionPreview({
        sourceKey: nodeKey,
        sourceAnchor: anchor,
        x: event.clientX - bounds.left,
        y: event.clientY - bounds.top,
      })
    }
  }

  const handleHandleMouseUp = (targetKey: string, targetAnchor: AnchorSide, event: ReactMouseEvent<HTMLButtonElement>) => {
    event.stopPropagation()
    event.preventDefault()

    if (!selectedPipelineId || !connectionDragRef.current) return

    const { sourceKey, sourceAnchor } = connectionDragRef.current
    if (sourceKey === targetKey) {
      connectionDragRef.current = null
      setConnectionPreview(null)
      return
    }

    setEdgesByPipeline((current) => {
      const currentEdges = current[selectedPipelineId] || []
      const filtered = currentEdges.filter((edge) => edge.sourceKey !== sourceKey)

      return {
        ...current,
        [selectedPipelineId]: [
          ...filtered,
          {
            id: `${sourceKey}-${targetKey}`,
            sourceKey,
            targetKey,
            sourceAnchor,
            targetAnchor,
          },
        ],
      }
    })

    connectionDragRef.current = null
    setConnectionPreview(null)
  }

  const nodeMap = useMemo(() => new Map(canvasNodes.map((node) => [node.key, node])), [canvasNodes])

  return (
    <PageLayout
      title={integration?.name || 'Studio da integração'}
      onRefresh={() => window.location.reload()}
      actions={[
        {
          key: 'organizar-canvas',
          label: 'Organizar canvas',
          icon: <Workflow size={16} />,
          variant: 'outline',
          onClick: handleAutoArrange,
        },
        {
          key: 'limpar-selecao',
          label: 'Limpar seleção',
          icon: <Workflow size={16} />,
          variant: 'outline',
          onClick: handleResetSelection,
        },
        {
          key: 'testar-fluxo',
          label: 'Testar fluxo',
          icon: <Play size={16} />,
          variant: 'outline',
          onClick: () => undefined,
        },
      ]}
    >
      <div className="overflow-x-auto rounded-2xl">
        <div
          ref={canvasRef}
          className="relative min-w-full"
          style={{
            width: canvasWidth,
            minWidth: '100%',
            height: canvasHeight,
            backgroundImage: 'radial-gradient(circle, rgba(148,163,184,0.45) 1px, transparent 1px)',
            backgroundSize: '18px 18px',
            backgroundColor: 'rgba(248,250,252,0.55)',
          }}
        >
          <svg className="absolute inset-0 pointer-events-none overflow-visible" style={{ width: '100%', height: '100%' }}>
            {activeEdges.map((edge) => {
              const sourceNode = nodeMap.get(edge.sourceKey)
              const targetNode = nodeMap.get(edge.targetKey)
              if (!sourceNode || !targetNode) return null

              return (
                <CanvasCurve
                  key={edge.id}
                  start={getAnchorPoint(sourceNode, edge.sourceAnchor)}
                  end={getAnchorPoint(targetNode, edge.targetAnchor)}
                />
              )
            })}

            {connectionPreview && (() => {
              const sourceNode = nodeMap.get(connectionPreview.sourceKey)
              if (!sourceNode) return null

              return (
                <CanvasCurve
                  start={getAnchorPoint(sourceNode, connectionPreview.sourceAnchor)}
                  end={{ x: connectionPreview.x, y: connectionPreview.y }}
                />
              )
            })()}
          </svg>

          {canvasNodes.map((node) => (
            <CanvasNode
              key={node.key}
              node={node}
              onHandleMouseDown={handleHandleMouseDown}
              onHandleMouseUp={handleHandleMouseUp}
            />
          ))}
        </div>
      </div>

    </PageLayout>
  )
}
