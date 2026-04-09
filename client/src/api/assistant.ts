import { apiClient } from "./client";
import type { AiChatMessage, AiChatResponse } from "../types/hrms";

export async function sendAssistantChat(messages: AiChatMessage[], currentPath: string) {
  const response = await apiClient.post<AiChatResponse>("/aiassistant/chat", {
    messages,
    currentPath,
  });

  return response.data;
}
