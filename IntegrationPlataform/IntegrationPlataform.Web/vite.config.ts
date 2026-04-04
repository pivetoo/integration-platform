import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: [
      { find: 'd-rts/styles', replacement: 'archon-ui/styles' },
      { find: 'd-rts', replacement: path.resolve(__dirname, './src/compat/d-rts.ts') },
      { find: 'react', replacement: path.resolve(__dirname, './node_modules/react') },
      { find: 'react-dom', replacement: path.resolve(__dirname, './node_modules/react-dom') },
      { find: 'react-router-dom', replacement: path.resolve(__dirname, './node_modules/react-router-dom') },
    ],
    dedupe: ['react', 'react-dom', 'react-router-dom'],
  },
})
