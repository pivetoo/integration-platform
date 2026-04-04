import { useMemo, useState } from 'react';
import { Bot, Minimize2, Send, Sparkles, X } from 'lucide-react';
import { copilotService } from '../../services/copilotService';

type ChatRole = 'user' | 'assistant';

interface ChatMessage {
  id: number;
  role: ChatRole;
  text: string;
}

export default function CopilotChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 1,
      role: 'assistant',
      text: 'Olá, eu sou o Copiloto do IntegrationHub. Descreva a integração que você quer montar.',
    },
  ]);

  const nextId = useMemo(() => messages.length + 1, [messages.length]);

  const handleSend = async () => {
    const text = input.trim();
    if (!text) {
      return;
    }

    const userMessage: ChatMessage = {
      id: nextId,
      role: 'user',
      text,
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');

    setIsLoading(true);
    try {
      const plan = await copilotService.plan({ prompt: text });
      const infoEtapas = plan.draft.etapas.map((e) => `${e.ordem}. ${e.nome}`).join(' | ');
      const missing = plan.missingFields.length > 0 ? `\nCampos faltantes: ${plan.missingFields.join(', ')}` : '';

      const assistantMessage: ChatMessage = {
        id: nextId + 1,
        role: 'assistant',
        text: `${plan.summary}\nIntegração: ${plan.draft.integracao.nome}\nPipeline: ${plan.draft.pipeline.nome}\nEtapas: ${infoEtapas}${missing}`,
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch {
      const assistantMessage: ChatMessage = {
        id: nextId + 1,
        role: 'assistant',
        text: 'Não consegui gerar o plano agora. Verifique se a API do Copiloto está disponível.',
      };
      setMessages((prev) => [...prev, assistantMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed bottom-4 right-4 z-[90]">
      {isOpen ? (
        <div className="w-[380px] max-w-[calc(100vw-1rem)] overflow-hidden rounded-t-xl border border-border bg-card shadow-2xl">
          <div className="flex items-center justify-between border-b border-border bg-primary px-4 py-3 text-primary-foreground">
            <div className="flex items-center gap-2">
              <Bot size={18} />
              <span className="text-sm font-semibold">Copiloto de Integrações</span>
            </div>
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="rounded-md p-1 text-primary-foreground/90 hover:bg-primary-foreground/15"
                aria-label="Minimizar chat"
              >
                <Minimize2 size={16} />
              </button>
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="rounded-md p-1 text-primary-foreground/90 hover:bg-primary-foreground/15"
                aria-label="Fechar chat"
              >
                <X size={16} />
              </button>
            </div>
          </div>

          <div className="h-[420px] space-y-3 overflow-y-auto bg-background px-4 py-3">
            {messages.map((message) => (
              <div
                key={message.id}
                className={`max-w-[90%] rounded-lg px-3 py-2 text-sm ${
                  message.role === 'assistant'
                    ? 'bg-muted text-foreground'
                    : 'ml-auto bg-primary text-primary-foreground'
                }`}
              >
                {message.text}
              </div>
            ))}
          </div>

          <div className="flex items-center gap-2 border-t border-border bg-card px-3 py-3">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  void handleSend();
                }
              }}
              placeholder="Escreva sua solicitação..."
              className="h-10 flex-1 rounded-md border border-input bg-background px-3 text-sm outline-none ring-0 focus:border-primary"
              disabled={isLoading}
            />
            <button
              type="button"
              onClick={() => void handleSend()}
              className="inline-flex h-10 w-10 items-center justify-center rounded-md bg-primary text-primary-foreground transition-opacity hover:opacity-90"
              aria-label="Enviar mensagem"
              disabled={isLoading}
            >
              <Send size={16} />
            </button>
          </div>
        </div>
      ) : (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={() => setIsOpen(true)}
            className="relative inline-flex h-14 w-14 items-center justify-center rounded-full border border-primary/20 bg-primary text-primary-foreground shadow-xl transition-transform hover:scale-105"
            aria-label="Abrir chat do copiloto"
          >
            <Bot size={24} />
            <span className="absolute -right-1 -top-1 inline-flex h-5 w-5 items-center justify-center rounded-full bg-warning text-warning-foreground">
              <Sparkles size={12} />
            </span>
          </button>
        </div>
      )}
    </div>
  );
}
