import type { MouseEvent } from 'react'
import { ChevronRight } from 'lucide-react'
import { useI18n } from 'archon-ui'

interface DetailsButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void
}

export default function DetailsButton({ onClick }: DetailsButtonProps) {
  const { t } = useI18n()
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-0.5 whitespace-nowrap text-xs font-medium text-primary hover:underline"
    >
      {t('common.action.details')}
      <ChevronRight size={14} />
    </button>
  )
}
