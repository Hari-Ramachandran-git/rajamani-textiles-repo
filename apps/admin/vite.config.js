import { defineConfig } from 'vite';
export default defineConfig({ publicDir: '../client/public', server: { proxy: { '/api': 'http://localhost:5000' }, fs: { allow: ['../..'] } } });
