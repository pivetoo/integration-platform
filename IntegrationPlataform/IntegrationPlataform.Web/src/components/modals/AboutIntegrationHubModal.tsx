import type { ReactNode } from 'react';
import { Activity, GitBranch, Plug2, ShieldCheck, Workflow } from 'lucide-react';
import { Badge, Modal, ModalContent, ModalDescription, ModalHeader, ModalTitle } from 'd-rts';

interface AboutIntegrationHubModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  systemName: string;
  companyName: string;
  userName: string;
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border bg-card/80 px-3 py-2">
      <p className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="text-sm font-medium text-foreground">{value}</p>
    </div>
  );
}

function FeatureItem({
  icon,
  title,
  description,
}: {
  icon: ReactNode;
  title: string;
  description: string;
}) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <div className="mb-2 flex items-center gap-2">
        <span className="flex h-7 w-7 items-center justify-center rounded-md bg-primary/10 text-primary">{icon}</span>
        <p className="text-sm font-semibold text-foreground">{title}</p>
      </div>
      <p className="text-xs text-muted-foreground">{description}</p>
    </div>
  );
}

export default function AboutIntegrationHubModal({
  open,
  onOpenChange,
  systemName,
  companyName,
  userName,
}: AboutIntegrationHubModalProps) {
  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="2xl" className="p-0 overflow-hidden">
        <div className="border-b bg-gradient-to-r from-primary/12 via-primary/5 to-transparent px-6 py-5">
          <ModalHeader className="space-y-2">
            <div className="flex items-center gap-2">
              <ModalTitle>Sobre o Hub de Integrações</ModalTitle>
              <Badge variant="destructive">Plataforma de Integração</Badge>
            </div>
            <ModalDescription>
              Centro de configuração e execução de integrações com pipelines, conectores, chamadas de API e monitoramento
              operacional.
            </ModalDescription>
          </ModalHeader>
        </div>

        <div className="space-y-5 px-6 py-5">
          <div className="grid gap-2 md:grid-cols-3">
            <InfoItem label="Sistema" value={systemName} />
            <InfoItem label="Empresa" value={companyName || '-'} />
            <InfoItem label="Usuário" value={userName || '-'} />
          </div>

          <div className="grid gap-3 md:grid-cols-2">
            <FeatureItem
              icon={<Workflow size={16} />}
              title="Pipelines"
              description="Orquestra fluxos de etapas com ordenação, regras de erro e processamento sequencial."
            />
            <FeatureItem
              icon={<Plug2 size={16} />}
              title="Conectores"
              description="Define integrações com sistemas externos e parâmetros de autenticação/comunicação."
            />
            <FeatureItem
              icon={<GitBranch size={16} />}
              title="Integrações"
              description="Agrupa recursos por domínio de negócio e centraliza a governança das conexões."
            />
            <FeatureItem
              icon={<Activity size={16} />}
              title="Operação"
              description="Acompanha execuções, filas de processamento e referências para auditoria operacional."
            />
          </div>

          <div className="rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-xs text-muted-foreground">
            <div className="mb-1 flex items-center gap-2 text-foreground">
              <ShieldCheck size={14} className="text-primary" />
              <span className="font-semibold">Boas práticas</span>
            </div>
            Use pipelines claros, com nomes padronizados e tratamento de erro explícito em cada etapa para facilitar manutenção
            e observabilidade.
          </div>
        </div>
      </ModalContent>
    </Modal>
  );
}
