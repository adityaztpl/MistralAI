import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type CartLine = {
  productId: string;
  name: string;
  unitPriceCents: number;
  quantity: number;
};

export type AddToCartInput = {
  productId: string;
  name: string;
  unitPriceCents: number;
  quantity?: number;
};

export type CartStore = {
  lines: CartLine[];
  addItem: (item: AddToCartInput) => void;
  removeItem: (productId: string) => void;
  setQuantity: (productId: string, quantity: number) => void;
  clearCart: () => void;
};

export const useCartStore = create<CartStore>()(
  persist(
    (set) => ({
      lines: [],
      addItem: ({ productId, name, unitPriceCents, quantity = 1 }) =>
        set((state) => {
          const existingLine = state.lines.find((line) => line.productId === productId);

          if (existingLine) {
            return {
              lines: state.lines.map((line) =>
                line.productId === productId
                  ? { ...line, quantity: line.quantity + quantity }
                  : line,
              ),
            };
          }

          return {
            lines: [
              ...state.lines,
              {
                productId,
                name,
                unitPriceCents,
                quantity,
              },
            ],
          };
        }),
      removeItem: (productId) =>
        set((state) => ({
          lines: state.lines.filter((line) => line.productId !== productId),
        })),
      setQuantity: (productId, quantity) =>
        set((state) => {
          if (quantity <= 0) {
            return {
              lines: state.lines.filter((line) => line.productId !== productId),
            };
          }

          return {
            lines: state.lines.map((line) =>
              line.productId === productId ? { ...line, quantity } : line,
            ),
          };
        }),
      clearCart: () => set({ lines: [] }),
    }),
    {
      name: 'cart-store-v1',
      partialize: (state) => ({ lines: state.lines }),
    },
  ),
);

export function selectCartItemCount(state: CartStore) {
  return state.lines.reduce((count, line) => count + line.quantity, 0);
}

export function selectCartSubtotalCents(state: CartStore) {
  return state.lines.reduce(
    (subtotal, line) => subtotal + line.unitPriceCents * line.quantity,
    0,
  );
}

export function useCartItemCount() {
  return useCartStore(selectCartItemCount);
}

export function useCartSubtotalCents() {
  return useCartStore(selectCartSubtotalCents);
}
