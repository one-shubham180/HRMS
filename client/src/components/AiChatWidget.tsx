import { useEffect, useMemo, useRef, useState } from "react";
import { Bot, ChevronDown, Compass, LoaderCircle, SendHorizontal, Sparkles, Trash2 } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import { streamAssistantChat } from "../api/assistant";
import { useAuthStore } from "../features/auth/authStore";
import type { AiAssistantAction, AiChatMessage, AiChatStreamEvent, Role } from "../types/hrms";

interface ChatEntry {
  id: string;
  role: "user" | "assistant";
  content: string;
  actions: AiAssistantAction[];
}

const quickPrompts = [
  "What can I do on this page?",
  "Explain the main HRMS features.",
  "Take me to payroll.",
];

const historyStoragePrefix = "hrms-ai-chat";

function createId() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function createWelcomeMessage(roles: Role[]): ChatEntry {
  return {
    id: createId(),
    role: "assistant",
    content:
      roles.includes("Employee")
        ? "I can help you understand this HRMS portal, keep track of our conversation, and guide you to the right page."
        : "I can walk through HRMS workflows, keep the conversation going across pages, and take you where you need to go.",
    actions: [],
  };
}

function loadStoredMessages(storageKey: string, roles: Role[]) {
  try {
    const rawValue = window.localStorage.getItem(storageKey);
    if (!rawValue) {
      return [createWelcomeMessage(roles)];
    }

    const parsed = JSON.parse(rawValue) as ChatEntry[];
    if (!Array.isArray(parsed) || parsed.length === 0) {
      return [createWelcomeMessage(roles)];
    }

    const sanitized = parsed.filter((message) => message.role === "user" || message.content.trim().length > 0);
    return sanitized.length > 0 ? sanitized : [createWelcomeMessage(roles)];
  } catch {
    return [createWelcomeMessage(roles)];
  }
}

function renderInlineContent(content: string) {
  const segments = content.split(/(\*\*[^*]+\*\*)/g).filter(Boolean);

  return segments.map((segment, index) => {
    const boldMatch = /^\*\*(.+)\*\*$/.exec(segment);
    if (boldMatch) {
      return (
        <strong key={`${segment}-${index}`} className="font-semibold text-ink">
          {boldMatch[1]}
        </strong>
      );
    }

    return <span key={`${segment}-${index}`}>{segment}</span>;
  });
}

function renderMessageContent(content: string) {
  const lines = content.split(/\r?\n/);
  const blocks: JSX.Element[] = [];
  let index = 0;

  while (index < lines.length) {
    const rawLine = lines[index];
    const line = rawLine.trim();

    if (!line) {
      index += 1;
      continue;
    }

    const bulletLines: string[] = [];
    while (index < lines.length) {
      const bulletLine = lines[index].trim();
      if (!/^([-*]|\d+\.)\s+/.test(bulletLine)) {
        break;
      }

      bulletLines.push(bulletLine);
      index += 1;
    }

    if (bulletLines.length > 0) {
      const ordered = /^\d+\.\s+/.test(bulletLines[0]);
      const ListTag = ordered ? "ol" : "ul";
      blocks.push(
        <ListTag
          key={`list-${blocks.length}`}
          className={`space-y-1 pl-5 ${ordered ? "list-decimal" : "list-disc"}`}
        >
          {bulletLines.map((item, itemIndex) => (
            <li key={`${item}-${itemIndex}`}>{renderInlineContent(item.replace(/^([-*]|\d+\.)\s+/, ""))}</li>
          ))}
        </ListTag>,
      );
      continue;
    }

    const headingMatch = /^#{1,3}\s+(.+)$/.exec(line) ?? /^\*\*(.+)\*\*$/.exec(line);
    if (headingMatch) {
      blocks.push(
        <h4 key={`heading-${blocks.length}`} className="text-sm font-semibold tracking-[0.01em] text-ink">
          {headingMatch[1]}
        </h4>,
      );
      index += 1;
      continue;
    }

    const paragraphLines = [line];
    index += 1;
    while (index < lines.length) {
      const nextLine = lines[index].trim();
      if (!nextLine || /^([-*]|\d+\.)\s+/.test(nextLine) || /^#{1,3}\s+/.test(nextLine) || /^\*\*.+\*\*$/.test(nextLine)) {
        break;
      }

      paragraphLines.push(nextLine);
      index += 1;
    }

    blocks.push(
      <p key={`paragraph-${blocks.length}`} className="whitespace-pre-wrap">
        {renderInlineContent(paragraphLines.join(" "))}
      </p>,
    );
  }

  return blocks;
}

