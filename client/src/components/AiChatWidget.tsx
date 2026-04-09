import { useEffect, useRef, useState } from "react";
import { Bot, ChevronDown, Compass, LoaderCircle, SendHorizontal, Sparkles } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import { sendAssistantChat } from "../api/assistant";
import { useAuthStore } from "../features/auth/authStore";
import type { AiAssistantAction, AiChatMessage } from "../types/hrms";

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

function createId() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function AiChatWidget() {
  const navigate = useNavigate();
  const location = useLocation();
  const roles = useAuthStore((state) => state.roles);
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatEntry[]>([
    {
      id: createId(),
      role: "assistant",
      content:
        roles.includes("Employee")
          ? "I can explain portal features, answer workflow questions, and take you to the right page when you ask."
          : "I can explain HRMS modules, summarize workflows, and open the right page for you when you ask.",
      actions: [],
    },
  ]);

  const transcriptRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);

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

  const submitMessage = async (messageText: string) => {
    const trimmed = messageText.trim();
    if (!trimmed || isSending) {
      return;
    }

    const nextUserMessage: ChatEntry = {
      id: createId(),
      role: "user",
      content: trimmed,
      actions: [],
    };

    const nextMessages = [...messages, nextUserMessage];

    setMessages(nextMessages);
    setInput("");
    setError(null);
    setIsSending(true);

    try {
      const response = await sendAssistantChat(
        nextMessages.map<AiChatMessage>((message) => ({ role: message.role, content: message.content })),
        location.pathname,
      );

      setMessages((current) => [
        ...current,
        {
          id: createId(),
          role: "assistant",
          content: response.message,
          actions: response.actions ?? [],
        },
      ]);

      if (response.autoNavigatePath) {
        navigate(response.autoNavigatePath);
      }
    } catch {
      setError("The assistant could not respond right now. Check the API key and model configuration, then try again.");
    } finally {
      setIsSending(false);
    }
  };

  return (
    <div
      className={`fixed right-5 z-40 flex items-end ${
        isOpen ? "inset-y-4" : "bottom-5"
      }`}
    >
      {isOpen ? (
        <section className="soft-pop flex h-full w-[min(420px,calc(100vw-1.5rem))] flex-col overflow-hidden rounded-[28px] border border-white/70 bg-white/95 shadow-[0_22px_65px_-24px_rgba(19,38,47,0.38)] backdrop-blur">
          <div className="border-b border-slate-200 px-4 py-2 text-slate-700">
            <div className="flex items-center justify-between gap-3">
              <div>
                <div className="inline-flex max-w-[280px] items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">
                  <Sparkles className="h-3.5 w-3.5" />
                  <span className="truncate">HRMS Compass • Features, workflows, navigation</span>
                </div>
              </div>
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
                  <p className="whitespace-pre-wrap">{message.content}</p>
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

            {isSending ? (
              <div className="flex justify-start">
                <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-4 py-2 text-sm text-slate-600">
                  <LoaderCircle className="h-4 w-4 animate-spin text-lagoon" />
                  Thinking
                </div>
              </div>
            ) : null}
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
