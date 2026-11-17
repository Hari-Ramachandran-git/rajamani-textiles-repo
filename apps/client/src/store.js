import { create } from 'zustand';
import { api } from '../../../packages/shared/api';
export const useShop = create((set) => ({
  products: [], cart: [], error: '', loading: true,
  load: async () => { set({ loading: true, error: '' }); try { set({ products: await api('/products') }); } catch (e) { set({ error: e.message }); } finally { set({ loading: false }); } },
  add: product => set(s => { const item = s.cart.find(i => i.id === product.id); return { cart: item ? s.cart.map(i => i.id === product.id ? { ...i, quantity: Math.min(i.quantity + 1, product.stock) } : i) : [...s.cart, { ...product, quantity: 1 }] }; }),
  quantity: (id, quantity) => set(s => ({ cart: s.cart.map(i => i.id === id ? { ...i, quantity: Math.max(0, Math.min(quantity, i.stock)) } : i).filter(i => i.quantity > 0) })),
  clear: () => set({ cart: [] })
}));