export function AiChatWidget() {
  const navigate = useNavigate();
  const location = useLocation();
  const { roles, email } = useAuthStore();
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatEntry[]>([]);
  const [historyReady, setHistoryReady] = useState(false);

  const transcriptRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);
  const storageKey = useMemo(() => `${historyStoragePrefix}:${email ?? "anonymous"}`, [email]);

  useEffect(() => {
    setMessages(loadStoredMessages(storageKey, roles));
    setHistoryReady(true);
  }, [roles, storageKey]);

  useEffect(() => {
    if (!historyReady) {
      return;
    }

    window.localStorage.setItem(storageKey, JSON.stringify(messages));
  }, [historyReady, messages, storageKey]);

  useEffect(() => {
    if (!transcriptRef.current) {
      return;
    }

    transcriptRef.current.scrollTop = transcriptRef.current.scrollHeight;
  }, [messages, isOpen]);

  useEffect(() => {
    const textarea = inputRef.current;
    if (!textarea) {
      return;
    }

    textarea.style.height = "0px";
    const computedStyle = window.getComputedStyle(textarea);
    const lineHeight = Number.parseFloat(computedStyle.lineHeight) || 24;
    const paddingTop = Number.parseFloat(computedStyle.paddingTop) || 0;
    const paddingBottom = Number.parseFloat(computedStyle.paddingBottom) || 0;
    const borderTop = Number.parseFloat(computedStyle.borderTopWidth) || 0;
    const borderBottom = Number.parseFloat(computedStyle.borderBottomWidth) || 0;
    const maxHeight = lineHeight * 4 + paddingTop + paddingBottom + borderTop + borderBottom;
    textarea.style.height = `${Math.min(textarea.scrollHeight, maxHeight)}px`;
    textarea.style.overflowY = textarea.scrollHeight > maxHeight ? "auto" : "hidden";
  }, [input]);

  const updateAssistantMessage = (
    assistantMessageId: string,
    updater: (message: ChatEntry) => ChatEntry,
  ) => {
    setMessages((current) =>
      current.map((message) => (message.id === assistantMessageId ? updater(message) : message)),
    );
  };

  const clearConversation = () => {
    const nextMessages = [createWelcomeMessage(roles)];
    window.localStorage.setItem(storageKey, JSON.stringify(nextMessages));
    setMessages(nextMessages);
    setError(null);
  };

  const submitMessage = async (messageText: string) => {
    const trimmed = messageText.trim();
    if (!trimmed || isSending) {
      return;
    }

    const userMessage: ChatEntry = {
      id: createId(),
      role: "user",
      content: trimmed,
      actions: [],
    };

    const assistantMessageId = createId();
    const assistantPlaceholder: ChatEntry = {
      id: assistantMessageId,
      role: "assistant",
      content: "",
      actions: [],
    };

    const requestMessages = [...messages, userMessage].map<AiChatMessage>((message) => ({
      role: message.role,
      content: message.content,
    }));

    setMessages((current) => [
      ...current.filter((message) => message.role === "user" || message.content.trim().length > 0),
      userMessage,
      assistantPlaceholder,
    ]);
    setInput("");
    setError(null);
    setIsSending(true);

    try {
      await streamAssistantChat({
        messages: requestMessages,
        currentPath: location.pathname,
        onEvent: (event: AiChatStreamEvent) => {
          if (event.type === "delta" && event.delta) {
            updateAssistantMessage(assistantMessageId, (message) => ({
              ...message,
              content: `${message.content}${event.delta ?? ""}`,
            }));
            return;
          }

          if (event.type === "complete") {
            updateAssistantMessage(assistantMessageId, (message) => ({
              ...message,
              content: event.message?.trim() || message.content || "I can help with HRMS workflows and navigation.",
              actions: event.actions ?? [],
            }));

            if (event.autoNavigatePath) {
              navigate(event.autoNavigatePath);
            }
            return;
          }

          if (event.type === "error") {
            setError(event.error ?? "The assistant could not respond right now.");
          }
        },
      });
    } catch (streamError) {
      setMessages((current) => current.filter((message) => message.id !== assistantMessageId));
      setError(streamError instanceof Error ? streamError.message : "The assistant could not respond right now.");
    } finally {
      setIsSending(false);
    }
  };

  return (
    <div className={`fixed right-5 z-40 flex items-end ${isOpen ? "inset-y-4" : "bottom-5"}`}>
      {isOpen ? (
        <section className="soft-pop flex h-full w-[min(420px,calc(100vw-1.5rem))] flex-col overflow-hidden rounded-[28px] border border-white/70 bg-white/95 shadow-[0_22px_65px_-24px_rgba(19,38,47,0.38)] backdrop-blur">
          <div className="border-b border-slate-200 px-4 py-2 text-slate-700">
            <div className="flex items-center justify-between gap-3">
              <div className="inline-flex max-w-[280px] items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">
                <Sparkles className="h-3.5 w-3.5" />
                <span className="truncate">HRMS Compass - Features, workflows, navigation</span>
              </div>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  className="inline-flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-100 hover:text-slate-700"
                  onClick={clearConversation}
                  aria-label="Clear conversation"
                  title="Clear chat"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
                <button
                  type="button"
                  className="inline-flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-100 hover:text-slate-700"
                  onClick={() => setIsOpen(false)}
                  aria-label="Collapse assistant"
                >
                  <ChevronDown className="h-5 w-5" />
                </button>
              </div>
            </div>
          </div>

          <div ref={transcriptRef} className="chat-scroll min-h-0 flex-1 space-y-4 overflow-y-auto px-4 py-4">
            {messages.map((message) => (
              <article key={message.id} className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}>
                <div
                  className={`max-w-[85%] rounded-[24px] px-4 py-3 text-sm leading-6 ${
                    message.role === "user"
                      ? "bg-ink text-white"
                      : "border border-slate-200 bg-[linear-gradient(180deg,#fffefb_0%,#f5f8f7_100%)] text-slate-700"
                  }`}
                >
                  {message.role === "assistant" ? (
                    <div className="mb-2 inline-flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.24em] text-lagoon">
                      <Bot className="h-3.5 w-3.5" />
                      Assistant
                    </div>
                  ) : null}
                  <div className="space-y-3">
                    {message.content
                      ? renderMessageContent(message.content)
                      : isSending && message.role === "assistant"
                        ? <p>Thinking...</p>
                        : null}
                  </div>
                  {message.actions.length > 0 ? (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {message.actions.map((action) => (
                        <button
                          key={`${message.id}-${action.path}`}
                          type="button"
                          className={`inline-flex items-center gap-2 rounded-full px-3 py-2 text-xs font-semibold transition ${
                            action.autoNavigate
                              ? "bg-lagoon text-white hover:bg-teal-700"
                              : "border border-slate-200 bg-white text-slate-700 hover:border-lagoon hover:text-lagoon"
                          }`}
                          onClick={() => navigate(action.path)}
                          title={action.description}
                        >
                          <Compass className="h-3.5 w-3.5" />
                          {action.label}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              </article>
            ))}
          </div>

          <div className="shrink-0 border-t border-slate-200 bg-slate-50/80 px-4 py-4">
            <div className="chat-chip-scroll -mx-1 mb-3 overflow-x-auto pb-1">
              <div className="flex min-w-max gap-2 px-1">
                {quickPrompts.map((prompt) => (
                  <button
                    key={prompt}
                    type="button"
                    className="shrink-0 whitespace-nowrap rounded-full border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:border-lagoon hover:text-lagoon"
                    onClick={() => submitMessage(prompt)}
                    disabled={isSending}
                  >
                    {prompt}
                  </button>
                ))}
              </div>
            </div>

            <form
              className="space-y-2"
              onSubmit={(event) => {
                event.preventDefault();
                void submitMessage(input);
              }}
            >
              <div className="flex items-end gap-2">
                <textarea
                  ref={inputRef}
                  rows={1}
                  className="input chat-input-scroll min-h-[48px] max-h-[120px] flex-1 resize-none py-[11px] leading-6"
                  placeholder="Ask about features or say 'take me to payroll'."
                  value={input}
                  onChange={(event) => setInput(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && !event.shiftKey) {
                      event.preventDefault();
                      if (!isSending && input.trim().length > 0) {
                        void submitMessage(input);
                      }
                    }
                  }}
                />
                <button
                  type="submit"
                  className="inline-flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-ink text-white transition hover:bg-slate-900 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isSending || input.trim().length === 0}
                  aria-label="Send message"
                >
                  {isSending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <SendHorizontal className="h-4 w-4" />}
                </button>
              </div>
              <div className="flex items-center justify-between gap-3">
                <p className="text-xs text-slate-500">Current page: {location.pathname}</p>
              </div>
            </form>

            {error ? <p className="mt-3 text-sm text-rose-600">{error}</p> : null}
          </div>
        </section>
      ) : null}

      {!isOpen ? (
        <button
          type="button"
          className="pulse-glow inline-flex items-center gap-3 rounded-full bg-ink px-5 py-3 text-sm font-semibold text-white shadow-[0_18px_45px_-18px_rgba(19,38,47,0.55)] transition hover:bg-slate-900"
          onClick={() => setIsOpen(true)}
        >
          <Bot className="h-4 w-4" />
          AI Chat
        </button>
      ) : null}
    </div>
  );
}
