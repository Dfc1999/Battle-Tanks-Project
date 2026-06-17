import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { computed } from '@angular/core';

export interface ChatMessageState {
    id?: string;
    playerId: string;
    playerName: string;
    message: string;
    timestamp: Date;
    isSystem?: boolean;
}

interface ChatState {
    messages: ChatMessageState[];
    isLoading: boolean;
}

const initialState: ChatState = {
    messages: [],
    isLoading: false,
};

// Chat store con NgRx Signals
export const ChatStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed((store) => ({
        messageCount: computed(() => store.messages().length),
        lastMessage: computed(() => {
            const msgs = store.messages();
            return msgs.length > 0 ? msgs[msgs.length - 1] : null;
        }),
        playerMessages: computed(() =>
            store.messages().filter(m => !m.isSystem)
        ),
        systemMessages: computed(() =>
            store.messages().filter(m => m.isSystem)
        ),
    })),
    withMethods((store) => ({

        setMessages(messages: ChatMessageState[]): void {
            console.log('[ChatStore] Cargando mensajes:', messages.length);
            patchState(store, { messages });
        },

        addMessage(message: ChatMessageState): void {
            console.log('[ChatStore] Nuevo mensaje de:', message.playerName);
            patchState(store, (state) => ({
                messages: [...state.messages, message],
            }));
        },

        addSystemMessage(text: string): void {
            const systemMessage: ChatMessageState = {
                playerId: 'system',
                playerName: 'Sistema',
                message: text,
                timestamp: new Date(),
                isSystem: true,
            };
            console.log('[ChatStore] Mensaje del sistema:', text);
            patchState(store, (state) => ({
                messages: [...state.messages, systemMessage],
            }));
        },

        clearMessages(): void {
            console.log('[ChatStore] Limpiando mensajes');
            patchState(store, { messages: [] });
        },

        setLoading(isLoading: boolean): void {
            patchState(store, { isLoading });
        },
    }))
);
