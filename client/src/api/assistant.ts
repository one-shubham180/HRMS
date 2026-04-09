import { useAuthStore } from "../features/auth/authStore";
import type { AiChatMessage, AiChatStreamEvent } from "../types/hrms";

const apiBaseUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5108/api";

interface StreamAssistantChatOptions {
  messages: AiChatMessage[];
  currentPath: string;
  onEvent: (event: AiChatStreamEvent) => void;
}

function normalizeStreamEvent(payload: Record<string, unknown>): AiChatStreamEvent {
  return {
    type: String(payload.type ?? payload.Type ?? "error") as AiChatStreamEvent["type"],
    delta: (payload.delta ?? payload.Delta ?? null) as string | null,
    message: (payload.message ?? payload.Message ?? null) as string | null,
    actions: (payload.actions ?? payload.Actions ?? []) as AiChatStreamEvent["actions"],
    autoNavigatePath: (payload.autoNavigatePath ?? payload.AutoNavigatePath ?? null) as string | null,
    error: (payload.error ?? payload.Error ?? null) as string | null,
  };
}

export async function streamAssistantChat({ messages, currentPath, onEvent }: StreamAssistantChatOptions) {
  const token = useAuthStore.getState().accessToken;
  const response = await fetch(`${apiBaseUrl}/aiassistant/chat-stream`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({
      messages,
      currentPath,
    }),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Unable to start assistant stream.");
  }

  if (!response.body) {
    throw new Error("Streaming is not available in this browser.");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  const processEventBlock = (eventBlock: string) => {
    const dataLines = eventBlock
      .split("\n")
      .filter((line) => line.startsWith("data:"))
      .map((line) => line.slice(5).trim());

    if (dataLines.length === 0) {
      return;
    }

    const payload = dataLines.join("");
    if (!payload) {
      return;
    }

    onEvent(normalizeStreamEvent(JSON.parse(payload) as Record<string, unknown>));
  };

  while (true) {
    const { value, done } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split("\n\n");
    buffer = events.pop() ?? "";

    for (const eventBlock of events) {
      processEventBlock(eventBlock);
    }
  }

  const finalBuffer = buffer.trim();
  if (finalBuffer) {
    processEventBlock(finalBuffer);
  }
}
