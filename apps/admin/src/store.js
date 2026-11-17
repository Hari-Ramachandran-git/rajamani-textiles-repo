import { create } from 'zustand';
import { api } from '../../../packages/shared/api';
export const useAdmin = create((set, get) => ({
  key: sessionStorage.getItem('adminKey') || '', products: [], orders: [], error: '', loading: false, connected: false,
  setKey: key => { sessionStorage.setItem('adminKey', key); set({ key, connected: false, orders: [], products: [] }); },
  request: (path, options = {}) => api(`/admin${path}`, { ...options, headers: { 'X-Admin-Key': get().key } }),
  load: async () => { set({ loading: true, error: '' }); try { const [products, orders] = await Promise.all([api('/products'), get().request('/orders')]); set({ products, orders, connected: true }); } catch (e) { set({ error: e.message, connected: false, orders: [], products: [] }); } finally { set({ loading: false }); } }
}));
